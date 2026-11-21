using System;
using System.Net;
using System.Threading.Tasks;
using Bravo.Api.Tests;
using Bravo.Core.Errors;
using Bravo.Desktop.Network;
using Bravo.Platform.Windows.Tests;
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
    private const string WhatTheTestsWrite = "the Internet settings or the credentials of the user";

    private const string UserName = "test-user";
    private const string Password = "test-password";

    // The name under which a development build keeps the credentials: the tests start one.
    private const string CredentialName = NetworkServiceCollectionExtensions.DevelopmentCredentialTargetName;

    private readonly TemporaryFolder _dataFolder = new();

    public void Dispose()
    {
        _dataFolder.Dispose();
    }

    [Fact]
    public async Task GetLatestRelease_ProxyOfTheInternetSettings_ReadsTheFeedThroughIt()
    {
        HostedRunner.SkipElsewhere(WhatTheTestsWrite);
        await using var proxy = new TestProxy { Body = TestUpdateFeed.Document };
        using var settings = InternetSettings.UseProxy(proxy.Address.Port);
        using var bravo = await BravoWithApi.StartAsync(_dataFolder);

        using var response = await bravo.Client.GetAsync(TestUpdateFeed.LatestReleaseRoute, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(proxy.Requests, IsForTheFeed);
    }

    // The first proxy that the script names does not listen. .NET goes on to the next one only for its own
    // proxy of the system, which is why the application puts no proxy of its own in its place.
    [Fact]
    public async Task GetLatestRelease_ScriptWhoseFirstProxyDoesNotListen_GoesOnToTheNextProxy()
    {
        HostedRunner.SkipElsewhere(WhatTheTestsWrite);
        await using var proxy = new TestProxy { Body = TestUpdateFeed.Document };
        var proxies = $"PROXY {TestProxy.ClosedAddress().Authority}; PROXY {proxy.Address.Authority}";
        var script = $$"""function FindProxyForURL(url, host) { return host == "feed.bravo.test" ? "{{proxies}}" : "DIRECT"; }""";
        await using var scriptServer = new TestProxy { Body = script, ContentType = "application/x-ns-proxy-autoconfig" };
        using var settings = InternetSettings.UseScript(new Uri(scriptServer.Address, "proxy.pac"));
        using var bravo = await BravoWithApi.StartAsync(_dataFolder);

        using var response = await bravo.Client.GetAsync(TestUpdateFeed.LatestReleaseRoute, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(proxy.Requests, IsForTheFeed);
    }

    // The credentials are written by a program of Windows, under the name that a development build reads. The
    // application answers the proxy with them, and has none once it forgot them: with no start in between.
    [Fact]
    public async Task GetLatestRelease_ProxyThatAsksAndCredentialsOfTheUser_IsAnsweredWithThemUntilTheyAreForgotten()
    {
        HostedRunner.SkipElsewhere(WhatTheTestsWrite);
        var accepted = new NetworkCredential(UserName, Password);
        await using var proxy = new TestProxy { Body = TestUpdateFeed.Document, Challenges = ["Basic realm=\"test\""], Accepts = accepted };
        await ProxySettingsFile.WriteAsync(_dataFolder, proxy.Address, useDefaultCredentials: false);
        try
        {
            CredentialCommand.Write(CredentialName, UserName, Password);
            using var bravo = await BravoWithApi.StartAsync(_dataFolder);

            using var answered = await bravo.Client.GetAsync(TestUpdateFeed.LatestReleaseRoute, TestContext.Current.CancellationToken);
            using var forgotten = await bravo.Client.DeleteAsync("/network/proxy-credentials", TestContext.Current.CancellationToken);
            using var asked = await bravo.Client.GetAsync(TestUpdateFeed.LatestReleaseRoute, TestContext.Current.CancellationToken);

            var problem = await answered.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(answered.StatusCode == HttpStatusCode.OK, $"{answered.StatusCode}: {problem}");
            Assert.Equal(HttpStatusCode.NoContent, forgotten.StatusCode);
            Assert.Equal(nameof(ErrorCode.ProxyAuthenticationRequired), await asked.ReadProblemCodeAsync());
        }
        finally
        {
            CredentialCommand.Delete(CredentialName);
        }
    }

    private static bool IsForTheFeed(TestProxyRequest request)
    {
        return request.Target.StartsWith(TestUpdateFeed.Address, StringComparison.Ordinal);
    }
}
