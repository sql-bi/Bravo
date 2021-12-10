using System;

#nullable disable

namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    [Flags]
    public enum SharedDatasetPermissions
    {
        None = 0,

        Read = 1,

        Write = 2,

        ReShared = 4,

        Explore = 8
    }
}
