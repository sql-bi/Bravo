using Bravo.Tests.Host;
using Bravo.Tests.Infrastructure.Diagnostics;
using Bravo.Tests.Infrastructure.Policies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Sqlbi.Bravo.Controllers;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Sqlbi.Bravo.Infrastructure.Policies;
using Xunit;

namespace Bravo.Tests.Controllers;

public class ApplicationControllerTests
{
    [Fact]
    public void GetDiagnostics_ReturnsTheRecordsAfterTheCursor()
    {
        var diagnosticLog = new DiagnosticLogStore();
        diagnosticLog.Add(TestLogEntries.Create("read"));
        diagnosticLog.Add(TestLogEntries.Create("new"));
        var controller = CreateController(diagnosticLog);

        var result = Assert.IsType<OkObjectResult>(controller.GetDiagnostics(after: 1));

        var page = Assert.IsType<DiagnosticLogPage>(result.Value);
        Assert.Equal("new", Assert.Single(page.Entries).Message);
        Assert.Equal(2, page.LastSequence);
    }

    [Fact]
    public void GetDiagnostics_ReturnsAtMostTheRequestedNumberOfRecords()
    {
        var diagnosticLog = new DiagnosticLogStore();
        diagnosticLog.Add(TestLogEntries.Create("older"));
        diagnosticLog.Add(TestLogEntries.Create("newer"));
        var controller = CreateController(diagnosticLog);

        var result = Assert.IsType<OkObjectResult>(controller.GetDiagnostics(after: 0, max: 1));

        var page = Assert.IsType<DiagnosticLogPage>(result.Value);
        Assert.Equal("newer", Assert.Single(page.Entries).Message);
        Assert.Equal(1, page.DroppedCount);
    }

    private static ApplicationController CreateController(DiagnosticLogStore diagnosticLog)
    {
        var policyService = new PolicyService(new FakePolicyReader());
        var logger = NullLogger<ApplicationController>.Instance;
        return new ApplicationController(new FakeTelemetry(), policyService, new DiagnosticLevelSwitch(), diagnosticLog, logger);
    }
}
