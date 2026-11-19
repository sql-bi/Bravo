using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Bravo.Platform.Windows.Tests.Updates;

/// <summary>
/// Answers in place of the storage of the feed: no request of a test leaves the process.
/// </summary>
internal sealed class FeedHandler(Func<CancellationToken, Task<HttpResponseMessage>> answer) : HttpMessageHandler
{
    private readonly Func<CancellationToken, Task<HttpResponseMessage>> _answer = answer;

    public Uri? RequestedAddress { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestedAddress = request.RequestUri;

        return _answer(cancellationToken);
    }
}
