using System;
using System.IO;

namespace Bravo.Setup;

/// <summary>
/// The last defense of the process. An installation waits for the program and ignores how it ends: an
/// exception that nothing expected becomes an exit code and a line of text, and never a process that Windows
/// ends with an error report of its own.
/// </summary>
internal static class ProcessBoundary
{
    public static int Run(Func<int> program, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(error);

        try
        {
            return program();
        }
        catch (Exception exception)
        {
            error.WriteLine($"{exception.GetType().Name}: {exception.Message}");
            return ExitCode.Unexpected;
        }
    }
}