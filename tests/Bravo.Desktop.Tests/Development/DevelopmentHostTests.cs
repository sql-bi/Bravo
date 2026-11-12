using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Bravo.Api.Security;
using Bravo.Api.Server;
using Bravo.Desktop.Development;
using Bravo.Desktop.Diagnostics;
using Bravo.Desktop.Hosting;
using Bravo.Desktop.Tests.SingleInstance;
using Bravo.Platform.Windows.SingleInstance;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Bravo.Desktop.Tests.Development;

/// <summary>
/// The host of a development build that was started for a browser: with one more origin, and with a file
/// for the address and the token of its API.
/// </summary>
public sealed class DevelopmentHostTests : IAsyncLifetime
{
    private const string DevelopmentOrigin = "http://127.0.0.1:8098";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "Bravo.Tests." + Guid.NewGuid().ToString("N"));
    private readonly OwnedInstance _instance = new();
    private readonly WebApplication _host;

    public DevelopmentHostTests()
    {
        var development = new DevelopmentOptions
        {
            AllowedOrigin = DevelopmentOrigin,
            ConnectionFilePath = ConnectionFilePath,
        };
        var identity = BuildIdentity.Development("1.1.0");
        var fatalExceptionHandler = Substitute.For<IFatalExceptionHandler>();
        _host = DesktopHost.Build(identity, development, _instance.Ownership, new SingleInstanceOptions(), fatalExceptionHandler);
    }

    // In a folder that does not exist yet: the host creates it.
    private string ConnectionFilePath => Path.Combine(_folder, "dev", "host.json");

    public async ValueTask InitializeAsync()
    {
        await _host.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _host.DisposeAsync();
        _instance.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public async Task Start_ConnectionFile_HoldsTheAddressAndTheTokenOfTheApi()
    {
        var text = await File.ReadAllTextAsync(ConnectionFilePath, TestContext.Current.CancellationToken);

        using var connection = JsonDocument.Parse(text);
        var address = _host.Services.GetRequiredService<IApiAddressProvider>().GetAddress();
        var token = _host.Services.GetRequiredService<ApiToken>().Value;
        Assert.Equal(address.GetLeftPart(UriPartial.Authority), connection.RootElement.GetProperty("address").GetString());
        Assert.Equal(token, connection.RootElement.GetProperty("token").GetString());
    }

    [Fact]
    public async Task Stop_ConnectionFile_IsDeleted()
    {
        await _host.StopAsync(TestContext.Current.CancellationToken);

        Assert.False(File.Exists(ConnectionFilePath));
    }

    [Theory]
    [InlineData(DevelopmentOrigin)]
    [InlineData("https://bravo.invalid")]
    public async Task Preflight_FromTheDevelopmentOriginAndFromTheOriginOfThePage_IsAllowed(string origin)
    {
        using var response = await SendPreflightAsync(origin);

        Assert.Equal(origin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Preflight_FromAnotherOrigin_IsNotAllowed()
    {
        using var response = await SendPreflightAsync("http://127.0.0.1:8099");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private async Task<HttpResponseMessage> SendPreflightAsync(string origin)
    {
        using var handler = new SocketsHttpHandler { UseProxy = false };
        using var client = new HttpClient(handler);
        var address = _host.Services.GetRequiredService<IApiAddressProvider>().GetAddress();
        using var request = new HttpRequestMessage(HttpMethod.Options, new Uri(address, "/options"));
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization");

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
