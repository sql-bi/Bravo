using System;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Xunit;

namespace Bravo.Tests.Infrastructure.Diagnostics;

public class ErrorReportTests
{
    [Fact]
    public void Text_HasTheTitleTheEnvironmentAndTheException()
    {
        var report = ErrorReport.Create(new InvalidOperationException("startup failed"));

        Assert.StartsWith("# Bravo for Power BI — Error Report", report.Text);
        Assert.Contains("## Environment information", report.Text);
        Assert.Contains("\"application\"", report.Text);
        Assert.Contains("## Exception details", report.Text);
        Assert.Contains("System.InvalidOperationException: startup failed", report.Text);
    }
}
