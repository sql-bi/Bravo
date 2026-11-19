using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Errors;

namespace Bravo.Core.Updates;

/// <summary>
/// Tells whether the update feed names a release newer than the build that runs, and which of its files is the
/// update of this copy. A newer release is also told with the beep of the system.
/// </summary>
public sealed class UpdateCheck(
    IReleaseFeed feed,
    IInstallationReader installationReader,
    ISystemBeep beep,
    UpdateCheckOptions options)
{
    private readonly IReleaseFeed _feed = feed;
    private readonly IInstallationReader _installationReader = installationReader;
    private readonly ISystemBeep _beep = beep;
    private readonly UpdateCheckOptions _options = options;

    /// <exception cref="BravoException">
    /// With <see cref="ErrorCode.UpdateCheckFailed"/>: the build has no feed, the feed cannot be read, or what
    /// it holds names no release.
    /// </exception>
    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        var release = await _feed.ReadAsync(cancellationToken).ConfigureAwait(false);

        // Four numbers on both sides: a preview is older than the release of its version.
        var isNewer = release.Version > _options.FileVersion;
        if (isNewer)
        {
            _beep.Play();
        }

        var installation = _installationReader.Read();
        var downloadAddress = UpdateFile.GetAddress(release.Download, installation, _options.IncludesRuntime);

        return new UpdateCheckResult(release.Version, isNewer, downloadAddress, release.ReleaseNotes);
    }
}
