using System;
using System.Drawing;
using System.Windows.Forms;
using Bravo.Desktop.Interop;
using Bravo.Desktop.WebView;

namespace Bravo.Desktop.Shell;

internal sealed class MainWindow : Form, IMainWindow
{
    private const string IconResourceName = "Bravo.ico";

    // What the window shows until the page has loaded: the background of the user interface in the color
    // mode of the system, so that nothing flashes white.
    private static readonly Color s_darkBackground = Color.FromArgb(0x20, 0x20, 0x20);
    private static readonly Color s_lightBackground = Color.FromArgb(0xF3, 0xF3, 0xF3);

    private readonly WebViewHost _webView;
    private readonly ShellExit _exit;
    private volatile bool _closeRequested;

    public MainWindow(BuildIdentity identity, WebViewHost webView, ShellExit exit)
    {
        _webView = webView;
        _exit = exit;

        Text = identity.WindowTitle;
        Icon = LoadIcon();
        BackColor = Application.SystemColorMode == SystemColorMode.Dark ? s_darkBackground : s_lightBackground;

        // The size that Windows gives a new window, in the middle of the screen. Nothing of the window is
        // kept from one start to the next.
        StartPosition = FormStartPosition.WindowsDefaultBounds;

        _webView.Ended += OnWebViewEnded;
    }

    public void RunMessageLoop()
    {
        Application.Run(this);
    }

    public void RequestClose()
    {
        // Read when the window is shown: a request that comes before the window has a handle is not lost.
        _closeRequested = true;
        PostToWindow(Close);
    }

    public void BringToForeground()
    {
        PostToWindow(() => _ = WindowActivationInterop.RestoreAndActivate(Handle));
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        CenterToScreen();
    }

    protected override void OnShown(EventArgs e)
    {
        if (_closeRequested)
        {
            Close();
            return;
        }

        base.OnShown(e);
        _webView.Start(this, BackColor);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        // Before the window is destroyed: the control closes the engine while its window still exists.
        _webView.Ended -= OnWebViewEnded;
        _webView.Dispose();
        base.OnFormClosed(e);
    }

    private static Icon LoadIcon()
    {
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream(IconResourceName)
            ?? throw new InvalidOperationException($"The assembly has no resource named {IconResourceName}.");
        return new Icon(stream);
    }

    private void OnWebViewEnded(object? sender, WebViewEndedEventArgs e)
    {
        _exit.Code = e.ExitCode;
        WebViewFailureDialog.Show(this, WebViewFailureMessage.For(e.Failure, e.Error));
        Close();
    }

    // Runs an action on the thread of the window, whatever thread asks for it. A window without a handle, not
    // shown yet or already closed, has no thread to run it on.
    private void PostToWindow(Action action)
    {
        if (!IsHandleCreated)
        {
            return;
        }

        try
        {
            BeginInvoke(action);
        }
        catch (InvalidOperationException)
        {
            // The window was closed in the meantime.
        }
    }
}
