using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud;

namespace Sqlbi.Bravo.Models.Authentication
{
    public sealed class GetEnvironmentsResponse(IEnumerable<CloudEnvironment> environments)
    {
        public IReadOnlyList<CloudEnvironmentDto> Environments { get; } = [.. environments.Select(e => e.ToDto())];
    }
}
