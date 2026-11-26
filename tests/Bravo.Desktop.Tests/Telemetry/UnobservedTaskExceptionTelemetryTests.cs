using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Bravo.Core.Telemetry;
using Bravo.Desktop.Telemetry;
using NSubstitute;
using Xunit;

namespace Bravo.Desktop.Tests.Telemetry;

public sealed class UnobservedTaskExceptionTelemetryTests
{
    private const string Message = "A task of the test failed, and nothing awaited it.";

    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task OnUnobservedTaskException_Exception_SendsItToTheTelemetry()
    {
        // Not started: the event of the runtime is raised for the tasks of every test.
        var telemetry = Substitute.For<IExceptionTelemetry>();
        var sender = new UnobservedTaskExceptionTelemetry(telemetry);
        var exception = new AggregateException(new InvalidOperationException(Message));
        var tracked = WhenTracked(telemetry);

        sender.OnUnobservedTaskException(this, new UnobservedTaskExceptionEventArgs(exception));

        Assert.Same(exception, await tracked);
    }

    // The runtime raises the event when it collects a task that failed and that nothing awaited.
    [Fact]
    public async Task StartAsync_TaskThatFailsUnawaited_SendsItsExceptionOnceCollected()
    {
        var telemetry = Substitute.For<IExceptionTelemetry>();
        var sender = new UnobservedTaskExceptionTelemetry(telemetry);
        var tracked = WhenTracked(telemetry);

        await sender.StartAsync(TestContext.Current.CancellationToken);
        FailATaskThatNothingAwaits();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        await sender.StopAsync(TestContext.Current.CancellationToken);

        await tracked;
        telemetry.Received(1).Track(Arg.Is<Exception>(exception => IsOfTheTest(exception)));
    }

    // The exception of the test, which the telemetry is given on a thread of the pool.
    private static Task<Exception> WhenTracked(IExceptionTelemetry telemetry)
    {
        var tracked = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        telemetry.When(t => t.Track(Arg.Is<Exception>(exception => IsOfTheTest(exception))))
            .Do(call => tracked.TrySetResult(call.Arg<Exception>()));
        return tracked.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
    }

    private static bool IsOfTheTest(Exception exception)
    {
        return exception.InnerException?.Message == Message;
    }

    // In a method of its own: no reference to the task is left once it returns.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FailATaskThatNothingAwaits()
    {
        _ = Task.FromException(new InvalidOperationException(Message));
    }
}
