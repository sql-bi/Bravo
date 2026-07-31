using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Authentication;

namespace Sqlbi.Bravo.Models.Authentication;

public sealed class SignInResponse(AuthenticationResult authenticationResult)
{
    public AccountDto Account { get; } = authenticationResult.ToDto();
}
