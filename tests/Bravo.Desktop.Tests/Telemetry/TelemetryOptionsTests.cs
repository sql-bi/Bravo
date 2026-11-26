using System;
using System.IO;
using Bravo.Desktop.Telemetry;
using Xunit;

namespace Bravo.Desktop.Tests.Telemetry;

public sealed class TelemetryOptionsTests
{
    private static readonly DataFolder s_dataFolder = new(@"C:\Tests\Bravo");

    // The connection string is compared, never written in a message: a test that fails does not print it.
    [Fact]
    public void For_ReleaseBuild_SendsToTheResourceOfTheReleases()
    {
        var options = TelemetryOptions.For(BuildIdentity.Release("1.1.0"), s_dataFolder, developmentEndpoint: null);

        Assert.True(options.ConnectionString == TelemetryOptions.ReleaseConnectionString);
        Assert.DoesNotContain(TelemetryOptions.DevelopmentInstrumentationKey, TelemetryOptions.ReleaseConnectionString);
    }

    // The SDK of the page adds the path of the endpoint to it as it is written: a slash at the end would make
    // a path with two slashes. The connection strings that the Azure portal shows end their endpoints with one.
    [Fact]
    public void ReleaseConnectionString_EndpointsEndWithoutASlash()
    {
        var fields = TelemetryOptions.ReleaseConnectionString.Split(';');

        Assert.False(
            Array.Exists(fields, field => field.Contains("Endpoint=", StringComparison.OrdinalIgnoreCase) && field.EndsWith('/')),
            "An endpoint of the connection string of the releases ends with a slash: remove it.");
    }

    // The webview takes the page to this server through the proxy.
    [Fact]
    public void For_ReleaseBuild_NamesTheServerOfTheIngestion()
    {
        var options = TelemetryOptions.For(BuildIdentity.Release("1.1.0"), s_dataFolder, developmentEndpoint: null);

        Assert.Equal(Uri.UriSchemeHttps, options.IngestionEndpoint?.Scheme);
    }

    [Fact]
    public void For_DevelopmentBuildWithoutAnEndpoint_SendsNothing()
    {
        var options = TelemetryOptions.For(BuildIdentity.Development("1.1.0"), s_dataFolder, developmentEndpoint: null);

        Assert.Equal((null, null), (options.ConnectionString, options.IngestionEndpoint));
    }

    [Fact]
    public void For_DevelopmentBuildWithAnEndpoint_SendsThereUnderAKeyOfNoResource()
    {
        var options = TelemetryOptions.For(BuildIdentity.Development("1.1.0"), s_dataFolder, "http://127.0.0.1:8097");

        Assert.Equal(
            "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=http://127.0.0.1:8097",
            options.ConnectionString);
        Assert.Equal(new Uri("http://127.0.0.1:8097"), options.IngestionEndpoint);
    }

    [Fact]
    public void For_AnyBuild_KeepsWhatWaitsInTheDataFolder()
    {
        var options = TelemetryOptions.For(BuildIdentity.Development("1.1.0"), s_dataFolder, developmentEndpoint: null);

        Assert.Equal(Path.Combine(s_dataFolder.Path, "Telemetry"), options.StorageFolder);
    }
}
