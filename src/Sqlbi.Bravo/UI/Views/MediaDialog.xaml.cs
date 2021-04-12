using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
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
            _logger = App.ServiceProvider.GetRequiredService<ILogger<MediaDialog>>();
            _logger.Trace();

            DialogTitle.Text = mediaOption.Title;
            DisplayedDescription.Text = mediaOption.Description;
            MediaPlayer.Source = new Uri(mediaOption.MediaLink);
            ProgressIndicator.IsActive = true;
        }

        private void OkClicked(object sender, RoutedEventArgs e)
        {
            _logger.Trace();

            Close();
        }

        private void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            _logger.Information(LogEvents.MediaDialogViewAction, "{@Details}", new object[] { new
            {
                Action = "MediaOpened",
                Uri = MediaPlayer.Source.AbsoluteUri
            }});

            ProgressIndicator.IsActive = false;
        }

        private void OnMediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            _logger.Error(LogEvents.MediaDialogExcetpion, e.ErrorException, "{@Details}", new object[] { new
            {
                Uri = MediaPlayer.Source.AbsoluteUri
            }});

            ErrorMessage.Visibility = Visibility.Visible;
            ProgressIndicator.IsActive = false;
        }

        private void OpenHyperlink(object sender, RequestNavigateEventArgs e)
        {
            _logger.Information(LogEvents.MediaDialogViewAction, "{@Details}", new object[] { new
            {
                Action = "RequestNavigate",
                Uri = e.Uri.AbsoluteUri
            }});

            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}