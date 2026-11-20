using Bravo.Core.Policies;

namespace Bravo.Api.Policies;

// Public, as every type that travels: the validation and the descriptions of the document are generated
// for public types only.
/// <summary>
/// The policies that an administrator set for the application, read when it started. A member that is absent is
/// a policy that is not configured: the option of the user applies. A member that is present is the value of the
/// option, which the user cannot change.
/// </summary>
public sealed record PolicyDetails
{
    public bool? TelemetryEnabled { get; init; }

    /// <summary>
    /// With <c>false</c> the host makes no request to the update feed: <c>GET /releases/latest</c> answers the
    /// error <c>UpdateCheckDisabled</c>.
    /// </summary>
    public bool? UpdateCheckEnabled { get; init; }

    public bool? UseSystemBrowserForAuthentication { get; init; }

    public bool? BuiltInTemplatesEnabled { get; init; }

    public bool? CustomTemplatesEnabled { get; init; }

    /// <summary>
    /// The folder of the templates of the organization, as the administrator wrote it.
    /// </summary>
    public string? CustomTemplatesOrganizationRepositoryPath { get; init; }

    internal static PolicyDetails FromPolicies(ApplicationPolicies policies)
    {
        return new PolicyDetails
        {
            TelemetryEnabled = policies.TelemetryEnabled,
            UpdateCheckEnabled = policies.UpdateCheckEnabled,
            UseSystemBrowserForAuthentication = policies.UseSystemBrowserForAuthentication,
            BuiltInTemplatesEnabled = policies.BuiltInTemplatesEnabled,
            CustomTemplatesEnabled = policies.CustomTemplatesEnabled,
            CustomTemplatesOrganizationRepositoryPath = policies.CustomTemplatesOrganizationRepositoryPath,
        };
    }
}
