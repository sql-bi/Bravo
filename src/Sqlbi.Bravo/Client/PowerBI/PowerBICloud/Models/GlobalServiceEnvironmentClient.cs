using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Client.PowerBI.PowerBICloud.Models
{
    internal class GlobalServiceEnvironmentClient
    {
        public string Name { get; set; }

        public string AppId { get; set; }

        public string RedirectUri { get; set; }

        public string AppInsightsId { get; set; }

        public string LocalyticsId { get; set; }
    }
}
