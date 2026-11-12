using System;
using System.Threading;

namespace Bravo.Platform.Windows.SingleInstance;

/// <summary>
/// The ownership of the instance: one process at a time holds it, from the moment it is acquired until it is
/// disposed or the process ends.
/// </summary>
/// <remarks>
/// A mutex belongs to a thread: the ownership is disposed on the thread that acquired it.
/// </remarks>
public sealed class InstanceOwnership : IDisposable
{
    private readonly Mutex _mutex;
    private bool _disposed;

    private InstanceOwnership(InstanceNames names, Mutex mutex)
    {
        Names = names;
        _mutex = mutex;
    }

    public InstanceNames Names { get; }

    /// <summary>
    /// Takes the ownership, waiting at most <paramref name="wait"/> for the owner to release it.
    /// </summary>
    /// <returns>The ownership, or <see langword="null"/> when another owner holds it.</returns>
    /// <exception cref="UnauthorizedAccessException">The mutex exists and this process may not open it.</exception>
    /// <exception cref="WaitHandleCannotBeOpenedException">The mutex exists and is not of the current user.</exception>
    public static InstanceOwnership? TryAcquire(InstanceNames names, TimeSpan wait)
    {
        ArgumentNullException.ThrowIfNull(names);

        var options = new NamedWaitHandleOptions { CurrentUserOnly = true, CurrentSessionOnly = true };
        var mutex = new Mutex(initiallyOwned: false, names.MutexName, options);
        try
        {
            if (!mutex.WaitOne(wait))
            {
                mutex.Dispose();
                return null;
            }
        }
        catch (AbandonedMutexException)
        {
            // The owner ended without releasing the mutex, which now belongs to this thread.
        }
        catch
        {
            mutex.Dispose();
            throw;
        }

        return new InstanceOwnership(names, mutex);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
