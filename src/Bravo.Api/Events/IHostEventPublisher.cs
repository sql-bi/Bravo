using Bravo.Core.Activation;

namespace Bravo.Api.Events;

/// <summary>
/// Tells the user interface what happened in the host. An event is kept for a page that is not listening
/// yet, and reaches at once a page that waits.
/// </summary>
public interface IHostEventPublisher
{
    /// <summary>
    /// Publishes a start of the application, with what its command line asked for.
    /// </summary>
    void PublishActivation(ActivationRequest request);
}
