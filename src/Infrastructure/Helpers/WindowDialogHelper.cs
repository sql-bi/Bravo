namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Sqlbi.Bravo.Infrastructure.Windows;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    internal static class WindowDialogHelper
    {
        public static bool OpenFileDialog(string filter, [NotNullWhen(true)] out string? path, CancellationToken cancellationToken)
        {
            var dialogOwner = Win32WindowWrapper.CreateFrom(ProcessHelper.GetCurrentProcessMainWindowHandle());
            var dialogResult = System.Windows.Forms.DialogResult.None;

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
                ProcessHelper.RunOnSTAThread(() => dialogResult = dialog.ShowDialog(dialogOwner));

                //var dialog2 = new Bravo.Infrastructure.Windows.SaveFileDialog
                //{
                //    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.DoNotVerify),
                //    Filter = "Vpax files (*.vpax)|*.vpax|All files (*.*)|*.*",
                //    Title = "Export file",
                //    DefaultExt = "vpax",
                //    //FileName = fileName
                //};
                //var result = dialog2.ShowDialog(hWnd: Process.GetCurrentProcess().MainWindowHandle);
            }

            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                path = dialog.FileName;
                return true;
            }
            else
            {
                path = null;
                return false;
            }
        }

        public static bool SaveFileDialog(string? fileName, string? filter, string defaultExt, [NotNullWhen(true)] out string? path, CancellationToken cancellationToken)
        {
            var dialogOwner = Win32WindowWrapper.CreateFrom(ProcessHelper.GetCurrentProcessMainWindowHandle());
            var dialogResult = System.Windows.Forms.DialogResult.None;
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
                ProcessHelper.RunOnSTAThread(() => dialogResult = dialog.ShowDialog(dialogOwner));

                //var dialog2 = new Bravo.Infrastructure.Windows.SaveFileDialog
                //{
                //    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.DoNotVerify),
                //    Filter = "Vpax files (*.vpax)|*.vpax|All files (*.*)|*.*",
                //    Title = "Export file",
                //    DefaultExt = "vpax",
                //    //FileName = fileName
                //};
                //var result = dialog2.ShowDialog(hWnd: Process.GetCurrentProcess().MainWindowHandle);
            }

            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                path = dialog.FileName!;
                return true;
            }
            else
            {
                path = null;
                return false;
            }
        }

        public static bool BrowseFolderDialog([NotNullWhen(true)] out string? path, CancellationToken cancellationToken)
        {
            var dialogOwner = Win32WindowWrapper.CreateFrom(ProcessHelper.GetCurrentProcessMainWindowHandle());
            var dialogResult = System.Windows.Forms.DialogResult.None;

            using var dialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                RootFolder = Environment.SpecialFolder.MyDocuments,
                ShowNewFolderButton = true,
            };
            
            if (!cancellationToken.IsCancellationRequested)
            {
                ProcessHelper.RunOnSTAThread(() => dialogResult = dialog.ShowDialog(dialogOwner));
            }

            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                path = dialog.SelectedPath;
                return true;
            }
            else
            {
                path = null;
                return false;
            }
        }
    }
}
