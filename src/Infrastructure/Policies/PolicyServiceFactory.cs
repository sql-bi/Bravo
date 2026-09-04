namespace Sqlbi.Bravo.Infrastructure.Policies;

internal interface IPolicyServiceFactory
{
    IPolicyService Create();
}

internal sealed class PolicyServiceFactory : IPolicyServiceFactory
{
    public IPolicyService Create()
        => new PolicyService(new RegistryPolicyReader(new RegistryPolicySourceFactory()));
}
