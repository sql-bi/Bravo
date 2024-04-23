namespace Sqlbi.Bravo.Infrastructure.Helpers;

using Sqlbi.Bravo.Infrastructure.Windows;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

internal static class WindowDialogHelper
{
    public static bool OpenFileDialog(string filter, [NotNullWhen(true)] out string? path, CancellationToken cancellationToken)
    {
        using var dialog = new System.Windows.Forms.OpenFileDialog()
        {
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.DoNotVerify),
            Filter = filter,
            Title = "Open file",
            ShowReadOnly = false,
            CheckFileExists = true
        };

        if (!cancellationToken.IsCancellationRequested)
        {
            var dialogResult = DialogResult.None;

            ProcessHelper.RunOnSTAThread(() => dialogResult = dialog.ShowDialog(owner: new Win32WindowWrapper(ProcessHelper.GetCurrentProcessMainWindowHandle())));

            if (dialogResult == DialogResult.OK)
            {
                path = dialog.FileName;
                return true;
            }
        }

        path = null;
        return false;
    }

    public static bool SaveFileDialog(string? fileName, string? filter, string defaultExt, [NotNullWhen(true)] out string? path, CancellationToken cancellationToken)
    {
        var defaultExtLowercase = defaultExt.ToLower();

        using var dialog = new System.Windows.Forms.SaveFileDialog()
        {
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.DoNotVerify),
            Filter = filter ?? $"{ defaultExt } files (*.{ defaultExtLowercase })|*.{ defaultExtLowercase }|All files (*.*)|*.*",
            Title = "Save file",
            DefaultExt = defaultExtLowercase,
            FileName = fileName
        };

        if (!cancellationToken.IsCancellationRequested)
        {
            var dialogResult = DialogResult.None;

            ProcessHelper.RunOnSTAThread(() => dialogResult = dialog.ShowDialog(owner: new Win32WindowWrapper(ProcessHelper.GetCurrentProcessMainWindowHandle())));

            if (dialogResult == DialogResult.OK)
            {
                path = dialog.FileName!;
                return true;
            }
        }

        path = null;
        return false;
    }

    public static bool BrowseFolderDialog([NotNullWhen(true)] out string? path, CancellationToken cancellationToken)
    {
        using var dialog = new FolderBrowserDialog()
        {
            RootFolder = Environment.SpecialFolder.MyDocuments,
            ShowNewFolderButton = true,
        };
        
        if (!cancellationToken.IsCancellationRequested)
        {
            var dialogResult = DialogResult.None;

            ProcessHelper.RunOnSTAThread(() => dialogResult = dialog.ShowDialog(owner: new Win32WindowWrapper(ProcessHelper.GetCurrentProcessMainWindowHandle())));

            if (dialogResult == DialogResult.OK)
            {
                path = dialog.SelectedPath;
                return true;
            }
        }

        path = null;
        return false;
    }
}
