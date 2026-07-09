namespace Sqlbi.Bravo.Models.Authentication
{
    using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Authentication;

    public sealed class SignInResponse(AuthenticationResult authenticationResult)
    {
        public AccountDto Account { get; } = authenticationResult.ToDto();
    }
}
