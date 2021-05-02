using MahApps.Metro.Controls;
using Sqlbi.Bravo.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class ConnectDialog : MetroWindow, INotifyPropertyChanged
    {
        private string searchText;

        public ConnectDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        public ObservableCollection<string> ActiveDesktops { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<OnlineDatasetSummary> OnlineDatasets { get; set; } = new ObservableCollection<OnlineDatasetSummary>();

        public string SearchText
        {
            get => searchText;
            set
            {
                searchText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VisibleOnlineDatasets)));
            }
        }

        public ObservableCollection<OnlineDatasetSummary> VisibleOnlineDatasets
        {
            get
            {
                if (string.IsNullOrEmpty(SearchText))
                {
                    return OnlineDatasets;
                }

                var items = OnlineDatasets.Where(
                    d => d.DisplayName.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                      || d.Owner.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                      || d.Workspace.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)).ToList();
                var collection = new ObservableCollection<OnlineDatasetSummary>(items);

                return collection;
            }
        }

        public int ResultIndex { get; private set; } = -1;

        public event PropertyChangedEventHandler PropertyChanged;

        public void ShowDesktopOptions(List<string> options)
        {
            ActiveDesktops.Clear();

            foreach (var item in options)
            {
                ActiveDesktops.Add(item);
            }

            if (options.Count > 0)
            {
                ActiveDesktopList.SelectedIndex = 0;
            }
            else
            {
                EmptyDesktopListMessage.Visibility = Visibility.Visible;
            }

            TheTabControl.SelectedIndex = 0;
            DesktopTab.IsEnabled = true;
            DatasetsTab.IsEnabled = false;
        }

        public void ShowOnlineDatasetOptions(List<OnlineDatasetSummary> options)
        {
            OnlineDatasets.Clear();

            foreach (var item in options)
            {
                OnlineDatasets.Add(item);
            }

            if (options.Count > 0)
            {
                OnlineDatasetsGrid.SelectedIndex = 0;
            }
            else
            {
                EmptyOnlineDatasetsListMessage.Visibility = Visibility.Visible;
            }

            TheTabControl.SelectedIndex = 1;
            DesktopTab.IsEnabled = false;
            DatasetsTab.IsEnabled = true;
        }

        private void AttachClicked(object sender, RoutedEventArgs e)
        {
            ResultIndex = ActiveDesktopList.SelectedIndex;
            DialogResult = true;
            Close();
        }

        private void ConnectClicked(object sender, RoutedEventArgs e)
        {
            ResultIndex = OnlineDatasetsGrid.SelectedIndex;
            DialogResult = true;
            Close();
        }

        private void DatagridEditingHandler(object sender, DataGridBeginningEditEventArgs e)
        {
            //// The DataGrids cannot be edited but the default behavior allows it.
            //// If don't do this we get a crash 'EditItem is not allowed' when a text entry is doubleclicked.
            e.Cancel = true;
        }
    }
}
