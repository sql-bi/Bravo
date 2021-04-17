using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Sqlbi.Bravo.Client.VertiPaqAnalyzer;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

            var startInfo = new ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true
            };

            Process.Start(startInfo);

            e.Handled = true;
        }

        private void HowToUseClicked(object sender, RoutedEventArgs e) => ShellView.Instance.ShowMediaDialog(new HowToUseBravoHelp());

        private async void AttachToPowerBIDesktopClicked(object sender, RoutedEventArgs e)
        {
            var service = App.ServiceProvider.GetRequiredService<IPowerBIDesktopService>();
            var instances = (await service.GetInstancesAsync()).ToArray();

            var options = new List<string>();

            bool NameIsUnique(string name) => instances.Count(i => i.Name == name) == 1;

            foreach (var instance in instances)
            {
                if (NameIsUnique(instance.Name))
                {
                    options.Add(instance.Name);
                }
                else
                {
                    options.Add($"{instance.Name} {instance.LocalEndPoint.ToString().Replace("127.0.0.1", string.Empty)}");
                }
            }

            var dlg = new ConnectDialog { Owner = Application.Current.MainWindow };
            dlg.ShowDesktopOptions(options);

            if (dlg.ShowDialog() == true && dlg.ResultIndex >= 0)
            {
                _logger.Information(LogEvents.StartConnectionAction, "{@Details}", new object[] { new
                                {
                                    Action = "AttachPowerBIDesktop"
                                }});

                var shellViewModel = App.ServiceProvider.GetRequiredService<ShellViewModel>();
                await shellViewModel.AddNewTabAsync(instances[dlg.ResultIndex]);
            }
        }

        private async void ConnectToPowerBIDatasetClicked(object sender, RoutedEventArgs e)
        {
            _logger.Trace();

            var service = App.ServiceProvider.GetRequiredService<IPowerBICloudService>();
            if (service.IsAuthenticated == false)
            {
                var loggedIn = await service.LoginAsync();
                if (loggedIn == false)
                {
                    return;
                }
            }

            Client.PowerBI.PowerBICloud.PowerBICloudSharedDataset[] datasets;

            try
            {
                datasets = (await service.GetDatasetsAsync()).ToArray();
            }
            catch (Exception exc)
            {
                _logger.Error(LogEvents.StartConnectionAction, exc);
                return;
            }

            var options = new List<OnlineDatasetSummary>();

            foreach (var dataset in datasets)
            {
                options.Add(new OnlineDatasetSummary
                {
                    DisplayName = dataset.Model.DisplayName,
                    Endorsement = dataset?.GalleryItem?.Status ?? 0,
                    Owner = $"{dataset.Model.CreatorUser.GivenName} {dataset.Model.CreatorUser.FamilyName}",
                    Refreshed = dataset.Model.LastRefreshTime,
                    Workspace = dataset.WorkspaceName,
                });
            }

            var dlg = new ConnectDialog { Owner = Application.Current.MainWindow };
            dlg.ShowOnlineDatasetOptions(options);

            if (dlg.ShowDialog() == true && dlg.ResultIndex >= 0)
            {
                _logger.Information(LogEvents.StartConnectionAction, "{@Details}", new object[] { new
                                {
                                    Action = "ConnectPowerBIDataset"
                                }});

                var shellViewModel = App.ServiceProvider.GetRequiredService<ShellViewModel>();
                await shellViewModel.AddNewTabAsync(datasets[dlg.ResultIndex]);
            }

            //await service.LogoutAsync();
        }

        private async void OpenVertiPaqAnalyzerFileClicked(object sender, RoutedEventArgs e)
        {
            _logger.Trace();

            var openFileDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Multiselect = false,
                Filter = "VertiPaq Analyzer file (*.vpax)|*.vpax",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            };

            if (openFileDialog.ShowDialog() == false)
            {
                return;
            }

            _logger.Information(LogEvents.StartConnectionAction, "{@Details}", new object[] { new
            {
                Action = "OpenVertiPaqAnalyzerFile"
            }});

            var vertiPaqAnalyzerFile = new VertiPaqAnalyzerFile
            {
                Path = openFileDialog.FileName
            };

            var shellViewModel = App.ServiceProvider.GetRequiredService<ShellViewModel>();
            await shellViewModel.AddNewTabAsync(vertiPaqAnalyzerFile);
        }
    }
}