using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Errors;

namespace Bravo.Core.Updates;

public interface IReleaseFeed
{
    /// <summary>
    /// Reads the release that the update feed names.
    /// </summary>
    /// <exception cref="BravoException">
    /// With <see cref="ErrorCode.UpdateCheckFailed"/>: the build has no feed, the feed cannot be read, or what
    /// it holds names no release.
    /// </exception>
    Task<PublishedRelease> ReadAsync(CancellationToken cancellationToken);
}
