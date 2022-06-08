namespace Sqlbi.Bravo.Infrastructure.Models
{
    using Sqlbi.Bravo.Models;

    public interface IAuthenticationResult
    {
        bool IsExpired { get; }

        string AccessToken { get; }

        IBravoAccount Account { get; }
    }
}