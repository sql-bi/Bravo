using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Api.Security;
using Bravo.Api.Server;
using Microsoft.Extensions.Hosting;

namespace Bravo.Desktop.Development;

/// <summary>
/// Writes the address and the token of the API to the file that the command line of a development build
/// named, once the host has started, and deletes the file when the host stops. The development server reads
/// it to give a page in a browser the object that the webview gives the page in the window.
/// </summary>
internal sealed class ConnectionFileWriter(
    DevelopmentOptions options,
    IApiAddressProvider apiAddress,
    ApiToken apiToken) : IHostedLifecycleService
{
    private readonly DevelopmentOptions _options = options;
    private readonly IApiAddressProvider _apiAddress = apiAddress;
    private readonly ApiToken _apiToken = apiToken;

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    // After the start of every service: the server has its port only then.
    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        if (_options.ConnectionFilePath is not { } path)
        {
            return;
        }

        var connection = new
        {
            address = _apiAddress.GetAddress().GetLeftPart(UriPartial.Authority),
            token = _apiToken.Value,
        };

        // Written beside the file and then moved: a reader finds the whole file or none.
        var folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var temporaryPath = path + ".tmp";
        await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(connection), cancellationToken)
            .ConfigureAwait(false);
        File.Move(temporaryPath, path, overwrite: true);
    }

    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    // The token ends with the process: the file would give a reader a host that is no longer there.
    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        if (_options.ConnectionFilePath is { } path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // The file is in use or gone: nothing depends on its deletion.
            }
        }

        return Task.CompletedTask;
    }
}
