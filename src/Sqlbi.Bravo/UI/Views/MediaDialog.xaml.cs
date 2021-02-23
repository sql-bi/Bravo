using System;
using System.Diagnostics;
using System.Windows;
using MahApps.Metro.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.UI.DataModel;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class MediaDialog : MetroWindow
    {
        private readonly ILogger _logger;

        public MediaDialog() => InitializeComponent();

        public MediaDialog(IInAppMediaOption mediaOption) : this()
        {
            _logger = App.ServiceProvider.GetRequiredService<ILogger<App>>();
            _logger.Trace();

            DialogTitle.Text = mediaOption.Title;
            DisplayedDescription.Text = mediaOption.Description;
            MediaPlayer.Source = new Uri(mediaOption.MediaLink);
            ProgressIndicator.IsActive = false;
        }

        private void OkClicked(object sender, RoutedEventArgs e) => Close();

        private void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            ProgressIndicator.IsActive = false;
        }

        private void OnMediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            _logger.Trace();
            _logger.Error(LogEvents.MediaPlaybackFailure, $"Unable to play: {MediaPlayer.Source}");

            ErrorMessage.Visibility = Visibility.Visible;

            ProgressIndicator.IsActive = false;
        }

        private void OpenHyperlink(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
