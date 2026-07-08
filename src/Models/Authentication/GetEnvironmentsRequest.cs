using System.ComponentModel.DataAnnotations;

namespace Sqlbi.Bravo.Models.Authentication
{
    public sealed record GetEnvironmentsRequest(
        [Required] string Email);
}
