using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;
using Sqlbi.Bravo.Infrastructure.Telemetry;
using Xunit;

namespace Bravo.Tests.Infrastructure.Diagnostics;

// The callbacks are called directly, so that no fatal exception hook is installed in the test process.
public class GlobalExceptionDispatcherTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RuntimeCallbacks_FatalAndUnobservedReportingOverlap_DeliversBoth(bool unobservedFirst)
    {
        using var releaseFirst = new ManualResetEventSlim();
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reported = new ConcurrentQueue<Exception>();
        var fatal = new InvalidOperationException("fatal");
        var unobserved = new UnobservedTaskExceptionEventArgs(new AggregateException("unobserved"));
        var firstOrigin = unobservedFirst ? ExceptionOrigin.UnobservedTask : ExceptionOrigin.AppDomain;

        var dispatcher = new GlobalExceptionDispatcher((exception, origin) =>
        {
            reported.Enqueue(exception);
            if (origin == firstOrigin)
            {
                firstEntered.TrySetResult();
                releaseFirst.Wait(s_timeout);
            }
        });

        var first = Task.Run(() => Raise(unobservedFirst));
        var second = Task.CompletedTask;
        try
        {
            await firstEntered.Task.WaitAsync(s_timeout);

            second = Task.Run(() => Raise(!unobservedFirst));
            await second.WaitAsync(s_timeout);

            Assert.False(first.IsCompleted, "The second callback must complete while the first report is still blocked.");
            Assert.Contains(fatal, reported);
            Assert.Contains(unobserved.Exception, reported);
            Assert.Equal(2, reported.Count);
            Assert.True(unobserved.Observed);
        }
        finally
        {
            releaseFirst.Set();
            await Task.WhenAll(first, second).WaitAsync(s_timeout);
        }

        void Raise(bool isUnobserved)
        {
            if (isUnobserved)
                dispatcher.OnUnobservedTaskException(null, unobserved);
            else
                RaiseFatal(dispatcher, fatal);
        }
    }

    [Fact]
    public void RuntimeCallbacks_ReentrantFatalException_ReportsOnlyTheFirst()
    {
        var reported = new List<Exception>();
        var first = new InvalidOperationException("first");
        var nested = new InvalidOperationException("nested");
        GlobalExceptionDispatcher? dispatcher = null;
        dispatcher = new GlobalExceptionDispatcher((exception, _) =>
        {
            reported.Add(exception);
            if (ReferenceEquals(exception, first))
                RaiseFatal(dispatcher!, nested);
        });

        RaiseFatal(dispatcher, first);

        Assert.Same(first, Assert.Single(reported));
    }

    [Fact]
    public async Task RuntimeCallbacks_ConcurrentFatalExceptions_ReportsTheFirstAndBlocksTheOther()
    {
        using var releaseFirst = new ManualResetEventSlim();
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reported = new ConcurrentQueue<Exception>();
        var first = new InvalidOperationException("first");
        var dispatcher = new GlobalExceptionDispatcher((exception, _) =>
        {
            reported.Enqueue(exception);
            firstEntered.TrySetResult();
            releaseFirst.Wait(s_timeout);
        });

        var firstTask = Task.Run(() => RaiseFatal(dispatcher, first));
        await firstEntered.Task.WaitAsync(s_timeout);

        // A background thread, because it stays blocked for the rest of the test run.
        var second = new Thread(() => RaiseFatal(dispatcher, new InvalidOperationException("second"))) { IsBackground = true };
        second.Start();
        var secondReturned = second.Join(TimeSpan.FromMilliseconds(500));
        releaseFirst.Set();
        await firstTask.WaitAsync(s_timeout);

        Assert.False(secondReturned);
        Assert.Same(first, Assert.Single(reported));
    }

    [Fact]
    public void RuntimeCallbacks_UnobservedTask_ReportsWithoutACrashReportOrFlush()
    {
        var telemetry = Substitute.For<ITelemetryService>();
        var handler = new UnhandledExceptionHandler(new CrashReportStore("crashes"), Substitute.For<ICrashReporterLauncher>(), TimeProvider.System, (_) => { });
        handler.AttachServices(NullLoggerFactory.Instance, telemetry, new TelemetryConsent(IsEnabled: true, TelemetryConsentSource.UserSettings));
        var args = new UnobservedTaskExceptionEventArgs(new AggregateException("unobserved"));
        var dispatcher = new GlobalExceptionDispatcher(handler.Handle);

        dispatcher.OnUnobservedTaskException(null, args);

        Assert.True(args.Observed);
        telemetry.Received(1).TrackException(args.Exception, Arg.Is<IDictionary<string, string>>(
            properties => properties["ExceptionOrigin"] == nameof(ExceptionOrigin.UnobservedTask)));
        telemetry.DidNotReceive().TryFlushBeforeShutdown(Arg.Any<int>());
        telemetry.DidNotReceive().FlushAsync(Arg.Any<CancellationToken>());
    }

    private static void RaiseFatal(GlobalExceptionDispatcher dispatcher, Exception exception)
    {
        dispatcher.OnUnhandledException(null, new UnhandledExceptionEventArgs(exception, isTerminating: true));
    }
}
