namespace Bravo.Tests.Infrastructure.Telemetry;

using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using NSubstitute;
using Sqlbi.Bravo.Infrastructure.Telemetry;
using System;
using System.Linq;
using Xunit;

public class DefaultTelemetryProcessorTests
{
    private readonly ITelemetryProcessor _next;
    private readonly DefaultTelemetryProcessor _processor;

    public DefaultTelemetryProcessorTests()
    {
        _next = Substitute.For<ITelemetryProcessor>();
        _processor = new DefaultTelemetryProcessor(_next);
    }

    [Theory]
    [InlineData(
        "Either the database '<oii>c49dbc76-d629-40ec-b57b-eccaa2a26614</oii>' does not exist",
        "Either the database '<oii>[REDACTED]</oii>' does not exist")]
    [InlineData(
        "User '<pii>john.doe@contoso.com</pii>' is not authorized",
        "User '<pii>[REDACTED]</pii>' is not authorized")]
    [InlineData(
        "Session for user '<euii>admin@company.com</euii>' has expired",
        "Session for user '<euii>[REDACTED]</euii>' has expired")]
    [InlineData(
        "Identifier '<eupi>a1b2c3d4-e5f6</eupi>' not found",
        "Identifier '<eupi>[REDACTED]</eupi>' not found")]
    [InlineData(
        "Connection '<conn>Server=myserver;Database=mydb;User=sa;Password=secret</conn>' failed",
        "Connection '<conn>[REDACTED]</conn>' failed")]
    public void Process_ExceptionWithSingleSensitiveTag_RedactsDetailsInfoMessage(string message, string expected)
    {
        var telemetry = new ExceptionTelemetry(new Exception(message));

        _processor.Process(telemetry);

        var detailMessage = telemetry.ExceptionDetailsInfoList.Single().Message;
        Assert.Equal(expected, detailMessage);
        _next.Received(1).Process(telemetry);
    }

    [Fact]
    public void Process_ExceptionWithMultipleSensitiveTags_RedactsAll()
    {
        var exception = new Exception("User '<pii>john@contoso.com</pii>' cannot access database '<oii>c49dbc76</oii>'");
        var telemetry = new ExceptionTelemetry(exception);

        _processor.Process(telemetry);

        var detailMessage = telemetry.ExceptionDetailsInfoList.Single().Message;
        Assert.Equal("User '<pii>[REDACTED]</pii>' cannot access database '<oii>[REDACTED]</oii>'", detailMessage);
    }

    [Fact]
    public void Process_ExceptionWithCaseInsensitiveTags_RedactsContent()
    {
        var telemetry = new ExceptionTelemetry(new Exception("User '<PII>john@contoso.com</PII>' is not authorized"));

        _processor.Process(telemetry);

        var detailMessage = telemetry.ExceptionDetailsInfoList.Single().Message;
        Assert.Equal("User '<PII>[REDACTED]</PII>' is not authorized", detailMessage);
    }

    [Theory]
    [InlineData("No sensitive data here")]
    [InlineData("Error code: 12345")]
    [InlineData("Simple error message")]
    public void Process_ExceptionWithoutSensitiveTags_LeavesDetailsInfoMessageUnchanged(string message)
    {
        var telemetry = new ExceptionTelemetry(new Exception(message));

        _processor.Process(telemetry);

        var detailMessage = telemetry.ExceptionDetailsInfoList.Single().Message;
        Assert.Equal(message, detailMessage);
        _next.Received(1).Process(telemetry);
    }

    [Fact]
    public void Process_ExceptionWithMismatchedTags_LeavesDetailsInfoMessageUnchanged()
    {
        var message = "Mismatched '<pii>value</oii>' should not match";
        var telemetry = new ExceptionTelemetry(new Exception(message));

        _processor.Process(telemetry);

        var detailMessage = telemetry.ExceptionDetailsInfoList.Single().Message;
        Assert.Equal(message, detailMessage);
    }

    [Fact]
    public void Process_ExceptionWithInnerException_RedactsAllDetailsInfoMessages()
    {
        var inner = new Exception("User '<pii>john@contoso.com</pii>' denied");
        var outer = new Exception("Operation failed for '<oii>workspace-id</oii>'", inner);
        var telemetry = new ExceptionTelemetry(outer);

        _processor.Process(telemetry);

        var details = telemetry.ExceptionDetailsInfoList.ToList();
        Assert.Equal(2, details.Count);
        Assert.Equal("Operation failed for '<oii>[REDACTED]</oii>'", details[0].Message);
        Assert.Equal("User '<pii>[REDACTED]</pii>' denied", details[1].Message);
    }

    [Fact]
    public void Process_ExceptionWithNestedInnerExceptions_RedactsEntireChain()
    {
        var innermost = new Exception("Connection '<conn>Server=secret;Password=123</conn>' failed");
        var middle = new Exception("Database '<oii>db-guid</oii>' error", innermost);
        var outer = new Exception("Top-level error", middle);
        var telemetry = new ExceptionTelemetry(outer);

        _processor.Process(telemetry);

        var details = telemetry.ExceptionDetailsInfoList.ToList();
        Assert.Equal(3, details.Count);
        Assert.Equal("Top-level error", details[0].Message);
        Assert.Equal("Database '<oii>[REDACTED]</oii>' error", details[1].Message);
        Assert.Equal("Connection '<conn>[REDACTED]</conn>' failed", details[2].Message);
    }

    [Fact]
    public void Process_RedactsTelemetryMessageProperty()
    {
        var telemetry = new ExceptionTelemetry(new Exception("test"))
        {
            Message = "User '<pii>john@contoso.com</pii>' encountered an error"
        };

        _processor.Process(telemetry);

        Assert.Equal("User '<pii>[REDACTED]</pii>' encountered an error", telemetry.Message);
    }

    [Fact]
    public void Process_RealWorldAnalysisServicesError_RedactsSensitiveContent()
    {
        var message = "Either the database '<oii>c49dbc76-d629-40ec-b57b-eccaa2a26614</oii>' does not exist, or you do not have permissions to access it.\r\nTechnical Details:\r\nRootActivityId: af2e37cb-455b-4e52-923b-97a635401a58\r\nDate (UTC): 3/18/2026 3:25:34 PM";
        var expected = "Either the database '<oii>[REDACTED]</oii>' does not exist, or you do not have permissions to access it.\r\nTechnical Details:\r\nRootActivityId: af2e37cb-455b-4e52-923b-97a635401a58\r\nDate (UTC): 3/18/2026 3:25:34 PM";
        var telemetry = new ExceptionTelemetry(new Exception(message));

        _processor.Process(telemetry);

        var detailMessage = telemetry.ExceptionDetailsInfoList.Single().Message;
        Assert.Equal(expected, detailMessage);
    }

    [Fact]
    public void Process_NonExceptionTelemetry_PassesThroughUnchanged()
    {
        var telemetry = new EventTelemetry("TestEvent");

        _processor.Process(telemetry);

        Assert.Equal("TestEvent", telemetry.Name);
        _next.Received(1).Process(telemetry);
    }

    [Fact]
    public void Process_AlwaysCallsNext()
    {
        var telemetry = new ExceptionTelemetry(new Exception("test"));

        _processor.Process(telemetry);

        _next.Received(1).Process(telemetry);
    }
}
