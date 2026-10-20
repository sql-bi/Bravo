using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Messages;
using Sqlbi.Bravo.Infrastructure.SingleInstance;

namespace Sqlbi.Bravo.Host;

internal static partial class BravoSingleInstance
{
    /// <summary>
    /// Claims the application instance or, when another process holds it, forwards the startup arguments to it.
    /// </summary>
    /// <returns>
    /// The server that holds the application instance, owned by the caller; <see langword="null"/> when this process
    /// is a secondary instance and must exit.
    /// </returns>
    public static SingleInstanceServer? ClaimOrRedirect()
    {
        var options = new SingleInstanceOptions
        {
            PipeName = InstancePipeName.Create(),
        };

        return ClaimOrRedirect(options, CreateActivationPayload, CreateFailureLoggerFactory);
    }

    internal static SingleInstanceServer? ClaimOrRedirect(
        SingleInstanceOptions options,
        Func<byte[]> createActivationPayload,
        Func<ILoggerFactory> createFailureLoggerFactory)
    {
        if (SingleInstanceServer.TryStart(options, out var server))
            return server;

        var result = SingleInstanceClient.Send(options, createActivationPayload());
        if (!result.IsDelivered)
        {
            // One known cause is a launch whose elevation differs from the primary's: PipeOptions.CurrentUserOnly checks
            // the ACL on WindowsIdentity.Owner, which elevation changes (https://github.com/dotnet/runtime/issues/123903).
            // No telemetry: consent needs the bootstrap.
            // TODO: Consider surfacing activation failures to the user.
            using var loggerFactory = createFailureLoggerFactory();
            LogActivationRedirectFailed(loggerFactory.CreateLogger(typeof(BravoSingleInstance)), result.Exception, result.Status);
        }

        return null;
    }

    private static byte[] CreateActivationPayload()
    {
        var startupSettings = StartupSettings.CreateFromCommandLineArguments();
        var startupMessage = AppInstanceStartupMessage.CreateFrom(startupSettings);

        return JsonSerializer.SerializeToUtf8Bytes(startupMessage);
    }

    private static ILoggerFactory CreateFailureLoggerFactory()
    {
        return LoggerFactory.Create((builder) =>
        {
            builder.AddBravoEventLog();
#if DEBUG
            builder.AddDebug();
#endif
        });
    }
}
