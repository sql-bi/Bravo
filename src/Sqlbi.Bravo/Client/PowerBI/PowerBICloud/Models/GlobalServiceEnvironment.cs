using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Client.PowerBI.PowerBICloud.Models
{
    internal class GlobalServiceEnvironment
    {
        public string CloudName { get; set; }
             
        public IEnumerable<GlobalServiceEnvironmentService> Services { get; set; }
                
        public IEnumerable<GlobalServiceEnvironmentClient> Clients { get; set; }
    }
}
