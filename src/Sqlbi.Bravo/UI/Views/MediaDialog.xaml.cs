using System;
using System.Windows;
using MahApps.Metro.SimpleChildWindow;
using Sqlbi.Bravo.UI.DataModel;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class MediaDialog : ChildWindow
    {
        public MediaDialog() => InitializeComponent();

        public MediaDialog(IInAppMediaOption mediaOption) : this()
        {
            DialogTitle.Text = mediaOption.Title;
            DisplayedDescription.Text = mediaOption.Description;
            MediaPlayer.Source = new Uri(mediaOption.MediaLink);
            ProgressIndicator.IsActive = false;
        }

        private void OkClicked(object sender, RoutedEventArgs e) => _ = Close();

        private void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            ProgressIndicator.IsActive = false;
        }

        private void OnMediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            // TODO: Add some logging for this?
            ProgressIndicator.IsActive = false;
        }
    }
}
