using System;
using System.Threading;
using Bravo.Platform.Windows.SingleInstance;

namespace Bravo.Platform.Windows.Tests.SingleInstance;

/// <summary>
/// Holds the ownership of an instance on a thread of its own, as the entry thread of a process does: a mutex
/// is released by the thread that acquired it, and a test does not stay on one thread.
/// </summary>
internal sealed class OwnedInstance : IDisposable
{
    private readonly ManualResetEventSlim _acquired = new();
    private readonly ManualResetEventSlim _release = new();
    private readonly Thread _thread;
    private InstanceOwnership? _ownership;
    private bool _abandon;

    public OwnedInstance(InstanceNames names)
    {
        _thread = new Thread(() => Hold(names)) { IsBackground = true };
        _thread.Start();
        _acquired.Wait();

        Ownership = _ownership ?? throw new InvalidOperationException("The instance already has an owner.");
    }

    public InstanceOwnership Ownership { get; }

    /// <summary>
    /// Ends the owner without releasing the ownership, as a process that is killed does.
    /// </summary>
    public void EndWithoutReleasing()
    {
        _abandon = true;
        Dispose();
    }

    public void Dispose()
    {
        _release.Set();
        _thread.Join();
    }

    private void Hold(InstanceNames names)
    {
        _ownership = InstanceOwnership.TryAcquire(names, TimeSpan.Zero);
        _acquired.Set();
        _release.Wait();

        if (!_abandon)
        {
            _ownership?.Dispose();
        }
    }
}
