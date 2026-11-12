using System.Globalization;

namespace Bravo.Core.Application;

/// <summary>
/// What the running application is: its version, the culture of its user and the kind of its build. The
/// values do not change while the application runs.
/// </summary>
public sealed record ApplicationInfo
{
    /// <summary>
    /// The version as a user reads it, such as <c>1.1.0</c> or <c>1.1.0-beta.1</c>.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// The culture of the user, by which the user interface chooses its language and its formats.
    /// </summary>
    public required CultureInfo Culture { get; init; }

    /// <summary>
    /// Whether the build is a development build, and not one of the release pipeline.
    /// </summary>
    public required bool IsDevelopmentBuild { get; init; }
}
