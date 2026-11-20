using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Errors;
using Bravo.Core.Policies;

namespace Bravo.Core.Updates;

/// <summary>
/// Tells whether the update feed names a release newer than the build that runs, and which of its files is the
/// update of this copy. A newer release is also told with the beep of the system.
/// </summary>
public sealed class UpdateCheck(
    IReleaseFeed feed,
    IInstallationReader installationReader,
    ISystemBeep beep,
    UpdateCheckOptions options,
    ApplicationPolicies policies)
{
    private readonly IReleaseFeed _feed = feed;
    private readonly IInstallationReader _installationReader = installationReader;
    private readonly ISystemBeep _beep = beep;
    private readonly UpdateCheckOptions _options = options;
    private readonly ApplicationPolicies _policies = policies;

    /// <exception cref="BravoException">
    /// With <see cref="ErrorCode.UpdateCheckDisabled"/>: a policy turned the check off, and nothing is read.
    /// With <see cref="ErrorCode.UpdateCheckFailed"/>: the build has no feed, the feed cannot be read, or what
    /// it holds names no release.
    /// </exception>
    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        // The setting of the user only decides whether the page checks at its start: the policy stops every request.
        if (_policies.UpdateCheckEnabled == false)
        {
            throw new BravoException(ErrorCode.UpdateCheckDisabled, "A policy of the administrator turned the update check off.");
        }

        if (_options.FeedAddress is not { } feedAddress)
        {
            throw new BravoException(ErrorCode.UpdateCheckFailed, "This build reads no update feed.");
        }

        var release = await _feed.ReadAsync(feedAddress, cancellationToken).ConfigureAwait(false);

        var installation = _installationReader.Read();
        var downloadAddress = UpdateFile.GetAddress(release.Download, installation, _options.IncludesRuntime);

        // Four numbers on both sides: a preview is older than the release of its version.
        var isNewer = release.Version > _options.FileVersion;
        if (isNewer)
        {
            _beep.Play();
        }

        return new UpdateCheckResult(release.Version, isNewer, downloadAddress, release.ReleaseNotes);
    }
}
