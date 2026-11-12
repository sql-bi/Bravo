using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Activation;
using Bravo.Desktop.Activation;
using Bravo.Desktop.Shell;
using NSubstitute;
using Xunit;

namespace Bravo.Desktop.Tests.Activation;

public sealed class ActivationPumpTests
{
    [Fact]
    public async Task RunAsync_RequestsPostedBeforeItStarts_BringTheWindowForwardOncePerRequest()
    {
        var channel = new ActivationChannel(capacity: 4);
        channel.Post(ActivationRequest.Empty);
        channel.Post(new ActivationRequest { ServerName = "localhost:12345", DatabaseName = "db" });
        channel.Complete();
        var window = Substitute.For<IMainWindow>();

        await new ActivationPump(channel).RunAsync(window, TestContext.Current.CancellationToken);

        window.Received(2).BringToForeground();
    }

    [Fact]
    public async Task RunAsync_CanceledWhileItWaitsForARequest_Ends()
    {
        var channel = new ActivationChannel(capacity: 4);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var running = new ActivationPump(channel).RunAsync(Substitute.For<IMainWindow>(), cancellation.Token);

        await cancellation.CancelAsync();

        await running;
    }
}
