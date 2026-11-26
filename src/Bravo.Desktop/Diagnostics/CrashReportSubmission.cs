using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Network;
using Bravo.Core.Policies;
using Bravo.Core.Settings;
using Bravo.Core.Telemetry;
using Bravo.Desktop.Network;
using Bravo.Desktop.Policies;
using Bravo.Desktop.Settings;
using Bravo.Desktop.Telemetry;
using Bravo.Desktop.WebView;
using Bravo.Platform.Windows.Network;
using Bravo.Platform.Windows.Policies;
using Bravo.Platform.Windows.Settings;
using Bravo.Platform.Windows.Updates;
using Microsoft.Extensions.Logging.Abstractions;
using ExceptionTelemetry = Microsoft.ApplicationInsights.DataContracts.ExceptionTelemetry;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// Sends the crash of a report with the telemetry, as an exception of the run that crashed rebuilt from the
/// report: by itself when a policy or the option of the user turns telemetry on, at the request of the user when
/// the option is off, never when a policy turns it off. It reads the policies and the settings, and writes
/// neither.
/// </summary>
internal sealed class CrashReportSubmission
{
    /// <summary>
    /// The property that tells a crash from the other exceptions that the host sends.
    /// </summary>
    internal const string KindPropertyName = "ExceptionKind";

    internal const string UnhandledKind = "Unhandled";

    // How long a send waits for the endpoint, or for the store.
    private static readonly TimeSpan s_sendTimeout = TimeSpan.FromSeconds(5);

    private readonly Destination? _destination;
    private readonly Lock _gate = new();
    private Task<TelemetryDelivery>? _sending;

    private CrashReportSubmission(CrashReportSubmissionMode mode, Destination? destination = null)
    {
        Mode = mode;
        _destination = destination;
    }

    public CrashReportSubmissionMode Mode { get; }

    /// <summary>
    /// What the reporter does with the telemetry of the report, as the build, the policies and the settings of
    /// the process that crashed decide.
    /// </summary>
    public static CrashReportSubmission For(CrashReport report, CrashReporterStart start, BuildIdentity identity)
    {
        // In the process of the reporter alone.
        return For(
            report,
            start,
            identity,
            static (settings, credentials) => ProxySelection.For(settings).ApplyToProcess(credentials));
    }

    /// <param name="report">The report.</param>
    /// <param name="start">The command line of the reporter.</param>
    /// <param name="identity">The build, the one of the process that crashed.</param>
    /// <param name="prepareNetwork">What gives the process the proxy of the settings, with its credentials.</param>
    internal static CrashReportSubmission For(
        CrashReport report,
        CrashReporterStart start,
        BuildIdentity identity,
        Action<ProxySettings, ICredentials> prepareNetwork)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(identity);

        var dataFolder = DataFolder.For(identity, start.Development);
        var options = TelemetryOptions.For(identity, dataFolder, start.Development.TelemetryEndpoint);
        if (options.ConnectionString is not { } connectionString)
        {
            return new CrashReportSubmission(CrashReportSubmissionMode.NoTelemetry);
        }

        if (report.Session is not { } session || report.Exception.Chain is not { } chain)
        {
            return new CrashReportSubmission(CrashReportSubmissionMode.EarlierFormat);
        }

        ApplicationPolicies policies;
        try
        {
            policies = RegistryPolicyReader.Read(PolicyKeys.For(identity, start.Development));
        }
        catch (PolicyReadException)
        {
            return new CrashReportSubmission(CrashReportSubmissionMode.PoliciesUnreadable);
        }

        if (policies.TelemetryEnabled == false)
        {
            return new CrashReportSubmission(CrashReportSubmissionMode.Forbidden);
        }

        // As the host reads them: without a file, the choice of the installation. A file that cannot be read gives
        // no consent of the user, and stays as it is.
        var installation = new RegistryInstallationReader(AppContext.BaseDirectory);
        var settings = new FileUserSettingsStore(
            SettingsServiceCollectionExtensions.GetFilePath(dataFolder),
            installation,
            new FileReplacer(TimeProvider.System),
            NullLogger<FileUserSettingsStore>.Instance);
        var consent = TelemetryConsent.Of(policies, settings.Current);
        var isOn = consent.IsGiven && !(consent.Source == TelemetryConsentSource.Option && settings.ReadFailed);
        var credentials = new ProxyCredentials(
            settings,
            new WindowsProxyCredentialStore(
                NetworkServiceCollectionExtensions.GetCredentialTargetName(identity),
                NullLogger<WindowsProxyCredentialStore>.Instance));

        var destination = new Destination(
            connectionString,
            options.StorageFolder,
            () => CreateItem(report, chain, TelemetryContext.Create(
                identity, new TelemetrySession(session), installation.Read(), WebViewRuntime.GetVersion())),
            () => prepareNetwork(settings.Current.Proxy, credentials));

        return new CrashReportSubmission(
            isOn ? CrashReportSubmissionMode.Automatic : CrashReportSubmissionMode.OnRequest,
            destination);
    }

    // The exception of the chain of the report, with the time and the trace of the crash.
    private static ExceptionTelemetry CreateItem(
        CrashReport report,
        IReadOnlyList<ExceptionLink> chain,
        TelemetryContext context)
    {
        var item = ExceptionTelemetryFactory.Create(chain, context, report.Trace);
        item.Timestamp = report.Time;
        item.Properties[KindPropertyName] = UnhandledKind;

        return item;
    }

    /// <summary>
    /// Sends the crash once, on a thread of the pool, and waits for the endpoint at most 5 seconds. A later call
    /// returns that send, whatever its end: a second send would find the crash of the first in the store as well,
    /// and send both. It never throws.
    /// </summary>
    /// <exception cref="InvalidOperationException">The mode sends nothing.</exception>
    public Task<TelemetryDelivery> SendAsync()
    {
        if (_destination is not { } destination)
        {
            throw new InvalidOperationException($"A report in the mode {Mode} is not sent.");
        }

        lock (_gate)
        {
            var onRequest = Mode == CrashReportSubmissionMode.OnRequest;

            return _sending ??= Task.Run(() => SendNowAsync(destination, onRequest));
        }
    }

    /// <summary>
    /// Waits, for the time given at most, for a send that has not ended.
    /// </summary>
    public void WaitForSending(TimeSpan timeout)
    {
        Task? sending;
        lock (_gate)
        {
            sending = _sending;
        }

        sending?.Wait(timeout);
    }

    private static async Task<TelemetryDelivery> SendNowAsync(Destination destination, bool onRequest)
    {
        try
        {
            // Before each send: the SDK takes the default proxy of the process at its first one.
            destination.PrepareNetwork();
            using var pipeline = TelemetryPipeline.Create(destination.ConnectionString, destination.StorageFolder);
            var delivery = await pipeline.SendAsync(destination.CreateItem(), s_sendTimeout).ConfigureAwait(false);

            // Send is offered only while telemetry is off, and the next start with it off deletes the store: what
            // waits there is not sent later.
            return onRequest && delivery == TelemetryDelivery.Kept ? TelemetryDelivery.NotSent : delivery;
        }
        catch (Exception)
        {
            // Whatever fails, from the proxy to the disk, the dialog says that nothing was sent.
            return TelemetryDelivery.NotSent;
        }
    }

    /// <param name="ConnectionString">Where the telemetry of the build goes.</param>
    /// <param name="StorageFolder">The store of the telemetry, where what is not sent waits.</param>
    /// <param name="CreateItem">The item of the crash.</param>
    /// <param name="PrepareNetwork">What gives the process the proxy of the settings.</param>
    private sealed record Destination(
        string ConnectionString,
        string StorageFolder,
        Func<ExceptionTelemetry> CreateItem,
        Action PrepareNetwork);
}
