using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Sqlbi.Bravo.Infrastructure.Telemetry;
using Xunit;

namespace Bravo.Tests.Infrastructure.Diagnostics;

[CollectionDefinition(nameof(GlobalExceptionHandlingCollection), DisableParallelization = true)]
public sealed class GlobalExceptionHandlingCollection
{
}

[Collection(nameof(GlobalExceptionHandlingCollection))]
public class GlobalExceptionHandlingTests : IDisposable
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);
    private static readonly FieldInfo s_handlerField = typeof(GlobalExceptionHandling)
        .GetField("s_handler", BindingFlags.NonPublic | BindingFlags.Static)!;

    private readonly object? _previousHandler = s_handlerField.GetValue(null);

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

        SetHandler((exception, origin) =>
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
                RaiseUnobserved(unobserved);
            else
                RaiseFatal(fatal);
        }
    }

    [Fact]
    public void RuntimeCallbacks_ReentrantFatalException_ReportsOnlyTheFirst()
    {
        var reported = new List<Exception>();
        var first = new InvalidOperationException("first");
        var nested = new InvalidOperationException("nested");
        SetHandler((exception, _) =>
        {
            reported.Add(exception);
            if (ReferenceEquals(exception, first))
                RaiseFatal(nested);
        });

        RaiseFatal(first);

        Assert.Same(first, Assert.Single(reported));
    }

    [Fact]
    public void RuntimeCallbacks_UnobservedTask_ReportsWithoutDialogOrFlush()
    {
        var dialog = new RecordingDialog();
        var telemetry = Substitute.For<ITelemetryService>();
        var handler = new UnhandledExceptionHandler(dialog, telemetry, NullLoggerFactory.Instance);
        var args = new UnobservedTaskExceptionEventArgs(new AggregateException("unobserved"));
        SetHandler(handler.Handle);

        RaiseUnobserved(args);

        Assert.True(args.Observed);
        Assert.Equal(0, dialog.ShowCount);
        telemetry.Received(1).TrackException(args.Exception, Arg.Is<IDictionary<string, string>>(
            properties => properties["ExceptionOrigin"] == nameof(ExceptionOrigin.UnobservedTask)));
        telemetry.DidNotReceive().TryFlushBeforeShutdown(Arg.Any<int>());
        telemetry.DidNotReceive().FlushAsync(Arg.Any<CancellationToken>());
    }

    public void Dispose() => s_handlerField.SetValue(null, _previousHandler);

    private static void SetHandler(GlobalExceptionHandler handler) => s_handlerField.SetValue(null, handler);

    // Calling the callbacks directly avoids installing fatal exception hooks in the test process.
    private static void RaiseFatal(Exception exception)
        => typeof(GlobalExceptionHandling)
            .GetMethod("OnUnhandledException", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [null, new UnhandledExceptionEventArgs(exception, isTerminating: true)]);

    private static void RaiseUnobserved(UnobservedTaskExceptionEventArgs args)
        => typeof(GlobalExceptionHandling)
            .GetMethod("OnUnobservedTaskException", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [null, args]);

    private sealed class RecordingDialog : IErrorReportDialog
    {
        public int ShowCount { get; private set; }

        public void Show(ErrorReport report) => ShowCount++;
    }
}
