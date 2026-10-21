using System;
using Bravo.Tests.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Host;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Xunit;

namespace Bravo.Tests.Host;

public class BravoLoggingBuilderExtensionsTests
{
    [Theory]
    [InlineData(DiagnosticLevelType.None)]
    [InlineData(DiagnosticLevelType.Basic)]
    public void AddBravoDiagnosticLog_DropsInformationBelowTheVerboseLevel(DiagnosticLevelType level)
    {
        var store = new DiagnosticLogStore();
        using var loggerFactory = CreateLoggerFactory(store, new DiagnosticLevelSwitch(level));
        var logger = loggerFactory.CreateLogger("Sqlbi.Bravo.Test");

        logger.LogInformation("dropped");
        logger.LogWarning("kept");

        Assert.False(logger.IsEnabled(LogLevel.Information));
        Assert.Equal("kept", Assert.Single(store.Read(after: 0).Entries).Message);
    }

    [Fact]
    public void AddBravoDiagnosticLog_FollowsALevelChangedAfterTheLoggerWasCreated()
    {
        var levelSwitch = new DiagnosticLevelSwitch(DiagnosticLevelType.Basic);
        using var loggerFactory = CreateLoggerFactory(new DiagnosticLogStore(), levelSwitch);
        var logger = loggerFactory.CreateLogger("Sqlbi.Bravo.Test");

        levelSwitch.Level = DiagnosticLevelType.Verbose;

        Assert.True(logger.IsEnabled(LogLevel.Information));
    }

    [Fact]
    public void AddBravoDiagnosticLog_KeepsFrameworkCategoriesAtWarningAtTheVerboseLevel()
    {
        var levelSwitch = new DiagnosticLevelSwitch(DiagnosticLevelType.Verbose);
        using var loggerFactory = CreateLoggerFactory(new DiagnosticLogStore(), levelSwitch);
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Hosting.Diagnostics");

        Assert.False(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
    }

    private static ILoggerFactory CreateLoggerFactory(DiagnosticLogStore store, DiagnosticLevelSwitch levelSwitch)
    {
        var timeProvider = new FixedTimeProvider(DateTimeOffset.UnixEpoch);
        return LoggerFactory.Create((builder) => builder.AddBravoDiagnosticLog(store, levelSwitch, timeProvider));
    }
}
