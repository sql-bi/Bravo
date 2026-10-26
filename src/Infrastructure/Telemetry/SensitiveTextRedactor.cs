namespace Sqlbi.Bravo.Infrastructure.Telemetry;

/// <summary>
/// Redacts text that leaves the machine: the content of the <see cref="SensitiveTags"/>, then the folders of the user
/// profile.
/// </summary>
internal sealed class SensitiveTextRedactor(UserProfilePathRedactor profilePaths)
{
    private readonly UserProfilePathRedactor _profilePaths = profilePaths;

    public static SensitiveTextRedactor CreateForCurrentUser() => new(UserProfilePathRedactor.CreateForCurrentUser());

    public string Redact(string? text) => _profilePaths.Redact(SensitiveTags.Redact(text));
}
