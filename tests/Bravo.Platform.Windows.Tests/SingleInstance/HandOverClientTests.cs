using System;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using Bravo.Platform.Windows.SingleInstance;
using Xunit;

namespace Bravo.Platform.Windows.Tests.SingleInstance;

public sealed class HandOverClientTests
{
    private static readonly byte[] s_payload = [1, 2, 3, 4, 5];

    [Fact]
    public async Task SendAsync_NoPipe_ReturnsUnreachableAfterTheConnectTimeout()
    {
        var client = new HandOverClient(TestInstance.CreateNames(), TestInstance.ShortOptions, TimeProvider.System);

        var result = await client.SendAsync(s_payload);

        Assert.Equal(new HandOverResult(HandOverStatus.Unreachable), result);
    }

    [Fact]
    public async Task SendAsync_OwnerThatDoesNotAnswerInTime_ReturnsNoAnswer()
    {
        var names = TestInstance.CreateNames();
        using var receiver = new RecordingReceiver(HandOverReply.Accepted);
        receiver.Block();
        var listenerOptions = new SingleInstanceOptions { ExchangeTimeout = TimeSpan.FromSeconds(30) };
        await using var listener = new RunningListener(names, listenerOptions, receiver);
        var clientOptions = new SingleInstanceOptions
        {
            ConnectTimeout = TimeSpan.FromSeconds(30),
            ExchangeTimeout = TimeSpan.FromMilliseconds(300),
        };
        var client = new HandOverClient(names, clientOptions, TimeProvider.System);

        var result = await client.SendAsync(s_payload);

        receiver.Unblock();
        Assert.Equal(new HandOverResult(HandOverStatus.NoAnswer), result);
    }

    [Fact]
    public async Task SendAsync_PipeThatTheUserMayNotOpen_ReturnsAccessDenied()
    {
        var names = TestInstance.CreateNames();
        await using var denied = CreatePipeThatDeniesTheCurrentUser(names);
        var client = new HandOverClient(names, TestInstance.ShortOptions, TimeProvider.System);

        var result = await client.SendAsync(s_payload);

        Assert.Equal(new HandOverResult(HandOverStatus.AccessDenied), result);
    }

    internal static NamedPipeServerStream CreatePipeThatDeniesTheCurrentUser(InstanceNames names)
    {
        using var identity = WindowsIdentity.GetCurrent();
        var user = identity.User ?? throw new InvalidOperationException("The current identity has no user.");

        var security = new PipeSecurity();
        security.AddAccessRule(new PipeAccessRule(user, PipeAccessRights.FullControl, AccessControlType.Deny));

        return NamedPipeServerStreamAcl.Create(
            names.PipeName,
            PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous,
            inBufferSize: 0,
            outBufferSize: 0,
            security);
    }
}
