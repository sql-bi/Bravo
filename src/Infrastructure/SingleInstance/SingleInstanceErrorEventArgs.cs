using System;

namespace Sqlbi.Bravo.Infrastructure.SingleInstance;

/// <summary>
/// Reports a failure that happened on the listener loop, where there is no caller to return it to.
/// Modelled on <see cref="System.IO.FileSystemWatcher.Error"/>: the component owns no logging
/// dependency, so the host decides what to do with it.
/// </summary>
internal sealed class SingleInstanceErrorEventArgs(Exception exception) : EventArgs
{
    public Exception Exception { get; } = exception;
}
