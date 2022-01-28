namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;

    public static class ProcessHelper
    {
        public static bool OpenInBrowser(Uri address)
        {
            if (address.Scheme.EqualsI(Uri.UriSchemeHttps) || address.Scheme.EqualsI(Uri.UriSchemeHttp))
            {
                if (address.IsAbsoluteUri && !address.IsFile && !address.IsUnc && !address.IsLoopback)
                {
                    if (AppConstants.ApplicationTrustedUriHosts.Any((trustedHost) => address.Host.EqualsI(trustedHost) || address.Host.EndsWith($".{ trustedHost }", StringComparison.OrdinalIgnoreCase)))
                    {
                        using var process = Process.Start(new ProcessStartInfo
                        {
                            FileName = address.OriginalString,
                            UseShellExecute = true,
                        });

                        return true;
                    }
                }
            }

            return false;
        }

        public static IReadOnlyList<Process> GetProcessesByName(string processName)
        {
            var processes = Process.GetProcessesByName(processName).ToList();

            for (var i = processes.Count - 1; i >= 0; i--)
            {
                if (processes[i].SessionId != AppConstants.CurrentSessionId)
                {
                    processes[i].Dispose();
                    processes.RemoveAt(i);
                }
            }

            return processes;
        }

        public static Process? GetParentProcess()
        {
            using var current = Process.GetCurrentProcess();
            var parent = current.GetParent();

            return parent;
        }

        public static IntPtr GetParentProcessMainWindowHandle()
        {
            using var parent = GetParentProcess();
            
            if (parent is not null)
                return parent.MainWindowHandle;

            return IntPtr.Zero;
        }

        public static Process? SafeGetProcessById(int? processId)
        {
            if (processId is null)
                return null;

            try
            {
                var process = Process.GetProcessById(processId.Value); // Throws ArgumentException if the process specified by the processId parameter is not running.

                if (process.SessionId != AppConstants.CurrentSessionId)
                    return null;

                if (process.HasExited)
                    return null;

                _ = process.ProcessName; // Throws InvalidOperationException if the process has exited, so the requested information is not available

                return process;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                return null;
            }
        }

        private static bool SafePredicate(Func<bool> predicate)
        {
            try
            {
                return predicate();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is Win32Exception)
            {
                return false;
            }
        }
    }
}
