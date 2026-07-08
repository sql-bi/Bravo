namespace Sqlbi.Bravo.Models.Authentication
{
    public sealed class SignInResponse(AppAccount account)
    {
        public AppAccountDto Account { get; } = account.ToDto();
    }
}
