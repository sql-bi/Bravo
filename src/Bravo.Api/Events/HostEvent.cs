namespace Bravo.Api.Events;

/// <summary>
/// Something that happened in the host and that the user interface is told about. One type for every kind
/// of event: <c>kind</c> names the kind, and the member with the data of that kind is the one present.
/// </summary>
internal sealed record HostEvent
{
    /// <summary>
    /// The position of the event among the events of this process: the first is 1, and none is skipped.
    /// </summary>
    public required int Number { get; init; }

    public required HostEventKind Kind { get; init; }

    /// <summary>
    /// Present when <c>kind</c> is <c>Activation</c>.
    /// </summary>
    public ActivationEvent? Activation { get; init; }
}
