namespace Sqlbi.Bravo.Infrastructure.Policies;

internal interface IPolicyReader
{
    PolicySnapshot Read();
}
