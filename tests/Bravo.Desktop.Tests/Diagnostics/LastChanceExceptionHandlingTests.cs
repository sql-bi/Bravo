using System;
using System.Threading;
using Bravo.Desktop.Diagnostics;
using NSubstitute;
using Xunit;

namespace Bravo.Desktop.Tests.Diagnostics;

public sealed class LastChanceExceptionHandlingTests
{
    private readonly IFatalExceptionHandler _handler = Substitute.For<IFatalExceptionHandler>();
    private int _waits;

    [Fact]
    public void OnUnhandledException_Exception_GivesItToTheHandler()
    {
        var handling = CreateHandling();
        var exception = new InvalidOperationException("It failed.");

        handling.OnUnhandledException(this, new UnhandledExceptionEventArgs(exception, isTerminating: true));

        _handler.Received(1).Handle(exception);
    }

    [Fact]
    public void OnUnhandledException_ObjectThatIsNotAnException_GivesAnExceptionThatNamesIt()
    {
        var handling = CreateHandling();

        handling.OnUnhandledException(this, new UnhandledExceptionEventArgs("thrown text", isTerminating: true));

        _handler.Received(1).Handle(Arg.Is<Exception>(exception => exception.Message.Contains("thrown text")));
    }

    [Fact]
    public void OnUnhandledException_ExceptionOfTheHandlerItself_IsNotGivenToTheHandler()
    {
        var handling = CreateHandling();
        var second = new UnhandledExceptionEventArgs(new InvalidOperationException("Second."), isTerminating: true);
        _handler
            .When(called => called.Handle(Arg.Any<Exception>()))
            .Do(_ => handling.OnUnhandledException(this, second));

        handling.OnUnhandledException(this, Escaped("First."));

        _handler.Received(1).Handle(Arg.Any<Exception>());
        Assert.Equal(0, _waits);
    }

    // The runtime ends the process when the handler of a thread returns: the other thread must not return first.
    [Fact]
    public void OnUnhandledException_ExceptionOfAnotherThreadWhileOneIsHandled_WaitsForTheEnd()
    {
        var handling = CreateHandling();
        _handler
            .When(called => called.Handle(Arg.Any<Exception>()))
            .Do(_ =>
            {
                var other = new Thread(() => handling.OnUnhandledException(this, Escaped("On another thread.")));
                other.Start();
                other.Join();
            });

        handling.OnUnhandledException(this, Escaped("First."));

        _handler.Received(1).Handle(Arg.Any<Exception>());
        Assert.Equal(1, _waits);
    }

    private static UnhandledExceptionEventArgs Escaped(string message)
    {
        return new UnhandledExceptionEventArgs(new InvalidOperationException(message), isTerminating: true);
    }

    private LastChanceExceptionHandling CreateHandling()
    {
        return new LastChanceExceptionHandling(_handler, () => Interlocked.Increment(ref _waits));
    }
}
