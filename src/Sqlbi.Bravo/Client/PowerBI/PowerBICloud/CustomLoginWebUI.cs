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
        private readonly Window _owner;

        public CustomLoginWebUI(Window owner) => _owner = owner ?? throw new ArgumentNullException(nameof(owner));

        public Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<Uri>();

            _owner.Dispatcher.Invoke(() =>
            {
                var window = new CustomLoginWindow(authorizationUri, redirectUri, taskCompletionSource, cancellationToken)
                {
                    Owner = _owner
                };

                window.ShowDialog();
            });

            return taskCompletionSource.Task;
        }
    }
}
