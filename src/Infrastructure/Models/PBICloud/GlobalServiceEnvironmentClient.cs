#nullable disable

namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    public class GlobalServiceEnvironmentClient
    {
        public string Name { get; set; }

        public string AppId { get; set; }

        public string RedirectUri { get; set; }

        public string AppInsightsId { get; set; }

        public string LocalyticsId { get; set; }
    }
}
