using Microsoft.Identity.Client;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Navigation;

namespace Sqlbi.Bravo.Client.PowerBI.PowerBICloud
{
    public partial class CustomLoginWindow : Window
    {
        private readonly Uri _authorizationUri;
        private readonly Uri _redirectUri;
        private readonly TaskCompletionSource<Uri> _taskCompletionSource;
        private readonly CancellationToken _cancellationToken;
        private CancellationTokenRegistration _token;

        public CustomLoginWindow(Uri authorizationUri, Uri redirectUri, TaskCompletionSource<Uri> taskCompletionSource, CancellationToken cancellationToken)
        {
            InitializeComponent();

            _authorizationUri = authorizationUri;
            _redirectUri = redirectUri;
            _taskCompletionSource = taskCompletionSource;
            _cancellationToken = cancellationToken;
        }

        private void WebBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.Uri.ToString().StartsWith(_redirectUri.ToString()) == false)
            {
                return;
            }

            var query = HttpUtility.ParseQueryString(e.Uri.Query);
            if (query.AllKeys.Any((x) => "code".Equals(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                _taskCompletionSource.SetResult(e.Uri);
            }
            else
            {
                var errorCode = query.Get("error");
                var errorMessage = query.Get("error_description");
                var exception = new MsalException(errorCode, errorMessage);

                _taskCompletionSource.SetException(exception);
            }

            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _token = _cancellationToken.Register(() => _taskCompletionSource.SetCanceled());
            webBrowser.Navigate(_authorizationUri);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _taskCompletionSource.TrySetCanceled();
            _token.Dispose();
        }
    }
}
