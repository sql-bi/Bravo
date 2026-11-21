namespace Bravo.Desktop.Tests.Network;

/// <summary>
/// The update feed of the tests of the network. Its address has a name that only the proxy of a test answers,
/// on the loopback address: a request for it never leaves the machine.
/// </summary>
internal static class TestUpdateFeed
{
    public const string Address = "http://feed.bravo.test/currentversion.json";

    public const string Document = """{"version":"1.0.9805.22885","download":"https://github.com/sql-bi/Bravo/releases/download/v1.0.14/Bravo.1.0.14.x64.msi","changelog":"https://github.com/sql-bi/Bravo/releases"}""";

    /// <summary>
    /// The route of the API that reads the feed.
    /// </summary>
    public const string LatestReleaseRoute = "/releases/latest";
}
