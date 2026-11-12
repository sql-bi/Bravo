using System;
using System.Threading;
using Bravo.Platform.Windows.SingleInstance;

namespace Bravo.Desktop.Tests.SingleInstance;

/// <summary>
/// Holds the ownership of an instance on a thread of its own, as the entry thread of the application does:
/// a mutex is released by the thread that acquired it, and an asynchronous test does not stay on one thread.
/// </summary>
internal sealed class OwnedInstance : IDisposable
{
    private readonly ManualResetEventSlim _acquired = new();
    private readonly ManualResetEventSlim _release = new();
    private readonly Thread _thread;
    private InstanceOwnership? _ownership;

    public OwnedInstance()
    {
        var id = Guid.NewGuid().ToString("N");
        var names = new InstanceNames($"Bravo.Tests.{id}", $"Bravo.Tests.{id}");
        _thread = new Thread(() => Hold(names)) { IsBackground = true };
        _thread.Start();
        _acquired.Wait();

        Ownership = _ownership ?? throw new InvalidOperationException("The instance of the test already has an owner.");
    }

    public InstanceOwnership Ownership { get; }

    public void Dispose()
    {
        _release.Set();
        _thread.Join();
        _acquired.Dispose();
        _release.Dispose();
    }

    private void Hold(InstanceNames names)
    {
        _ownership = InstanceOwnership.TryAcquire(names, TimeSpan.Zero);
        _acquired.Set();
        _release.Wait();
        _ownership?.Dispose();
    }
}
