using Microsoft.Identity.Client.Extensibility;
using System;
using System.Threading;
using System.Threading.Tasks;
//using System.Windows;

namespace Sqlbi.Bravo.Infrastructure.Authentication
{
    // Originally from https://github.com/runceel/EmbeddedMsalCustomWebUi.Wpf
    //internal class LoginWebUI : ICustomWebUi
    //{
    //    private readonly Window _owner;

    //    public LoginWebUI(Window owner) => _owner = owner ?? throw new ArgumentNullException(nameof(owner));

    //    public Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken cancellationToken)
    //    {
    //        var taskCompletionSource = new TaskCompletionSource<Uri>();

    //        _owner.Dispatcher.Invoke(() =>
    //        {
    //            var window = new LoginWindow(authorizationUri, redirectUri, taskCompletionSource, cancellationToken)
    //            {
    //                Owner = _owner
    //            };

    //            window.ShowDialog();
    //        });

    //        return taskCompletionSource.Task;
    //    }
    //}
}
