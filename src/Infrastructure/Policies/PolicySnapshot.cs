using Sqlbi.Bravo.Infrastructure.Configuration.Settings;

namespace Sqlbi.Bravo.Infrastructure.Policies;

/// <summary>
/// Contains the effective policy values from one read of the configured sources.
/// </summary>
/// <remarks>
/// Null means not configured; false and zero are configured values.
/// </remarks>
public sealed record PolicySnapshot(
    bool? TelemetryEnabled,
    UpdateChannelType? UpdateChannel,
    bool? UpdateCheckEnabled,
    bool? UseSystemBrowserForAuthentication,
    bool? BuiltInTemplatesEnabled,
    bool? CustomTemplatesEnabled,
    string? CustomTemplatesOrganizationRepositoryPath)
{
    public static PolicySnapshot NotConfigured { get; } = new(null, null, null, null, null, null, null);
}
