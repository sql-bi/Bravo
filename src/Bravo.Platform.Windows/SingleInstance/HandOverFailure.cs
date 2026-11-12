namespace Bravo.Platform.Windows.SingleInstance;

/// <summary>
/// Why a second process could not hand its request over to the owner of the instance.
/// </summary>
public enum HandOverFailure
{
    /// <summary>
    /// The owner runs where this process has no access, as when their privilege levels differ.
    /// </summary>
    AccessDenied,

    /// <summary>
    /// The owner did not take the request within the time limit: it could not be reached, or did not answer.
    /// </summary>
    NotResponding,

    /// <summary>
    /// The owner refused the request, as an owner of another version does.
    /// </summary>
    Refused,
}
