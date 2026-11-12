using System;
using Bravo.Core.Activation;

namespace Bravo.Api.Events;

internal sealed class HostEventPublisher(EventLog log) : IHostEventPublisher
{
    private readonly EventLog _log = log;

    public void PublishActivation(ActivationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var activation = ActivationEvent.FromRequest(request);
        _log.Append(number => new HostEvent
        {
            Number = number,
            Kind = HostEventKind.Activation,
            Activation = activation,
        });
    }
}
