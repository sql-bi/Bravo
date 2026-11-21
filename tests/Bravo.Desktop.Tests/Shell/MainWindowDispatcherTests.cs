using System;
using System.Threading.Tasks;
using Bravo.Desktop.Shell;
using Xunit;

namespace Bravo.Desktop.Tests.Shell;

public sealed class MainWindowDispatcherTests : IDisposable
{
    private readonly FakeMainWindow _window = new();
    private readonly MainWindowDispatcher _dispatcher = new();

    public void Dispose()
    {
        _window.Dispose();
    }

    [Fact]
    public async Task RunAsync_NoWindow_ReturnsNullAndDoesNotRunTheFunction()
    {
        var hasRun = false;

        var result = await _dispatcher.RunAsync(_ => Record(ref hasRun), TestContext.Current.CancellationToken);

        Assert.Null(result);
        Assert.False(hasRun);
    }

    [Fact]
    public async Task RunAsync_AttachedWindow_RunsTheFunctionWithTheHandleOfTheWindow()
    {
        _dispatcher.Attach(_window);

        var result = await _dispatcher.RunAsync(handle => $"{handle}", TestContext.Current.CancellationToken);

        Assert.Equal($"{FakeMainWindow.Handle}", result);
    }

    [Fact]
    public async Task RunAsync_WindowThatWasDetached_ReturnsNull()
    {
        _dispatcher.Attach(_window);
        _dispatcher.Detach();

        var result = await _dispatcher.RunAsync(_ => "ran", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    private static string Record(ref bool hasRun)
    {
        hasRun = true;

        return "ran";
    }
}
