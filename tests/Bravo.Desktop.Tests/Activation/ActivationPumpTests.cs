using System.Threading;
using System.Threading.Tasks;
using Bravo.Api.Events;
using Bravo.Core.Activation;
using Bravo.Desktop.Activation;
using Bravo.Desktop.Shell;
using NSubstitute;
using Xunit;

namespace Bravo.Desktop.Tests.Activation;

public sealed class ActivationPumpTests
{
    private static readonly ActivationRequest s_reportOpened = new() { ServerName = "localhost:12345", DatabaseName = "db" };

    private readonly ActivationChannel _channel = new(capacity: 4);
    private readonly IHostEventPublisher _events = Substitute.For<IHostEventPublisher>();
    private readonly IMainWindow _window = Substitute.For<IMainWindow>();
    private readonly ActivationPump _pump;

    public ActivationPumpTests()
    {
        _pump = new ActivationPump(_channel, _events);
    }

    [Fact]
    public async Task RunAsync_RequestsPostedBeforeItStarts_BringTheWindowForwardOncePerRequest()
    {
        _channel.Post(ActivationRequest.Empty);
        _channel.Post(s_reportOpened);
        _channel.Complete();

        await _pump.RunAsync(_window, TestContext.Current.CancellationToken);

        _window.Received(2).BringToForeground();
    }

    [Fact]
    public async Task RunAsync_RequestsPostedBeforeItStarts_ArePublishedAsEventsInTheirOrder()
    {
        _channel.Post(ActivationRequest.Empty);
        _channel.Post(s_reportOpened);
        _channel.Complete();

        await _pump.RunAsync(_window, TestContext.Current.CancellationToken);

        Received.InOrder(() =>
        {
            _events.PublishActivation(ActivationRequest.Empty);
            _events.PublishActivation(s_reportOpened);
        });
    }

    [Fact]
    public async Task RunAsync_CanceledWhileItWaitsForARequest_Ends()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var running = _pump.RunAsync(_window, cancellation.Token);

        await cancellation.CancelAsync();

        await running;
    }
}
