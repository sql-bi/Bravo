namespace Sqlbi.Bravo.Infrastructure.Extensions;

using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using System.Diagnostics;
using System.Text;

internal static class ProcessExtensions
{
    public static bool IsPBIDesktop(this Process process)
    {
        try
        {
            return process.ProcessName.Equals(AppEnvironment.PBIDesktopProcessName, StringComparison.Ordinal);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static string? GetMainWindowTitle(this Process process)
    {
        if (process.HasExited)
            return null;

        process.Refresh(); // Ensure MainWindowTitle is up-to-date

        var title = process.MainWindowTitle;

        if (title.Length == 0)
            title = GetFromThreads(process);

        if (title.Length > 0 && process.IsPBIDesktop())
            title = RemovePBIDesktopSuffix(title);

        return title.Length > 0 ? title : null;

        static string GetFromThreads(Process process)
        {
            ProcessThreadCollection threads;
            try
            {
                threads = process.Threads;
            }
            catch (InvalidOperationException)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(capacity: 1_000);

            foreach (ProcessThread thread in threads)
            {
                User32.EnumThreadWindows(thread.Id, (hWnd, lParam) =>
                {
                    if (User32.IsWindowVisible(hWnd))
                    {
                        User32.SendMessage(hWnd, User32.WindowMessage.WM_GETTEXT, builder.Capacity, builder);

                        var value = builder.ToString();
                        if (value.Length > 0)
                            return false;
                    }

                    return true;
                },
                IntPtr.Zero);

                if (builder.Length > 0)
                    break;
            }

            return builder.ToString();
        }

        static string RemovePBIDesktopSuffix(string title)
        {
            foreach (var suffix in AppEnvironment.PBIDesktopMainWindowTitleSuffixes)
            {
                var index = title.LastIndexOf(suffix);
                if (index >= 0)
                {
                    title = title[..index];
                    break;
                }
            }

            return title;
        }
    }
}
