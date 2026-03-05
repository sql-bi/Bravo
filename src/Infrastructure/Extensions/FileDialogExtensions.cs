namespace Sqlbi.Bravo.Infrastructure.Extensions;

using Sqlbi.Bravo.Infrastructure.Helpers;
using System.Windows.Forms;

internal static class FileDialogExtensions
{
    public static DialogResult ShowDialogOnStaThread(this FileDialog dialog)
    {
        return ProcessHelper.RunOnSTAThread(() =>
        {
            var handle = ProcessHelper.GetCurrentProcessMainWindowHandle();
            var window = NativeWindow.FromHandle(handle);

            return dialog.ShowDialog(owner: window);
        });
    }
}
