namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Management;
    using System.Security.Claims;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    internal static class ProcessHelper
    {
        public static void RunOnSTAThread(Action action)
        {
            var threadStart = new ThreadStart(action);
            var thread = new Thread(threadStart);
            thread.CurrentCulture = thread.CurrentUICulture = CultureInfo.CurrentCulture;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        public static void InvokeOnUIThread(Control control, Action action) => InvokeOnUIThread(action, control);
        
        public static void InvokeOnUIThread(Action action, Control? control = null)
        {
            if (control is null)
            {
                var mainWindowHandle = GetCurrentProcessMainWindowHandle();
                control = Control.FromHandle(mainWindowHandle);
            }

            //if (!Application.MessageLoop)
            //{
            //}

            if (control.InvokeRequired)
            {
                control.Invoke(action);
            }
            else
            {
                action();
            }
        }

        public static async Task<T> RunOnUISynchronizationContextContext<T>(Func<Task<T>> callback)
        {
            var previousSynchronizationContext = SynchronizationContext.Current;

            SynchronizationContext.SetSynchronizationContext(AppWindow.UISynchronizationContext);
            try
            {
                var result = await callback();
                return result;
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousSynchronizationContext);
            }
        }

        public static void RunOnUISynchronizationContext(Action action)
        {
            var previousSynchronizationContext = SynchronizationContext.Current;

            SynchronizationContext.SetSynchronizationContext(AppWindow.UISynchronizationContext);
            try
            {
                action();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousSynchronizationContext);
            }
        }

        public static void OpenControlPanelItem(string canonicalName)
        {
            // https://docs.microsoft.com/en-us/windows/win32/shell/controlpanel-canonical-names
             
            var startInfo = new ProcessStartInfo
            {
                FileName = Environment.ExpandEnvironmentVariables("%WINDIR%\\System32\\control.exe"),
                Arguments = canonicalName
            };

            using var process = Process.Start(startInfo); 
        }

        public static bool OpenBrowser(Uri address)
        {
            if (address.IsAbsoluteUri && !address.IsFile && !address.IsUnc && !address.IsLoopback)
            {
                if (address.Scheme.EqualsI(Uri.UriSchemeHttps) || address.Scheme.EqualsI(Uri.UriSchemeHttp))
                {
                    if (AppEnvironment.TrustedUriHosts.Any((trustedHost) => address.Host.EqualsI(trustedHost) || address.Host.EndsWith($".{ trustedHost }", StringComparison.OrdinalIgnoreCase)))
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

        public static bool OpenFileExplorer(string path)
        {
            if (Directory.Exists(path))
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = Environment.ExpandEnvironmentVariables("%WINDIR%\\explorer.exe"),
                    Arguments = $"/root,\"{ path }\""
                };

                using var process = Process.Start(startInfo);
                return true;
            }

            return false;
        }

        public static bool OpenShellExecute(string path, bool waitForStarted, [NotNullWhen(true)] out int? processId, CancellationToken cancellationToken = default)
        {
            if (File.Exists(path))
            {
                const string Pbix = ".pbix";
                const string Xlsx = ".xlsx";

                var extension = Path.GetExtension(path);
                var isAllowed = (new[] { Pbix, Xlsx }).Any((ext) => ext.EqualsI(extension));
                var isPbix = extension.EqualsI(Pbix);

                if (isAllowed)
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    };

                    using var process = Process.Start(startInfo);

                    if (process is not null)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        processId = process.Id;

                        if (waitForStarted)
                        {
                            try
                            {
                                process.WaitForInputIdle(5_000);
                            }
                            catch (InvalidOperationException)
                            {
                                // ignore
                            }

                            if (isPbix)
                            {
                                for (var i = 0; i < 60; i++)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    if (process.HasExited)
                                        break;

                                    // If the result is null it means that the SSAS instance is not ready yet
                                    if (process.GetPBIDesktopMainWindowTitle() is not null)
                                        break;

                                    Thread.Sleep(1_000);
                                }
                            }
                        }

                        return true;
                    }
                }
            }

            processId = null;
            return false;
        }

        public static bool Open(string path)
        {
            if (File.Exists(path))
            {
                if (OpenShellExecute(path, waitForStarted: false, out _))
                    return true;
            }
            else if (Directory.Exists(path))
            {
                if (OpenFileExplorer(path))
                    return true;
            }

            return false;
        }

        public static IReadOnlyList<Process> GetProcessesByName(string processName)
        {
            var processes = Process.GetProcessesByName(processName).ToList();

            for (var i = processes.Count - 1; i >= 0; i--)
            {
                if (processes[i].SessionId != AppEnvironment.SessionId)
                {
                    processes[i].Dispose();
                    processes.RemoveAt(i);
                }
            }

            return processes;
        }

        public static Process? GetParentProcess()
        {
            // ManagementObjectSearcher.Get() raises a System.InvalidCastException when executed on the current thread, this regardless of the apartment state of the current thread (which is STA)
            //
            // System.InvalidCastException "Specified cast is not valid."
            //    at System.StubHelpers.InterfaceMarshaler.ConvertToNative(Object objSrc, IntPtr itfMT, IntPtr classMT, Int32 flags)
            //    at System.Management.SecuredIWbemServicesHandler.ExecQuery_(String strQueryLanguage, String strQuery, Int32 lFlags, IWbemContext pCtx, IEnumWbemClassObject& ppEnum)
            //    at System.Management.ManagementObjectSearcher.Get()

            var parentProcessId = (int?)null;

            RunOnSTAThread(GetImpl);

            var parentProcess = SafeGetProcessById(parentProcessId);
            return parentProcess;

            void GetImpl()
            {
                var queryString = $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = { AppEnvironment.ProcessId } AND SessionId = { AppEnvironment.SessionId }";

                using var searcher = new ManagementObjectSearcher(queryString);
                using var collection = searcher.Get();
                using var @object = collection.OfType<ManagementObject>().SingleOrDefault();

                if (@object is not null)
                {
                    parentProcessId = (int)(uint)@object["ParentProcessId"];
                }
            }
        }

        public static IntPtr GetCurrentProcessMainWindowHandle()
        {
            using var current = Process.GetCurrentProcess();

            return current.MainWindowHandle;
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

                if (process.SessionId != AppEnvironment.SessionId)
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

        public static bool IsUserAdministrator()
        {
            // Move to Infrastructure.Security namespace

            using var windowsIdentity = WindowsIdentity.GetCurrent();

            if (windowsIdentity is not null)
            {
                var windowsPrincipal = new WindowsPrincipal(windowsIdentity);
                var userClaims = new List<Claim>(windowsPrincipal.UserClaims);

                var builtinAdministratorsSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, domainSid: null);
                var isUserAdministrator = windowsPrincipal.UserClaims.Any((claim) => claim.Value.Contains(builtinAdministratorsSid.Value));

                return isUserAdministrator;
            }

            return false;
        }

        public static bool IsRunningAsAdministrator()
        {
            // Move to Infrastructure.Security namespace

            using var windowsIdentity = WindowsIdentity.GetCurrent();

            if (windowsIdentity?.Owner is not null)
            {
                var isRunningAsAdministrator = windowsIdentity.Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid);
                return isRunningAsAdministrator;
            }

            return false;
        }

        public static bool SafePredicate(Func<bool> predicate)
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
