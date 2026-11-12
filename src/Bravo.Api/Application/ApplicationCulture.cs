namespace Bravo.Api.Application;

/// <summary>
/// The culture of the user, by which the user interface chooses its language and its formats.
/// </summary>
public sealed record ApplicationCulture
{
    /// <summary>
    /// The language tag of the culture, such as <c>en-US</c>.
    /// </summary>
    public required string IetfLanguageTag { get; init; }

    /// <summary>
    /// The two letters of the language of the culture, such as <c>en</c>.
    /// </summary>
    public required string TwoLetterISOLanguageName { get; init; }
}
