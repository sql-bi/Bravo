using System;
using System.Windows;
using MahApps.Metro.Controls;
using Sqlbi.Bravo.UI.DataModel;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class MediaDialog : MetroWindow
    {
        public MediaDialog() => InitializeComponent();

        public MediaDialog(IInAppMediaOption mediaOption) : this()
        {
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
            // TODO REQUIREMENTS: Add some logging for this?
            // TODO REQUIREMENTS: What to do if can't play the specified media?
            ProgressIndicator.IsActive = false;
        }
    }
}
