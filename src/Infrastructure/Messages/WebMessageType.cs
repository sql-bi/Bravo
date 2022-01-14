namespace Sqlbi.Bravo.Infrastructure.Messages
{
    internal enum WebMessageType
    {
        None = 0,
        ApplicationUpdateAvailable = 1,
        NetworkStatusChanged = 2,
        PBIDesktopReportOpen = 3,
        PBICloudDatasetOpen = 4,
        VpaxFileOpen = 5,
    }
}
