namespace Sqlbi.Bravo.Infrastructure.Telemetry;

internal interface ITelemetryServiceFactory
{
    ITelemetryService Create(bool enabled);
}

internal sealed class TelemetryServiceFactory : ITelemetryServiceFactory
{
    public ITelemetryService Create(bool enabled)
        => TelemetryService.Create(TelemetrySessionInfo.ConnectionString, enabled, TelemetryService.DefaultStorageFolder);
}
