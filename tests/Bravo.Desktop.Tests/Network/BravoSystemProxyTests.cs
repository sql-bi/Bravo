using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using Bravo.Desktop.Network;
using Bravo.Platform.Windows.Tests.Network;
using Xunit;

namespace Bravo.Desktop.Tests.Network;

/// <summary>
/// The executable with what only the machine can give it: a proxy and a script in the Internet settings of the
/// user, and credentials in the Windows Credential Manager. The tests write them on a runner of GitHub
/// Actions only, which is thrown away after the run, and elsewhere they are skipped: no test changes the
/// settings or the credentials of a developer. The application starts after they are written.
/// </summary>
[Collection(BravoExecutable.Collection)]
public sealed class BravoSystemProxyTests : IDisposable
{
    private const string UserName = "test-user";
    private const string Password = "test-password";

    private readonly TemporaryFolder _dataFolder = new();

    public void Dispose()
    {
        _dataFolder.Dispose();
    }

    [Fact]
    public async Task GetLatestRelease_ProxyOfTheInternetSettings_ReadsTheFeedThroughIt()
    {
        SkipOffTheRunner();
        await using var proxy = new TestProxy { Body = BravoNetworkTests.Feed };
        using var settings = InternetSettings.UseProxy(proxy.Address.Port);
        using var bravo = await StartAsync();

        using var response = await bravo.Client.GetAsync(BravoNetworkTests.LatestRelease, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(proxy.Requests, IsForTheFeed);
    }

    // The first proxy that the script names does not listen. .NET goes on to the next one only for its own
    // proxy of the system, which is why the application puts no proxy of its own in its place.
    [Fact]
    public async Task GetLatestRelease_ScriptWhoseFirstProxyDoesNotListen_GoesOnToTheNextProxy()
    {
        SkipOffTheRunner();
        await using var proxy = new TestProxy { Body = BravoNetworkTests.Feed };
        var proxies = $"PROXY 127.0.0.1:{ClosedPort()}; PROXY 127.0.0.1:{proxy.Address.Port}";
        var script = $$"""function FindProxyForURL(url, host) { return host == "feed.bravo.test" ? "{{proxies}}" : "DIRECT"; }""";
        await using var scriptServer = new TestProxy { Body = script, ContentType = "application/x-ns-proxy-autoconfig" };
        using var settings = InternetSettings.UseScript(new Uri(scriptServer.Address, "proxy.pac"));
        using var bravo = await StartAsync();

        using var response = await bravo.Client.GetAsync(BravoNetworkTests.LatestRelease, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(proxy.Requests, IsForTheFeed);
    }

    // The credentials are written by a program of Windows, under the name that a development build reads.
    [Fact]
    public async Task GetLatestRelease_ProxyThatAsksAndCredentialsOfTheUser_IsAnsweredWithTheKeptCredentials()
    {
        SkipOffTheRunner();
        var accepted = new NetworkCredential(UserName, Password);
        await using var proxy = new TestProxy { Body = BravoNetworkTests.Feed, Challenges = ["Basic realm=\"test\""], Accepts = accepted };
        var settings = new
        {
            proxy = new { type = 2, useDefaultCredentials = false, address = proxy.Address.AbsoluteUri, bypassOnLocal = false },
        };
        await File.WriteAllTextAsync(
            _dataFolder.Combine("usersettings.json"),
            JsonSerializer.Serialize(settings),
            TestContext.Current.CancellationToken);
        var targetName = NetworkServiceCollectionExtensions.DevelopmentCredentialTargetName;
        try
        {
            RunCredentialCommand($"/generic:{targetName}", $"/user:{UserName}", $"/pass:{Password}");
            using var bravo = await StartAsync();

            using var response = await bravo.Client.GetAsync(BravoNetworkTests.LatestRelease, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(proxy.Requests, request => request.AuthorizationScheme == "Basic");
        }
        finally
        {
            RunCredentialCommand($"/delete:{targetName}");
        }
    }

    private static void SkipOffTheRunner()
    {
        Assert.SkipUnless(
            Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true",
            "It writes the Internet settings or the credentials of the user: it runs on a runner of GitHub Actions only.");
    }

    private Task<BravoWithApi> StartAsync()
    {
        return BravoWithApi.StartAsync(_dataFolder, BravoNetworkTests.FeedAddress, _ => { });
    }

    private static bool IsForTheFeed(TestProxyRequest request)
    {
        return request.Target.StartsWith(BravoNetworkTests.FeedAddress, StringComparison.Ordinal);
    }

    private static int ClosedPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        return port;
    }

    private static void RunCredentialCommand(params string[] arguments)
    {
        var start = new ProcessStartInfo(Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\cmdkey.exe"), arguments)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
        };
        using var process = Process.Start(start);
        Assert.NotNull(process);
        process.WaitForExit();
    }
}
