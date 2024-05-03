namespace Sqlbi.Bravo.Infrastructure.Helpers;

using Microsoft.Win32.SafeHandles;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using Sqlbi.Bravo.Models;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

internal static class ProcessHelper
{
    public static void RunOnSTAThread(Action action)
    {
        var start = new ThreadStart(action);
        var thread = new Thread(start);
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
            var handle = GetCurrentProcessMainWindowHandle();
            control = Control.FromHandle(handle);
        }

        if (control.InvokeRequired)
            control.Invoke(action);
        else
            action();
    }

    public static async Task<T> RunOnUISynchronizationContextContext<T>(Func<Task<T>> callback)
    {
        var previous = SynchronizationContext.Current;

        SynchronizationContext.SetSynchronizationContext(AppWindow.UISynchronizationContext);
        try
        {
            return await callback().ConfigureAwait(false);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    public static void RunOnUISynchronizationContext(Action action)
    {
        var previous = SynchronizationContext.Current;

        SynchronizationContext.SetSynchronizationContext(AppWindow.UISynchronizationContext);
        try
        {
            action();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
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
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = Environment.ExpandEnvironmentVariables("%WINDIR%\\explorer.exe"),
                Arguments = $"/root,\"{path}\""
            });

            return true;
        }

        return false;
    }

    public static bool OpenShellExecute(string path, bool waitForStarted, [NotNullWhen(true)] out int? processId, CancellationToken cancellationToken = default)
    {
        if (File.Exists(path))
        {
            var extension = Path.GetExtension(path);

            var allowed = extension.EqualsI(".pbix") || extension.EqualsI(".xlsx") || extension.EqualsI(".code-workspace");
            if (allowed)
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });

                if (process is not null)
                {
                    processId = process.Id;

                    if (waitForStarted && WaitForStarted(process, cancellationToken))
                        return true;
                }
            }
        }

        processId = null;
        return false;

        static bool WaitForStarted(Process process, CancellationToken cancellationToken)
        {
            try
            {
                _ = process.WaitForInputIdle(5_000);
            }
            catch (InvalidOperationException)
            {
                // ignore
            }

            if (process.IsPBIDesktop())
            {
                // We force 5 minutes which is the default timeout of HTTP requests
                var waitTimeout = TimeSpan.FromMinutes(5).TotalSeconds;

                for (var i = 0; i < waitTimeout; i++)
                {
                    // The HTTP request times out or user has cancelled the operation by using the "Cancel" button
                    cancellationToken.ThrowIfCancellationRequested();

                    // The PBIDesktop main window title is not null when the SSAS instance has started and the model is fully loaded
                    if (process.GetMainWindowTitle() is not null)
                        return true;

                    if (process.HasExited)
                        break;

                    Thread.Sleep(1_000);
                }
            }

            return !process.HasExited;
        }
    }

    public static bool Open(string path)
    {
        if (File.Exists(path))
        {
            return OpenShellExecute(path, waitForStarted: false, out _);
        }
        else if (Directory.Exists(path))
        {
            return OpenFileExplorer(path);
        }

        return false;
    }

    public static FileStream? GetPBIDesktopPBIXFile(int processId)
    {
        if (!Environment.Is64BitProcess || Environment.OSVersion.IsWindows7OrLower())
        {
            AppEnvironment.AddDiagnostics(DiagnosticMessageType.Text, name: $"{nameof(ProcessHelper)}.{nameof(GetPBIDesktopPBIXFile)}", content: "This method is only supported on 64-bit Windows OS version 8 or later.", DiagnosticMessageSeverity.Warning);
            return null;
        }

        var isPbiDesktopProcess = Ntdll.NtQuerySystemInformationProcessInformation(AppEnvironment.SessionId, AppEnvironment.PBIDesktopProcessImageName).ContainsKey(processId);
        if (isPbiDesktopProcess)
        {
            var path = GetPathFromCommandLineArgs(processId);
            if (path is not null)
                return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            return GetStreamFromFileHandles(processId);
        }

        return null;

        static string? GetPathFromCommandLineArgs(int processId)
        {
            var cmdline = Ntdll.NtQueryInformationProcessCommandLineInformation(processId);
            if (cmdline is not null)
            {
                // CommandLineToArgvW has slightly different behavior compared to the rules at https://learn.microsoft.com/en-us/cpp/cpp/main-function-command-line-args?view=msvc-170#parsing-c-command-line-arguments
                // See also System.Environement.SegmentCommandLine at https://github.com/dotnet/runtime/blob/9892d46d0789329919d9a17f275fd1fff603b76f/src/libraries/System.Private.CoreLib/src/System/Environment.Windows.cs#L223
                if (Shell32.CommandLineToArgs(cmdline) is { } args && args.Length > 0)
                {
                    var pbix = args.Where((arg) => arg.EndsWithI(".pbix") && File.Exists(arg)).ToArray();
                    if (pbix.Length == 1)
                        return pbix[0];
                }
            }
            return null;
        }

        static FileStream? GetStreamFromFileHandles(int processId)
        {
            var fileObjectTypeIndex = GetFileObjectTypeIndex();
            var fileObjects = Ntdll.NtQueryInformationProcessHandleInformation(processId).Where((h) => h.ObjectTypeIndex == fileObjectTypeIndex).ToArray();

            using var sourceProcessHandle = Ntdll.NtOpenProcess(processId, Ntdll.ProcessAccess.PROCESS_DUP_HANDLE);
            using var targetProcess = Process.GetCurrentProcess();

            foreach (var fileObject in fileObjects)
            {
                using var sourceHandle = new SafeFileHandle(fileObject.HandleValue, ownsHandle: false);

                if (!Kernel32.DuplicateHandle(sourceProcessHandle, sourceHandle, targetProcess.SafeHandle, out var lpTargetHandle))
                    continue;

                using var targetHandle = new SafeFileHandle(lpTargetHandle, ownsHandle: true);

                var deviceType = Ntdll.NtQueryVolumeInformationFileFsDeviceInformation(targetHandle);
                if (deviceType == Ntdll.FILE_DEVICE_TYPE.FILE_DEVICE_DISK)
                {
                    var path = Kernel32.GetFinalPathNameByHandle(targetHandle);

                    if (File.Exists(path) && Path.GetExtension(path) is { } ext && ext.EqualsI(".pbix"))
                    {
                        try
                        {
                            return File.OpenRead(path);
                        }
                        catch (IOException)
                        {
                            // skip "TempSaves" file which is locked by the PBIDesktop process
                        }
                    }
                }
            }

            return null;
        }

        static int GetFileObjectTypeIndex()
        {
            // Get the TypeIndex of the OBJECT_TYPE "File" for the current OS version. See also https://medium.com/@ashabdalhalim/a-light-on-windows-10s-object-header-typeindex-value-e8f907e7073a
            using var temp = new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, bufferSize: 4096, FileOptions.DeleteOnClose);
            var info = Ntdll.NtQueryObjectTypeInformation(temp.SafeFileHandle);
            return info.TypeIndex;
        }
    }

    public static string? GetMainWindowTitle(int processId)
    {
        using var process = SafeGetProcessById(processId);
        return process?.GetMainWindowTitle();
    }

    public static int[] GetProcessIdsByImageName(string imageName, StringComparison comparison = StringComparison.Ordinal, int? parentProcessId = null)
    {
        var pids = Ntdll.NtQuerySystemInformationProcessInformation(AppEnvironment.SessionId, imageName, comparison).Keys.ToArray();
        if (pids.Length > 0 && parentProcessId.HasValue)
        {
            var child = Kernel32.SnapshotProcess(parentProcessId.Value).Select((e) => e.th32ProcessID);
            pids = pids.Intersect(child).ToArray();
        }

        return pids;
    }

    public static (int Id, string ImageName, string? MainWindowTitle)? GetParentProcess()
    {
        var snapshot = Kernel32.SnapshotProcess().ToArray();

        var current = snapshot.Single((e) => e.th32ProcessID == Environment.ProcessId);
        var parent = snapshot.SingleOrDefault((e) => e.th32ProcessID == current.th32ParentProcessID);
        if (parent.th32ProcessID == default)
            return null;

        using var process = SafeGetProcessById(parent.th32ProcessID);
        var title = process?.GetMainWindowTitle();

        return (parent.th32ProcessID, parent.szExeFile, title);
    }

    public static IntPtr GetCurrentProcessMainWindowHandle()
    {
        using var process = Process.GetCurrentProcess();
        return process.MainWindowHandle;
    }

    /// <summary>
    /// Returns true if the app is running as an MSIX package on Windows 10, version 1709 (build 16299) or later
    /// </summary>
    public static bool IsRunningAsMsixPackage()
    {
        if (Environment.OSVersion.IsWindows7OrLower())
            return false;

        var name = Kernel32.GetCurrentPackageFullName();
        return name != null;
    }

    public static Process? SafeGetProcessById(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId); // Throws ArgumentException if the process specified by the processId parameter is not running.

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
}
