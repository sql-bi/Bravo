using System;
using System.Collections.Generic;
using System.Threading;

namespace Bravo.Core.Activation;

/// <summary>
/// The queue of the activations, their one path. Whoever receives a start adds a request; a single consumer
/// reads the requests in the order they were added. A request added before the consumer exists waits for it.
/// </summary>
public interface IActivationChannel
{
    /// <summary>
    /// Adds a request without waiting. A request that is not accepted is not kept.
    /// </summary>
    ActivationPostResult Post(ActivationRequest request);

    /// <summary>
    /// The requests, as they arrive, until the queue is closed and empty.
    /// </summary>
    /// <exception cref="InvalidOperationException">The queue already has its consumer.</exception>
    IAsyncEnumerable<ActivationRequest> ReadAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Closes the queue: from now on no request is accepted.
    /// </summary>
    void Complete();
}
