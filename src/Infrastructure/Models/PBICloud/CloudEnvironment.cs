namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    using Sqlbi.Bravo.Infrastructure.Contracts.PBICloud;
    using Sqlbi.Bravo.Infrastructure.Extensions;

    [DebuggerDisplay("{Name}")]
    public sealed record CloudEnvironment(
        string Name,
        string Description,
        string AuthorityUri,
        string ClientId,
        string RedirectUri,
        string ResourceId,
        string BackendUri,
        string ClusterUri)
    {
        public Uri GetBackendRequestUri(string path)
            => new(new Uri(BackendUri), relativeUri: path);

        public string GetIdentityProvider()
            => $"{AuthorityUri}, {ResourceId}, {ClientId}";

        internal static CloudEnvironment FromContract(CloudEnvironmentContract contract)
        {
            var aadService = contract.Services.Single((s) => s.IsAad());
            var powerbiBackendService = contract.Services.Single((s) => s.IsPowerBIBackend());
            var powerbiDesktopClient = contract.Clients.Single((c) => c.IsPowerBIDesktop());

            return new CloudEnvironment(
                Name: contract.CloudName,
                Description: contract.GetDescription(),
                AuthorityUri: aadService.Endpoint,
                ClientId: powerbiDesktopClient.AppId,
                RedirectUri: powerbiDesktopClient.RedirectUri,
                ResourceId: powerbiBackendService.ResourceId,
                BackendUri: powerbiBackendService.Endpoint,
                ClusterUri: string.Empty);
        }
    }
}
