using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Bravo.Desktop.Tests.Network;

/// <summary>
/// The executable, started with an update feed of the test, and a client of its API: the application writes
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
    /// <param name="feedAddress">The update feed that the application reads.</param>
    /// <param name="configure">Changes the start, such as the environment of the process.</param>
    public static async Task<BravoWithApi> StartAsync(TemporaryFolder dataFolder, string feedAddress, Action<ProcessStartInfo> configure)
    {
        var connectionFile = dataFolder.Combine(ConnectionFileName);
        var start = BravoExecutable.CreateStartInfo(
            dataFolder.Path,
            $"--dev-update-feed={feedAddress}",
            $"--dev-connection-file={connectionFile}");

        // The proxy variables of the process that runs the tests do not reach the application.
        foreach (var name in new[] { "HTTP_PROXY", "HTTPS_PROXY", "ALL_PROXY", "NO_PROXY" })
        {
            start.Environment.Remove(name);
        }

        configure(start);

        var process = Process.Start(start) ?? throw new InvalidOperationException("Bravo did not start.");
        try
        {
            var client = await ConnectAsync(process, connectionFile);

            return new BravoWithApi(process, client);
        }
        catch
        {
            BravoExecutable.Kill(process);
            process.Dispose();
            throw;
        }
    }

    /// <summary>
    /// The code of a problem that the API answered with 409.
    /// </summary>
    public static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        return problem.GetProperty("code").GetString();
    }

    // The tests of the network do not wait for the window nor for the end of the application, which other
    // tests prove: the process is ended with the test.
    public void Dispose()
    {
        Client.Dispose();
        BravoExecutable.Kill(_process);
        _process.Dispose();
    }

    // The application writes the file once its host has started, before its window is shown.
    private static async Task<HttpClient> ConnectAsync(Process process, string connectionFile)
    {
        var start = Stopwatch.GetTimestamp();
        while (!File.Exists(connectionFile))
        {
            if (process.HasExited)
            {
                // A start that ends at once with success handed itself over to a development build that
                // already runs for this user.
                Assert.SkipWhen(process.ExitCode == 0, "A development build of Bravo is running.");
                Assert.Fail($"Bravo ended with exit code {process.ExitCode} before it wrote the address of its API.");
            }

            var waited = Stopwatch.GetElapsedTime(start);
            Assert.True(waited < BravoExecutable.StartTimeout, "Bravo did not write the address of its API in time.");
            await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);
        }

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
