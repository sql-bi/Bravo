using System;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// What the application does with an exception that nothing caught, before the process ends.
/// </summary>
internal interface IFatalExceptionHandler
{
    /// <summary>
    /// Called once, on the thread where the exception escaped. The process ends when it returns.
    /// </summary>
    void Handle(Exception exception);
}
