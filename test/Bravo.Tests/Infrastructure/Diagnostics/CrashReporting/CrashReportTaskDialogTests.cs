using Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;
using Xunit;

namespace Bravo.Tests.Infrastructure.Diagnostics.CrashReporting;

public class CrashReportTaskDialogTests
{
    [Theory]
    [InlineData("System.InvalidOperationException", "InvalidOperationException")]
    [InlineData("Exception", "Exception")]
    [InlineData("MyApp.ResultException`1[[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]", "ResultException`1")]
    [InlineData("MyApp.Outer+InnerException", "InnerException")]
    public void GetTypeName_LeavesOutTheNamespace(string fullName, string expected)
    {
        Assert.Equal(expected, CrashReportTaskDialog.GetTypeName(fullName));
    }
}
