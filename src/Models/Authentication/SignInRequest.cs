using System.ComponentModel.DataAnnotations;

namespace Sqlbi.Bravo.Models.Authentication
{
    public sealed record SignInRequest(
        [Required] string Email,
        [Required] CloudEnvironmentDto Environment);
}
