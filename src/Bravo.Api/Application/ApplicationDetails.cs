using Bravo.Core.Application;

namespace Bravo.Api.Application;

// Public, as every type that travels: the validation and the descriptions of the document are generated
// for public types only.
/// <summary>
/// What the running application is. The values do not change while the application runs.
/// </summary>
public sealed record ApplicationDetails
{
    /// <summary>
    /// The version of the application as a user reads it, such as <c>1.1.0</c> or <c>1.1.0-beta.1</c>.
    /// </summary>
    public required string Version { get; init; }

    public required ApplicationCulture Culture { get; init; }

    /// <summary>
    /// Whether the build is a development build, and not one of the release pipeline.
    /// </summary>
    public required bool IsDevelopmentBuild { get; init; }

    internal static ApplicationDetails FromInfo(ApplicationInfo info)
    {
        return new ApplicationDetails
        {
            Version = info.Version,
            Culture = new ApplicationCulture
            {
                IetfLanguageTag = info.Culture.IetfLanguageTag,
                TwoLetterISOLanguageName = info.Culture.TwoLetterISOLanguageName,
            },
            IsDevelopmentBuild = info.IsDevelopmentBuild,
        };
    }
}
