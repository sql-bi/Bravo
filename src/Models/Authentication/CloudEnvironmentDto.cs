using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
using System.ComponentModel.DataAnnotations;

namespace Sqlbi.Bravo.Models.Authentication
{
    public sealed record CloudEnvironmentDto(
        [Required] string Name,
        [Required] string Description,
        [Required] string AuthorityUri,
        [Required] string ClientId,
        [Required] string RedirectUri,
        [Required] string ResourceId,
        [Required] string BackendUri,
        [Required(AllowEmptyStrings = true)] string ClusterUri);

    internal static class CloudEnvironmentDtoMappingExtensions
    {
        internal static CloudEnvironmentDto ToDto(this CloudEnvironment model) => new(
            model.Name,
            model.Description,
            model.AuthorityUri,
            model.ClientId,
            model.RedirectUri,
            model.ResourceId,
            model.BackendUri,
            model.ClusterUri);

        internal static CloudEnvironment ToModel(this CloudEnvironmentDto dto) => new(
            dto.Name,
            dto.Description,
            dto.AuthorityUri,
            dto.ClientId,
            dto.RedirectUri,
            dto.ResourceId,
            dto.BackendUri,
            dto.ClusterUri);
    }
}
