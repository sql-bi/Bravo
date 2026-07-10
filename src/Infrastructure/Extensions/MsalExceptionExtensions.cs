namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    using Microsoft.Identity.Client;

    internal static class MsalExceptionExtensions
    {
        /// <summary>
        /// Returns <see langword="true"/> if the exception represents the user canceling
        /// an interactive authentication prompt (e.g. closing the sign-in window).
        /// </summary>
        public static bool IsAuthenticationCanceled(this MsalException exception)
            => exception.ErrorCode == MsalError.AuthenticationCanceledError;
    }
}
