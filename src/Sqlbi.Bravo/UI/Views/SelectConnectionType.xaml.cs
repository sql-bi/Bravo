using Dax.Vpax.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class SelectConnectionType : UserControl
    {
        private readonly ILogger _logger;

        public SelectConnectionType()
        {
            InitializeComponent();
            
            _logger = App.ServiceProvider.GetRequiredService<ILogger<SelectConnectionType>>();
        }

        private void RequestNavigateHyperlink(object sender, RequestNavigateEventArgs e)
        {
            _logger.Information(LogEvents.NavigateHyperlink, "{@Details}", new object[] { new
            {
                Uri = e.Uri.AbsoluteUri
            }});

            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void HowToUseClicked(object sender, RoutedEventArgs e) => ShellView.Instance.ShowMediaDialog(new HowToUseBravoHelp());

        private void AttachToWindowClicked(object sender, RoutedEventArgs e)
        {
            _logger.Trace();

            var instances = App.ServiceProvider.GetRequiredService<IPowerBIDesktopService>().GetInstances();

            foreach (var instance in instances)
            {
                _ = MessageBox.Show($"{ instance.Name } @ { instance.LocalEndPoint }", "TODO", MessageBoxButton.OK);                
            }

            // TODO REQUIREMENTS: need to know how to connect here
            _ = MessageBox.Show(
                "Need to attach to an active window",
                "TODO",
                MessageBoxButton.OK,
                MessageBoxImage.Question);

            _logger.Information(LogEvents.StartConnectionAction, "{@Details}", new object[] { new
            {
                Action = "AttachPowerBIDesktop"
            }});
        }

        private async void ConnectToDatasetClicked(object sender, RoutedEventArgs e)
        {
            _logger.Trace();

            #region Test

            var service = App.ServiceProvider.GetRequiredService<IPowerBICloudService>();

            var messageBoxResult = MessageBox.Show("YES = System Browser, NO = Custom UI", "Login options", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                //
                // Testing custom UI
                //
                var loginResult = await service.LoginWithCustomUIAsync();
                if (loginResult == false)
                {
                    _ = MessageBox.Show("Login failed", "TODO", MessageBoxButton.OK);
                    return;
                }
            }
            else if (messageBoxResult == MessageBoxResult.Yes)
            {
                //
                // Testing system browser
                //
                using var loginCancellationTokenSource = new CancellationTokenSource();
                var dialogInfo1 = new DebugDialogInfo();
                var loginTask = service.LoginWithSystemBrowserAsync(() => Dispatcher.Invoke(dialogInfo1.Close), loginCancellationTokenSource.Token);
                dialogInfo1.listboxMessages.Items.Add($"Login in progress using system browser ...");
                dialogInfo1.listboxMessages.Items.Add($"Option 1 ->\tComplete authentication within the expected timeout ({ service.LoginTimeout.TotalSeconds } seconds)");
                dialogInfo1.listboxMessages.Items.Add($"Option 2 ->\tClose this window to abort the login operation");

                var dialogResultCancel = dialogInfo1.ShowDialog();
                if (dialogResultCancel == true)
                {
                    loginCancellationTokenSource.Cancel();
                    MessageBox.Show($"Login canceled by user", "TODO", MessageBoxButton.OK);
                    return;
                }

                var loginResult = await loginTask;
                if (loginResult == false)
                {
                    _ = MessageBox.Show("Login failed or timeout expired", "TODO", MessageBoxButton.OK);
                    return;
                }
            }
            else
            {
                return;
            }

            _ = MessageBox.Show($"{ service.Account.Username } - { service.Account.Environment } @ TenantId { service.Account.HomeAccountId.TenantId } ", "TODO", MessageBoxButton.OK);

            // Display workspaces info
            {
                var dialogInfo2 = new DebugDialogInfo();

                var datasets = await service.GetSharedDatasetsAsync();
                foreach (var dataset in datasets)
                    dialogInfo2.listboxMessages.Items.Add($"{ dataset.WorkspaceName }({ dataset.WorkspaceType }) - { dataset.Model.DisplayName }({ dataset.Model.Description })");

                dialogInfo2.ShowDialog();
            }

            await service.LogoutAsync();

            #endregion

            // TODO REQUIREMENTS: need to know how to connect here
            _ = MessageBox.Show(
                "Need to sign-in and connect to a dataset",
                "TODO",
                MessageBoxButton.OK,
                MessageBoxImage.Question);

            _logger.Information(LogEvents.StartConnectionAction, "{@Details}", new object[] { new
            {
                Action = "ConnectPowerBIDataset"
            }});
        }

        private void OpenVertipaqFileClicked(object sender, RoutedEventArgs e)
        {
            _logger.Trace();

            var openFileDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Multiselect = false,
                Filter = "Vertipaq files (*.vpax)|*.vpax",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _logger.Information(LogEvents.StartConnectionAction, "{@Details}", new object[] { new
                {
                    Action = "OpenVertipaqFile"
                }});

                var fileContent = VpaxTools.ImportVpax(openFileDialog.FileName);

                var vm = DataContext as TabItemViewModel;
                vm.ConnectionType = BiConnectionType.VertipaqAnalyzerFile;
                vm.ConnectionName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                vm.AnalyzeModelVm.OnPropertyChanged(nameof(AnalyzeModelViewModel.ConnectionName));
                vm.ShowAnalysisOfLoadedModel(fileContent.DaxModel);
            }
        }
    }
}
