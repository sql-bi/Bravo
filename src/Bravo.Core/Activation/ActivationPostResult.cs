namespace Bravo.Core.Activation;

public enum ActivationPostResult
{
    Accepted,

    /// <summary>
    /// The queue holds as many requests as it can: the consumer is not reading.
    /// </summary>
    Full,

    /// <summary>
    /// The queue was closed: its consumer is gone.
    /// </summary>
    Closed,
}
