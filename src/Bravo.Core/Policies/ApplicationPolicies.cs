namespace Bravo.Core.Policies;

/// <summary>
/// What an administrator set for the application, read once at the start. A member without a value is a
/// policy that is not configured: the choice of the user applies. A policy applies over the settings of the
/// user and is never written in them.
/// </summary>
public sealed record ApplicationPolicies
{
    /// <summary>
    /// No policy configured, as for a development build that is given none.
    /// </summary>
    public static ApplicationPolicies None { get; } = new();

    public bool? TelemetryEnabled { get; init; }

    public bool? UpdateCheckEnabled { get; init; }

    public bool? UseSystemBrowserForAuthentication { get; init; }

    public bool? BuiltInTemplatesEnabled { get; init; }

    public bool? CustomTemplatesEnabled { get; init; }

    /// <summary>
    /// The folder of the templates of the organization, as the administrator wrote it: environment variables
    /// are not expanded.
    /// </summary>
    public string? CustomTemplatesOrganizationRepositoryPath { get; init; }
}
