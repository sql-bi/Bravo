namespace Bravo.Platform.Windows.SingleInstance;

internal enum HandOverStatus
{
    /// <summary>
    /// The owner answered: see <see cref="HandOverResult.Reply"/>.
    /// </summary>
    Answered,

    /// <summary>
    /// No pipe of the owner could be reached in time.
    /// </summary>
    Unreachable,

    /// <summary>
    /// The pipe exists and this process may not use it.
    /// </summary>
    AccessDenied,

    /// <summary>
    /// The owner was reached and did not answer in time.
    /// </summary>
    NoAnswer,
}

internal readonly record struct HandOverResult(HandOverStatus Status, HandOverReply Reply = default)
{
    public bool IsAccepted => Status == HandOverStatus.Answered && Reply == HandOverReply.Accepted;

    /// <summary>
    /// Whether the same request can succeed later: the owner may be starting, busy or ending.
    /// </summary>
    public bool CanRetry
    {
        get
        {
            return Status switch
            {
                HandOverStatus.Unreachable or HandOverStatus.NoAnswer => true,
                HandOverStatus.Answered => Reply is HandOverReply.Busy or HandOverReply.ShuttingDown,
                _ => false,
            };
        }
    }
}
