using Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;
using Xunit;

namespace Bravo.Tests.Infrastructure.Diagnostics.CrashReporting;

public class CrashReporterLauncherTests
{
    [Fact]
    public void CreateStartInfo_RunsTheExecutableWithTheReportPath()
    {
        var startInfo = CrashReporterLauncher.CreateStartInfo(@"C:\Program Files\Bravo\Bravo.exe", @"C:\crashes\a b.json");

        Assert.Equal(@"C:\Program Files\Bravo\Bravo.exe", startInfo.FileName);
        Assert.Equal(["--crash-report", @"C:\crashes\a b.json"], startInfo.ArgumentList);
    }

    [Fact]
    public void CreateStartInfo_DoesNotUseTheShell()
    {
        var startInfo = CrashReporterLauncher.CreateStartInfo(@"C:\Bravo.exe", @"C:\a.json");

        Assert.False(startInfo.UseShellExecute);
    }

    [Fact]
    public void CreateStartInfo_RunsInTheFolderOfTheExecutable()
    {
        var startInfo = CrashReporterLauncher.CreateStartInfo(@"C:\Program Files\Bravo\Bravo.exe", @"C:\a.json");

        Assert.Equal(@"C:\Program Files\Bravo", startInfo.WorkingDirectory);
    }
}
