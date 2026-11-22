namespace Bravo.Core.Errors;

/// <summary>
/// What a use case can report with a <see cref="BravoException"/>, as the user interface is told. The list is
/// closed and is part of the contract of the API: the user interface has a text for every member, and the
/// name of a member never changes meaning.
/// </summary>
public enum ErrorCode
{
    // The first member takes the value 1: zero, the value of a variable that nobody set, is no code.

    /// <summary>
    /// The update feed cannot be read, or what it holds names no release: whether a newer version exists is
    /// not known.
    /// </summary>
    UpdateCheckFailed = 1,

    /// <summary>
    /// The settings of the user could not be written: the ones before the change are still in force.
    /// </summary>
    SettingsSaveFailed = 2,

    /// <summary>
    /// An administrator turned the update check off with a policy: no request is made to the update feed.
    /// </summary>
    UpdateCheckDisabled = 3,

    /// <summary>
    /// The proxy server asks for credentials, and the ones it was given, if any, were not accepted.
    /// </summary>
    ProxyAuthenticationRequired = 4,

    /// <summary>
    /// The proxy server answered, and refused the connection to the server.
    /// </summary>
    ProxyConnectionRefused = 5,

    /// <summary>
    /// The server did not answer: its name is not known, the connection was refused, or it took too long.
    /// </summary>
    ServerUnreachable = 6,

    /// <summary>
    /// The secure connection to the server could not be established: its certificate is not trusted.
    /// </summary>
    SecureConnectionFailed = 7,

    /// <summary>
    /// The credentials of the proxy server could not be kept or forgotten: the Windows Credential Manager refused
    /// them. The ones before stay as they were.
    /// </summary>
    ProxyCredentialsStoreFailed = 8,
}
