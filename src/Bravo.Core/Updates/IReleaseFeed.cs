using System;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Errors;

namespace Bravo.Core.Updates;

public interface IReleaseFeed
{
    /// <summary>
    /// Reads the release that the update feed at an address names.
    /// </summary>
    /// <exception cref="BravoException">
    /// With <see cref="ErrorCode.UpdateCheckFailed"/>: the feed cannot be read, or what it holds names no
    /// release.
    /// </exception>
    Task<PublishedRelease> ReadAsync(Uri address, CancellationToken cancellationToken);
}
