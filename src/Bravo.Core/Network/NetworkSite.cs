namespace Bravo.Core.Network;

/// <summary>
/// A site that the application needs to reach, as the connection test names it.
/// </summary>
public enum NetworkSite
{
    /// <summary>
    /// The sign-in of Microsoft, where an account of the Power BI service signs in.
    /// </summary>
    SignIn,

    /// <summary>
    /// The Power BI service.
    /// </summary>
    PowerBIService,

    /// <summary>
    /// The DAX Formatter service.
    /// </summary>
    DaxFormatter,

    /// <summary>
    /// The update feed of the application.
    /// </summary>
    UpdateFeed,
}
