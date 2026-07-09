namespace Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Contracts
{
    using System;

    [Flags]
    public enum CloudPermissions
    {
        None = 0,
        Read = 1,
        Write = 2,
        ReShared = 4,
        Explore = 8
    }
}