using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Bravo.Desktop;

internal static partial class Program
{
    private static void LogStarted(ILogger logger, BuildIdentity identity, string? logFile)
    {
        LogStarted(
            logger,
            identity.Version,
            identity.IsDevelopmentBuild,
            RuntimeInformation.FrameworkDescription,
            identity.IncludesRuntime,
            RuntimeInformation.ProcessArchitecture,
            RuntimeInformation.OSDescription,
            logFile ?? "none");
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Bravo {Version} started. Development build: {IsDevelopmentBuild}. Runtime: {Runtime}, carried by the build: {IncludesRuntime}. Process: {Architecture}. System: {System}. Log file: {LogFile}.")]
    private static partial void LogStarted(
        ILogger logger,
        string version,
        bool isDevelopmentBuild,
        string runtime,
        bool includesRuntime,
        Architecture architecture,
        string system,
        string logFile);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "The policies cannot be read: the application ends.")]
    private static partial void LogPoliciesUnreadable(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Bravo ended with the exit code {ExitCode}.")]
    private static partial void LogEnded(ILogger logger, int exitCode);
}
