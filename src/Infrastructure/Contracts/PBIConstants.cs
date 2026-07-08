namespace Sqlbi.Bravo.Infrastructure.Contracts
{
    internal static class PBIConstants
    {
        public static class Endpoints
        {
            public static readonly Uri GlobalCloudPowerBIUri = new("https://api.powerbi.com", UriKind.Absolute);
        }

        public static class Registry
        {
            public const string PowerBIDiscoveryUrlValueName = "PowerBIDiscoveryUrl";
            public const string PowerBISubkeyName = @"SOFTWARE\Microsoft\Microsoft Power BI\";
            public const string PowerBIPolicySubkeyName = @"SOFTWARE\Policies\Microsoft\Microsoft Power BI\";
        }
    }
}
