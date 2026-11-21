using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Bravo.Desktop.Tests.Network;

/// <summary>
/// The executable, started with the update feed of the tests, and a client of its API: the application writes
/// the address and the token of the API in a file of the data folder of the test.
/// </summary>
internal sealed class BravoWithApi : IDisposable
{
    private const string ConnectionFileName = "connection.json";

    private readonly Process _process;

    private BravoWithApi(Process process, HttpClient client)
    {
        _process = process;
        Client = client;
    }

    public HttpClient Client { get; }

    /// <param name="dataFolder">The data folder of the test.</param>
    /// <param name="systemProxy">
    /// The proxy of the system for the application, which .NET takes from the variable <c>HTTP_PROXY</c> of
    /// the process before it looks at Windows. With none, the application has the one of the machine.
    /// </param>
    public static async Task<BravoWithApi> StartAsync(TemporaryFolder dataFolder, Uri? systemProxy = null)
    {
        var connectionFile = dataFolder.Combine(ConnectionFileName);
        var start = BravoExecutable.CreateStartInfo(
            dataFolder.Path,
            $"--dev-update-feed={TestUpdateFeed.Address}",
            $"--dev-connection-file={connectionFile}");
        if (systemProxy is not null)
        {
            start.Environment["HTTP_PROXY"] = systemProxy.AbsoluteUri;
        }

        var process = Process.Start(start) ?? throw new InvalidOperationException("Bravo did not start.");
        try
        {
            // The application writes the file once its host has started, before its window is shown.
            await BravoExecutable.WaitForStartAsync(process, () => File.Exists(connectionFile), "write the address of its API");
            var client = await ConnectAsync(connectionFile);

            return new BravoWithApi(process, client);
        }
        catch
        {
            BravoExecutable.Kill(process);
            process.Dispose();
            throw;
        }
    }

    // The tests of the network do not wait for the window nor for the end of the application, which other
    // tests prove: the process is ended with the test.
    public void Dispose()
    {
        Client.Dispose();
        BravoExecutable.Kill(_process);
        _process.Dispose();
    }

    private static async Task<HttpClient> ConnectAsync(string connectionFile)
    {
        var text = await File.ReadAllTextAsync(connectionFile, TestContext.Current.CancellationToken);
        using var connection = JsonDocument.Parse(text);
        var address = connection.RootElement.GetProperty("address").GetString();
        var token = connection.RootElement.GetProperty("token").GetString();
        Assert.NotNull(address);

        // No proxy: the request goes to the socket of the application, whatever the settings of the machine.
        var client = new HttpClient(new SocketsHttpHandler { UseProxy = false }, disposeHandler: true)
        {
            BaseAddress = new Uri(address),
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}
