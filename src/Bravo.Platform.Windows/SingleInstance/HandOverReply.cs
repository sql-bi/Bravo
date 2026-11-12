namespace Bravo.Platform.Windows.SingleInstance;

/// <summary>
/// The answer of the owner to a request, one byte on the pipe. Zero is not an answer.
/// </summary>
public enum HandOverReply : byte
{
    Accepted = 1,

    /// <summary>
    /// The owner cannot take the request now: the same request can be sent again.
    /// </summary>
    Busy = 2,

    /// <summary>
    /// The owner is ending: the instance will soon be free.
    /// </summary>
    ShuttingDown = 3,

    UnsupportedVersion = 4,

    PayloadTooLarge = 5,

    InvalidRequest = 6,
}
