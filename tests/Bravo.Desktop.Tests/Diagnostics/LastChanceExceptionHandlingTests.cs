using System;
using Bravo.Desktop.Diagnostics;
using NSubstitute;
using Xunit;

namespace Bravo.Desktop.Tests.Diagnostics;

public sealed class LastChanceExceptionHandlingTests
{
    [Fact]
    public void OnUnhandledException_Exception_GivesItToTheHandler()
    {
        var handler = Substitute.For<IFatalExceptionHandler>();
        var handling = new LastChanceExceptionHandling(handler);
        var exception = new InvalidOperationException("It failed.");

        handling.OnUnhandledException(this, new UnhandledExceptionEventArgs(exception, isTerminating: true));

        handler.Received(1).Handle(exception);
    }

    [Fact]
    public void OnUnhandledException_ObjectThatIsNotAnException_GivesAnExceptionThatNamesIt()
    {
        var handler = Substitute.For<IFatalExceptionHandler>();
        var handling = new LastChanceExceptionHandling(handler);

        handling.OnUnhandledException(this, new UnhandledExceptionEventArgs("thrown text", isTerminating: true));

        handler.Received(1).Handle(Arg.Is<Exception>(exception => exception.Message.Contains("thrown text")));
    }

    [Fact]
    public void OnUnhandledException_ExceptionWhileAnotherOneIsHandled_IsNotGivenToTheHandler()
    {
        var handler = Substitute.For<IFatalExceptionHandler>();
        var handling = new LastChanceExceptionHandling(handler);
        var first = new UnhandledExceptionEventArgs(new InvalidOperationException("First."), isTerminating: true);
        var second = new UnhandledExceptionEventArgs(new InvalidOperationException("Second."), isTerminating: true);
        handler
            .When(called => called.Handle(Arg.Any<Exception>()))
            .Do(_ => handling.OnUnhandledException(this, second));

        handling.OnUnhandledException(this, first);

        handler.Received(1).Handle(Arg.Any<Exception>());
    }
}
