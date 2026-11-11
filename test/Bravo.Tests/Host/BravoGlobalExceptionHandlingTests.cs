using System.Reflection;
using Sqlbi.Bravo.Host;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;
using Xunit;

namespace Bravo.Tests.Host;

public class BravoGlobalExceptionHandlingTests
{
    [Fact]
    public void CrashPath_DoesNotReferenceAppEnvironment()
    {
        // The initialization of AppEnvironment can be the crash being reported: the report must still be saved in its
        // folder, the reporter started, and the message box shown when either fails.
        MethodBase?[] roots =
        [
            typeof(BravoCrashReporter).TypeInitializer,
            typeof(UnhandledExceptionHandler).GetMethod(nameof(UnhandledExceptionHandler.Handle)),
            typeof(CrashReporterLauncher).GetMethod(nameof(CrashReporterLauncher.Launch)),
            typeof(BravoGlobalExceptionHandling).GetMethod("ShowFallback", BindingFlags.NonPublic | BindingFlags.Static),
        ];

        foreach (var root in roots)
        {
            Assert.NotNull(root);
            Assert.DoesNotContain(typeof(AppEnvironment), CallGraph.GetReferencedTypes(root));
        }
    }
}
