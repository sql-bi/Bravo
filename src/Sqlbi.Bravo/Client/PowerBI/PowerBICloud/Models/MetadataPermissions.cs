using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Client.PowerBI.PowerBICloud.Models
{
    [Flags]
    internal enum MetadataPermissions
    {
        None = 0,

        Read = 1,

        Write = 2,

        ReShared = 4,

        Explore = 8
    }
}
