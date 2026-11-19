using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Errors;
using Bravo.Core.Settings;
using Bravo.Core.Updates;
using Microsoft.Extensions.Logging;

namespace Bravo.Platform.Windows.Settings;

/// <summary>
/// The settings of the user in the file that 1.0.x reads and writes as well, read once, at the first use, and
/// replaced whole at every change. Without a file the settings are the defaults, with the choice of telemetry
/// that the installation recorded. A file that cannot be read counts as none, and the next change replaces it.
/// </summary>
public sealed partial class FileUserSettingsStore : IUserSettingsStore, IDisposable
{
    private readonly string _path;
    private readonly IInstallationReader _installationReader;
    private readonly FileReplacer _replacer;
    private readonly ILogger<FileUserSettingsStore> _logger;
    private readonly Lazy<Snapshot> _read;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private volatile Snapshot? _replaced;

    /// <param name="path">The file of the settings.</param>
    /// <param name="installationReader">Where the choice of telemetry of the installation is read.</param>
    /// <param name="replacer">What writes the file.</param>
    /// <param name="logger">Where a file that cannot be read or written is told.</param>
    public FileUserSettingsStore(
        string path,
        IInstallationReader installationReader,
        FileReplacer replacer,
        ILogger<FileUserSettingsStore> logger)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        _path = path;
        _installationReader = installationReader;
        _replacer = replacer;
        _logger = logger;
        _read = new Lazy<Snapshot>(Read);
    }

    public event EventHandler? Changed;

    public UserSettings Current => Latest.Settings;

    private Snapshot Latest => _replaced ?? _read.Value;

    public async Task ReplaceAsync(UserSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var document = Latest.Document.WithSettings(settings);
            try
            {
                await _replacer.ReplaceAsync(_path, document.ToUtf8Bytes(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                LogSaveFailed(exception, _path);
                throw new BravoException(ErrorCode.SettingsSaveFailed, "The settings could not be saved.", exception);
            }

            _replaced = new Snapshot(document, settings);
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _gate.Dispose();
    }

    private Snapshot Read()
    {
        try
        {
            var document = UserSettingsDocument.Parse(File.ReadAllText(_path));
            return new Snapshot(document, document.ToSettings());
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return ReadWithoutFile();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            LogReadFailed(exception, _path);
            return ReadWithoutFile();
        }
    }

    private Snapshot ReadWithoutFile()
    {
        var telemetryEnabled = _installationReader.ReadTelemetryEnabled() ?? UserSettings.Default.TelemetryEnabled;
        var settings = UserSettings.Default with { TelemetryEnabled = telemetryEnabled };

        return new Snapshot(new UserSettingsDocument(), settings);
    }

    /// <param name="Document">The file as it was read or last written, with the members that are no setting.</param>
    /// <param name="Settings">The settings that it holds.</param>
    private sealed record Snapshot(UserSettingsDocument Document, UserSettings Settings);
}
