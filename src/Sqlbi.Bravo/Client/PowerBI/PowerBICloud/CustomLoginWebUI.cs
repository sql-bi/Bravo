using Microsoft.Identity.Client.Extensibility;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Sqlbi.Bravo.Client.PowerBI.PowerBICloud
{
    // Originally from https://github.com/runceel/EmbeddedMsalCustomWebUi.Wpf
    public class CustomLoginWebUI : ICustomWebUi
    {
        public const int DefaultWindowWidth = 600;
        public const int DefaultWindowHeight = 800;

        private readonly Window _owner;
        private readonly string _title;
        private readonly int _windowWidth;
        private readonly int _windowHeight;
        private readonly WindowStartupLocation _windowStartupLocation;

        public CustomLoginWebUI(Window owner, string title = "Sign in", int windowWidth = DefaultWindowWidth, int windowHeight = DefaultWindowHeight, WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterOwner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _title = title;
            _windowWidth = windowWidth;
            _windowHeight = windowHeight;
            _windowStartupLocation = windowStartupLocation;
        }

        public Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<Uri>();

            _owner.Dispatcher.Invoke(() =>
            {
                var window = new CustomLoginWindow(authorizationUri, redirectUri, taskCompletionSource, cancellationToken)
                {
                    Owner = _owner,
                    Title = _title,
                    Width = _windowWidth,
                    Height = _windowHeight,
                    WindowStartupLocation = _windowStartupLocation,
                };

                window.ShowDialog();
            });

            return taskCompletionSource.Task;
        }
    }
}
