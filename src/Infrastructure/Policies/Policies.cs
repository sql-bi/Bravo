namespace Sqlbi.Bravo.Infrastructure.Policies
{
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;

    internal interface IPolicies
    {
        bool? TelemetryEnabled { get; }
        UpdateChannelType? UpdateChannel { get; }
        bool? UpdateCheckEnabled { get; }
        bool? UseSystemBrowserForAuthentication { get; }
        bool? BuiltInTemplatesEnabled { get; }
        bool? CustomTemplatesEnabled { get; }
        string? CustomTemplatesOrganizationRepositoryPath { get; }
    }

    /// <summary>
    /// Immutable snapshot of Bravo's effective policy values. Pure data - see
    /// <see cref="PoliciesFactory"/> for how instances are read from the registry, parsed,
    /// and merged with LocalMachine/CurrentUser precedence.
    /// </summary>
    internal sealed record Policies(
        bool? TelemetryEnabled,
        UpdateChannelType? UpdateChannel,
        bool? UpdateCheckEnabled,
        bool? UseSystemBrowserForAuthentication,
        bool? BuiltInTemplatesEnabled,
        bool? CustomTemplatesEnabled,
        string? CustomTemplatesOrganizationRepositoryPath) : IPolicies;
}
