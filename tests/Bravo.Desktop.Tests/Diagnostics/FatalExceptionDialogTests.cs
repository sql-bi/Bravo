using System;
using Bravo.Desktop.Diagnostics;
using Xunit;

namespace Bravo.Desktop.Tests.Diagnostics;

public sealed class FatalExceptionDialogTests
{
    private readonly InvalidOperationException _exception = new("It failed.");

    [Fact]
    public void CreateText_Exception_SaysThatTheApplicationEndsAndNamesTheError()
    {
        var text = FatalExceptionDialog.CreateText(_exception, reportPath: null);

        Assert.StartsWith($"{Product.Name} met an unexpected error and has to close.", text, StringComparison.Ordinal);
        Assert.EndsWith("System.InvalidOperationException: It failed.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateText_Report_SaysWhereTheDetailsAre()
    {
        var text = FatalExceptionDialog.CreateText(_exception, @"C:\Data\Logs\Bravo-1.crash.json");

        Assert.EndsWith(@"The details are in C:\Data\Logs\Bravo-1.crash.json.", text, StringComparison.Ordinal);
    }
}
