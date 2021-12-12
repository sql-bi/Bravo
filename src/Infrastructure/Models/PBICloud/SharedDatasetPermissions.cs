using System;

#nullable disable

namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    /// <summary>
    /// v201901 - metadata/v201901/gallery/sharedDatasets
    /// </summary>
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
