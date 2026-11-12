namespace Bravo.Api.Security;

internal static class ApiTokenAuthenticationDefaults
{
    /// <summary>
    /// The authentication scheme of the token of the user interface. Every policy names the schemes it
    /// accepts: a second token is a second scheme, accepted only where a policy names it.
    /// </summary>
    public const string AuthenticationScheme = "ApiToken";
}
