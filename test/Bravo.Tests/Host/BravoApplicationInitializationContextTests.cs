using Sqlbi.Bravo.Host;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Policies;
using Xunit;

namespace Bravo.Tests.Host;

/// <summary>
/// Verifies that <see cref="BravoApplicationInitializationContext"/> carries the initialization
/// entries as-is and owns only what the initialization created.
/// </summary>
public class BravoApplicationInitializationContextTests
{
    [Fact]
    public void Entries_ReturnTheSameObjectsTheInitializationProduced()
    {
        var instance = new FakeInstance { IsPrimary = true };
        //var policies = Policies.None;
        //var userSettings = new UserSettings();
        //var telemetry = new FakeTelemetry();
        //var webProxy = new FakeWebProxy();

        using var context = new BravoApplicationInitializationContext(instance);

        Assert.Same(instance, context.Instance);
    }

    [Fact]
    public void Dispose_DisposesTheInstance()
    {
        var instance = new FakeInstance();
        var context = FakeInitializationContext.Create(instance);

        context.Dispose();

        Assert.True(instance.Disposed);
    }

    //[Fact]
    //public void Dispose_DoesNotDisposeEntriesTheInitializationDidNotCreate()
    //{
    //    // Telemetry is still a process-wide static that owns itself; the context must not dispose it
    //    // until the initialization creates it.
    //    var telemetry = new FakeTelemetry();
    //    var context = FakeInitializationContext.Create(telemetry: telemetry);

    //    context.Dispose();

    //    Assert.False(telemetry.Disposed);
    //}
}
