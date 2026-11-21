using System;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Api.Events;
using Bravo.Core.Activation;
using Bravo.Desktop.Activation;
using Bravo.Desktop.Shell;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

namespace Bravo.Desktop.Tests.Shell;

public sealed class MainWindowRunnerTests : IDisposable
{
    private readonly CancellationTokenSource _hostStopping = new();
    private readonly ActivationChannel _activations = new(capacity: 4);
    private readonly FakeMainWindow _window = new();
    private readonly MainWindowDispatcher _dispatcher = new();
    private readonly MainWindowRunner _runner;

    public MainWindowRunnerTests()
    {
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Returns(_hostStopping.Token);

        var pump = new ActivationPump(_activations, Substitute.For<IHostEventPublisher>());
        _runner = new MainWindowRunner(lifetime, _activations, pump, _dispatcher);
    }

    public void Dispose()
    {
        _window.Dispose();
        _hostStopping.Dispose();
    }

    [Fact]
    public void Run_HostThatStopsWhileTheWindowIsOpen_ClosesTheWindow()
    {
        _window.WhileOpen = _hostStopping.Cancel;

        _runner.Run(_window);

        Assert.Equal(1, _window.CloseRequests);
    }

    [Fact]
    public void Run_HostThatIsStoppedAfterTheWindowWasClosed_DoesNotCloseItAgain()
    {
        _window.WhileOpen = _window.Close;
        _runner.Run(_window);

        _hostStopping.Cancel();

        Assert.Equal(0, _window.CloseRequests);
    }

    [Fact]
    public void Run_HostThatIsAlreadyStopping_DoesNotShowTheWindow()
    {
        _hostStopping.Cancel();

        _runner.Run(_window);

        Assert.Equal(0, _window.MessageLoops);
    }

    [Fact]
    public void Run_WindowThatWasClosed_LeavesTheActivationsClosed()
    {
        _window.WhileOpen = _window.Close;

        _runner.Run(_window);

        Assert.Equal(ActivationPostResult.Closed, _activations.Post(ActivationRequest.Empty));
    }

    [Fact]
    public void Run_HostThatIsAlreadyStopping_LeavesTheActivationsClosed()
    {
        _hostStopping.Cancel();

        _runner.Run(_window);

        Assert.Equal(ActivationPostResult.Closed, _activations.Post(ActivationRequest.Empty));
    }

    [Fact]
    public void Run_RequestsThatWaitedForTheWindow_BringItForwardWhenItIsShown()
    {
        _activations.Post(ActivationRequest.Empty);
        _activations.Post(ActivationRequest.Empty);
        _window.WhileOpen = () =>
        {
            SpinWait.SpinUntil(() => _window.ForegroundRequests == 2, TimeSpan.FromSeconds(30));
            _window.Close();
        };

        _runner.Run(_window);

        Assert.Equal(2, _window.ForegroundRequests);
    }

    [Fact]
    public void Run_RequestThatArrivesWhileTheWindowIsOpen_BringsItForward()
    {
        _window.WhileOpen = () =>
        {
            _activations.Post(ActivationRequest.Empty);
            SpinWait.SpinUntil(() => _window.ForegroundRequests == 1, TimeSpan.FromSeconds(30));
            _window.Close();
        };

        _runner.Run(_window);

        Assert.Equal(1, _window.ForegroundRequests);
    }

    [Fact]
    public async Task Run_WhileTheWindowIsOpen_TheDispatcherReachesIt()
    {
        Task<string?>? reaching = null;
        _window.WhileOpen = () =>
        {
            reaching = _dispatcher.RunAsync(_ => "reached", CancellationToken.None);
            _window.Close();
        };

        _runner.Run(_window);

        Assert.NotNull(reaching);
        Assert.Equal("reached", await reaching);
    }

    [Fact]
    public async Task Run_WindowThatWasClosed_LeavesTheDispatcherWithNoWindow()
    {
        _window.WhileOpen = _window.Close;
        _runner.Run(_window);

        var result = await _dispatcher.RunAsync(_ => "reached", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }
}
