using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Client.PowerBI.PowerBICloud.Models
{
    internal class TenantCluster
    {
	    //public string DynamicClusterUri { get; set; }

        public string FixedClusterUri { get; set; }

        //public string NewTenantId { get; set; }

        //public string RuleDescription { get; set; }

        public int TTLSeconds { get; set; }
    }
}