using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Bravo.Architecture.Tests.Telemetry;

/// <summary>
/// The installer and the application send to the same resource. Each holds the connection string as a constant of
/// its own: the installer references no project.
/// </summary>
public sealed class ReleaseConnectionStringTests
{
    [Fact]
    public void ReleaseConnectionString_OfTheInstaller_IsTheOneOfTheApplication()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "The application and the installer are built only on Windows.");
        using var assemblies = ProductionAssemblies.Read();

        var application = ReadConstant(assemblies, "Bravo.Desktop.Telemetry.TelemetryOptions");
        var installer = ReadConstant(assemblies, "Bravo.Setup.Telemetry.TelemetryDestination");

        // No value in the message: the connection string goes into no output of a test.
        Assert.True(application is not null && application == installer, "The installer and the application send to different resources.");
    }

    private static string? ReadConstant(MetadataAssemblies assemblies, string typeName)
    {
        var type = assemblies.Types.Single(type => type.FullName == typeName);
        var field = type.GetField("ReleaseConnectionString", BindingFlags.Static | BindingFlags.NonPublic);
        return field?.GetRawConstantValue() as string;
    }
}
