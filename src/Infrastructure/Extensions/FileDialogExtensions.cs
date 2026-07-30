using System.Windows.Forms;
using Sqlbi.Bravo.Infrastructure.Helpers;

namespace Sqlbi.Bravo.Infrastructure.Extensions;

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
