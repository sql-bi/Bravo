using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Services.PowerBI;

namespace Sqlbi.Bravo.Infrastructure.Helpers;

internal static class ProcessHelper
{
    public static T RunOnSTAThread<T>(Func<T> func)
    {
        T result = default!;

        RunOnSTAThread(() =>
        {
            result = func();
        });

        return result;
    }

    public static void RunOnSTAThread(Action action)
    {
        Exception? exception = null;

        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.CurrentCulture = CultureInfo.CurrentCulture;
        thread.CurrentUICulture = CultureInfo.CurrentUICulture;
        // Set as background to ensure that it does not prevent the process from exiting
        thread.IsBackground = true;
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            // Re-throw the exception on the calling thread to preserve the original stack trace
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
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

        // Control.FromHandle returns null when the handle does not belong to a control of this
        // process: there is no UI thread to marshal to, so the action runs on the calling thread.
        if (control is not null && control.InvokeRequired)
        {
            control.Invoke(action);
        }
        else
        {
            action();
        }
    }

    /// <summary>
    /// Makes <see cref="AppWindow.UISynchronizationContext"/> the ambient <see cref="SynchronizationContext.Current"/>
    /// while <paramref name="callback"/> runs - it does not itself run anything on the UI thread, it only lets code
    /// that reads the ambient context capture the right one. Keep <paramref name="callback"/> synchronous - any
    /// <see langword="await"/> inside it would resume after this method has already restored the previous context.
    /// </summary>
    public static T RunWithUISynchronizationContext<T>(Func<T> callback)
    {
        var previousSynchronizationContext = SynchronizationContext.Current;

        SynchronizationContext.SetSynchronizationContext(AppWindow.UISynchronizationContext);
        try
        {
            return callback();
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
                if (AppEnvironment.TrustedUriHosts.Any((trustedHost) => address.Host.EqualsI(trustedHost) || address.Host.EndsWith($".{trustedHost}", StringComparison.OrdinalIgnoreCase)))
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
                Arguments = $"/root,\"{path}\""
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
            const string CodeWorkspace = ".code-workspace";

            var extension = Path.GetExtension(path);
            var isAllowed = (new[] { Pbix, Xlsx, CodeWorkspace }).Any((ext) => ext.EqualsI(extension));
            var isPbix = extension.EqualsI(Pbix);

            if (isAllowed)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                };

                using var process = Process.Start(startInfo);

                if (process is not null && !process.HasExited)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (waitForStarted)
                    {
                        try
                        {
                            _ = process.WaitForInputIdle(5_000);
                        }
                        catch (InvalidOperationException)
                        {
                            // ignore
                        }

                        if (isPbix)
                        {
                            // We force 5 minutes which is the default timeout of HTTP requests
                            var waitTimeout = TimeSpan.FromMinutes(5).TotalSeconds;

                            for (var i = 0; i < waitTimeout; i++)
                            {
                                // Cancellation can be requested by the user via the "Cancel" button or when the HTTP request times out.
                                cancellationToken.ThrowIfCancellationRequested();

                                if (process.HasExited)
                                {
                                    processId = null;
                                    return false;
                                }

                                // If the report name is null it means that the SSAS instance is not yet started and/or the model is not yet fully loaded
                                var isAvailable = PowerBIDesktop.GetReportName(process.GetMainWindowTitle()) is not null;
                                if (isAvailable)
                                    break;

                                Thread.Sleep(1_000);
                            }
                        }
                    }

                    processId = process.Id;
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

    public static IntPtr GetCurrentProcessMainWindowHandle()
    {
        using var current = Process.GetCurrentProcess();

        return current.MainWindowHandle;
    }

    public static Process? SafeGetProcessById(int? processId)
    {
        if (processId is null)
            return null;

        if (!Process.TryGetById(processId.Value, AppEnvironment.SessionId, out var process))
            return null;

        return process;
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
