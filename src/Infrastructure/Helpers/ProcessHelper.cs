using Sqlbi.Bravo.Infrastructure.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    public static class ProcessHelper
    {
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
        public static Process? GetCurrentProcessParent()
        {
            using var current = Process.GetCurrentProcess();
            var parent = current.GetParent();

            return parent;
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
