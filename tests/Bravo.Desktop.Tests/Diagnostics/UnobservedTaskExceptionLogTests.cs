using System;
using System.Threading.Tasks;
using Bravo.Api.Diagnostics;
using Bravo.Desktop.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bravo.Desktop.Tests.Diagnostics;

public sealed class UnobservedTaskExceptionLogTests : IDisposable
{
    private readonly TemporaryFolder _dataFolder = new();

    public void Dispose()
    {
        _dataFolder.Dispose();
    }

    [Fact]
    public void OnUnobservedTaskException_Exception_RecordsItAsAnError()
    {
        // Not attached: the event of the runtime is raised for the tasks of every test.
        var log = new UnobservedTaskExceptionLog(_dataFolder.Log.Factory.CreateLogger<UnobservedTaskExceptionLog>());
        var exception = new AggregateException(new InvalidOperationException("It failed."));

        log.OnUnobservedTaskException(this, new UnobservedTaskExceptionEventArgs(exception));

        var record = Assert.Single(_dataFolder.Log.Buffer.Read(after: 0).Records);
        Assert.Equal(LogRecordLevel.Error, record.Level);
        Assert.Contains("It failed.", record.Exception, StringComparison.Ordinal);
    }
}
