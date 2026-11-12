namespace Bravo.Desktop.SingleInstance;

internal static class BravoInstance
{
    /// <summary>
    /// What the names of the mutex and of the pipe begin with. It stays the same from one release to the
    /// next, so that two versions are one instance; a change of the messages is a new protocol version.
    /// </summary>
    public const string ApplicationId = "Bravo.BEA7871EE59F4086A72C06950412689B";
}
