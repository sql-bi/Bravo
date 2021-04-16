namespace Sqlbi.Bravo.UI.ViewModels
{
    public class OnlineDatasetSummary
    {
        public string DisplayName { get; set; }

        public int Endorsement{ get; set; }

        public string Owner { get; set; }

        public string Workspace { get; set; }

        public string Refreshed { get; set; }

        public bool ShowPromoted => Endorsement == 1;

        public bool ShowCertified => Endorsement == 2;
    }
}
