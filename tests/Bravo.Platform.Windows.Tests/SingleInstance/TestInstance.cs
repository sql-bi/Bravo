using System;
using Bravo.Platform.Windows.SingleInstance;

namespace Bravo.Platform.Windows.Tests.SingleInstance;

internal static class TestInstance
{
    /// <summary>
    /// Names that no other test and no running application uses: each test has an instance of its own.
    /// </summary>
    public static InstanceNames CreateNames()
    {
        var id = Guid.NewGuid().ToString("N");
        return new InstanceNames($"Bravo.Tests.{id}", $"Bravo.Tests.{id}");
    }

    /// <summary>
    /// Limits short enough for a test that waits for one of them to expire.
    /// </summary>
    public static SingleInstanceOptions ShortOptions { get; } = new()
    {
        ConnectTimeout = TimeSpan.FromMilliseconds(200),
        ExchangeTimeout = TimeSpan.FromMilliseconds(500),
        ArbitrationTimeout = TimeSpan.FromSeconds(1),
        RetryInterval = TimeSpan.FromMilliseconds(20),
    };
}
