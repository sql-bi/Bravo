using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Bravo.Api.Diagnostics;
using Bravo.Desktop.Diagnostics;
using Bravo.Platform.Windows.Tests;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bravo.Desktop.Tests.Diagnostics;

public sealed class HostLogTests : IDisposable
{
    private const string ApplicationCategory = "Bravo.Desktop.Tests.Diagnostics";
    private const string LibraryCategory = "Microsoft.AspNetCore.Tests";

    private readonly TemporaryFolder _dataFolder = new();
    private readonly HostLog _log;

    public HostLogTests()
    {
        _log = _dataFolder.OpenLog();
    }

    public void Dispose()
    {
        _log.Dispose();
        _dataFolder.Dispose();
    }

    [Fact]
    public void Open_DataFolder_CreatesTheFileOfTheRunInItsFolderOfLogs()
    {
        Assert.Equal(_dataFolder.Combine(HostLog.FolderName), Path.GetDirectoryName(_log.FilePath));
        Assert.True(File.Exists(_log.FilePath));
    }

    [Fact]
    public void Log_InformationOfTheApplication_IsRecorded()
    {
        Write(ApplicationCategory, LogLevel.Information, "What the application did.");

        var record = Assert.Single(_log.Buffer.Read(after: 0).Records);
        Assert.Equal((LogRecordLevel.Information, ApplicationCategory), (record.Level, record.Category));
    }

    // The information of a library tells of every request, with its address.
    [Fact]
    public void Log_InformationOfALibrary_IsNotRecorded()
    {
        Write(LibraryCategory, LogLevel.Information, "A request started.");

        Assert.Empty(_log.Buffer.Read(after: 0).Records);
    }

    [Fact]
    public void Log_WarningOfALibrary_IsRecorded()
    {
        Write(LibraryCategory, LogLevel.Warning, "Something went wrong in a library.");

        var record = Assert.Single(_log.Buffer.Read(after: 0).Records);
        Assert.Equal((LogRecordLevel.Warning, LibraryCategory), (record.Level, record.Category));
    }

    [Fact]
    public void Log_Record_IsInTheFileBeforeTheLogIsClosed()
    {
        Write(ApplicationCategory, LogLevel.Information, "Written at once.");

        Assert.Contains($"{ApplicationCategory}[0] Written at once.", ReadFile(), StringComparison.Ordinal);
    }

    [Fact]
    public void Log_InsideAnActivity_CarriesItsTrace()
    {
        using var activity = new Activity("Bravo.Tests").Start();

        Write(ApplicationCategory, LogLevel.Information, "Part of the work of the activity.");

        var record = Assert.Single(_log.Buffer.Read(after: 0).Records);
        Assert.Equal(activity.TraceId.ToHexString(), record.TraceId);
    }

    [Fact]
    public void Log_OutsideAnyActivity_HasNoTrace()
    {
        Write(ApplicationCategory, LogLevel.Information, "Part of no work.");

        var record = Assert.Single(_log.Buffer.Read(after: 0).Records);
        Assert.Null(record.TraceId);
    }

    [Fact]
    public void Log_Exception_IsRecordedWithItsCause()
    {
        var cause = new IOException("The cause.");
        var exception = new InvalidOperationException("The failure.", cause);

        Write(ApplicationCategory, LogLevel.Warning, "It failed.", exception);

        var record = Assert.Single(_log.Buffer.Read(after: 0).Records);
        Assert.Contains("The cause.", record.Exception, StringComparison.Ordinal);
    }

    // A file where the folder of the logs should be: the folder cannot be created.
    [Fact]
    public void Open_FolderOfLogsThatCannotBeCreated_RecordsItAndGoesOnWithoutAFile()
    {
        using var dataFolder = new TemporaryFolder();
        File.WriteAllText(dataFolder.Combine(HostLog.FolderName), "Not a folder.");

        using var log = dataFolder.OpenLog();

        Assert.Null(log.FilePath);
        Assert.Equal(LogRecordLevel.Warning, Assert.Single(log.Buffer.Read(after: 0).Records).Level);
    }

    // Writes to the Event Log of the computer: only a runner of GitHub Actions is thrown away after the run.
    [Fact]
    public void Log_WarningWhenAskedToWriteToTheEventLog_ReachesTheApplicationLogOfWindowsWithItsTrace()
    {
        HostedRunner.SkipElsewhere("to the Event Log of the computer");
        var marker = $"Bravo.Tests.{Guid.NewGuid():N}";
        using var activity = new Activity("Bravo.Tests").Start();
        var trace = $"TraceId: {activity.TraceId.ToHexString()}";

        using (var log = HostLog.Open(_dataFolder.AsDataFolder(), writeToEventLog: true, TimeProvider.System))
        {
            var logger = log.Factory.CreateLogger(ApplicationCategory);
            logger.Log(LogLevel.Warning, default, marker, null, (state, _) => state);
        }

        using var applicationLog = new EventLog("Application");
        var entry = Assert.Single(applicationLog.Entries.Cast<EventLogEntry>(), entry => entry.Message.Contains(marker, StringComparison.Ordinal));
        Assert.Equal((".NET Runtime", 1000L), (entry.Source, entry.InstanceId));
        Assert.Contains(trace, entry.Message, StringComparison.Ordinal);
    }

    private void Write(string category, LogLevel level, string message, Exception? exception = null)
    {
        var logger = _log.Factory.CreateLogger(category);
        logger.Log(level, default, message, exception, (state, _) => state);
    }

    // The file stays open for writing until the log is closed.
    private string ReadFile()
    {
        using var stream = new FileStream(_log.FilePath!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
