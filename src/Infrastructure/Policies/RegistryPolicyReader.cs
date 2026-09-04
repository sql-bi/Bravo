using Sqlbi.Bravo.Infrastructure.Configuration.Settings;

namespace Sqlbi.Bravo.Infrastructure.Policies;

internal sealed class RegistryPolicyReader(IPolicySourceFactory sourceFactory) : IPolicyReader
{
    private readonly IPolicySourceFactory _sourceFactory = sourceFactory;

    public PolicySnapshot Read()
    {
        using var machineSource = _sourceFactory.Open(PolicyScope.Computer);
        var machinePolicies = FromSource(machineSource);

        using var userSource = _sourceFactory.Open(PolicyScope.User);
        var userPolicies = FromSource(userSource);

        return Merge(machinePolicies, userPolicies);
    }

    internal static PolicySnapshot FromSource(IPolicySource source) => new(
        TelemetryEnabled: source.GetBool("TelemetryEnabled"),
        UpdateChannel: source.GetEnum<UpdateChannelType>("UpdateChannel"),
        UpdateCheckEnabled: source.GetBool("UpdateCheckEnabled"),
        UseSystemBrowserForAuthentication: source.GetBool("UseSystemBrowserForAuthentication"),
        BuiltInTemplatesEnabled: source.GetBool("BuiltInTemplatesEnabled"),
        CustomTemplatesEnabled: source.GetBool("CustomTemplatesEnabled"),
        CustomTemplatesOrganizationRepositoryPath: source.GetString("CustomTemplatesOrganizationRepositoryPath"));

    internal static PolicySnapshot Merge(PolicySnapshot machinePolicies, PolicySnapshot userPolicies)
    {
        // LocalMachine takes precedence over CurrentUser when both are configured
        return new PolicySnapshot(
            TelemetryEnabled: machinePolicies.TelemetryEnabled ?? userPolicies.TelemetryEnabled,
            UpdateChannel: machinePolicies.UpdateChannel ?? userPolicies.UpdateChannel,
            UpdateCheckEnabled: machinePolicies.UpdateCheckEnabled ?? userPolicies.UpdateCheckEnabled,
            UseSystemBrowserForAuthentication: machinePolicies.UseSystemBrowserForAuthentication ?? userPolicies.UseSystemBrowserForAuthentication,
            BuiltInTemplatesEnabled: machinePolicies.BuiltInTemplatesEnabled ?? userPolicies.BuiltInTemplatesEnabled,
            CustomTemplatesEnabled: machinePolicies.CustomTemplatesEnabled ?? userPolicies.CustomTemplatesEnabled,
            CustomTemplatesOrganizationRepositoryPath: machinePolicies.CustomTemplatesOrganizationRepositoryPath ?? userPolicies.CustomTemplatesOrganizationRepositoryPath);
    }
}
