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
            Title.Text = mediaOption.Title;
            DisplayedDescription.Text = mediaOption.Description;
            MediaPlayer.Source = new Uri(mediaOption.MediaLink);
        }

        private void OkClicked(object sender, RoutedEventArgs e) => _ = Close();
    }
}
