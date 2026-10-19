namespace Sqlbi.Bravo.Infrastructure.Policies;

internal interface IPolicySourceFactory
{
    IPolicySource Open(PolicyScope scope);
}
