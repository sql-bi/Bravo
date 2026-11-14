using System;
using System.Drawing;
using System.Windows.Forms;
using Bravo.Core.Settings;
using Bravo.Desktop.Interop;
using Bravo.Desktop.WebView;

namespace Bravo.Desktop.Shell;

internal sealed class MainWindow : Form, IMainWindow
{
    private const string IconResourceName = "Bravo.ico";

    private readonly WebViewHost _webView;
    private readonly ShellExit _exit;
    private readonly IUserSettingsStore _settings;
    private (Theme Theme, WindowColors Colors)? _appliedTheme;
    private volatile bool _closeRequested;

    public MainWindow(BuildIdentity identity, WebViewHost webView, ShellExit exit, IUserSettingsStore settings)
    {
        _webView = webView;
        _exit = exit;
        _settings = settings;

        Text = identity.WindowTitle;
        Icon = LoadIcon();

        // The size that Windows gives a new window, in the middle of the screen. Nothing of the window is
        // kept from one start to the next.
        StartPosition = FormStartPosition.WindowsDefaultBounds;

        // Before the window is shown: nothing is drawn in the colors of another theme first.
        ApplyTheme();

        _webView.Ended += OnWebViewEnded;
        _settings.Changed += OnSettingsChanged;
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

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        // The frame belongs to the handle: a new handle gets the frame of the theme again.
        _appliedTheme = null;
        ApplyTheme();
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
        _webView.Start(this);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        // Before the window is destroyed: the control closes the engine while its window still exists.
        _settings.Changed -= OnSettingsChanged;
        _webView.Ended -= OnWebViewEnded;
        _webView.Dispose();
        base.OnFormClosed(e);
    }

    // Windows tells every window that a setting of the system changed. When it is the color mode, a theme
    // that follows the system changes with it.
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (ColorModeInterop.IsSettingChange(m.Msg))
        {
            // Posted: the engine cannot be called while a message that another process sent is handled.
            PostToWindow(ApplyTheme);
        }
    }

    private static Icon LoadIcon()
    {
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream(IconResourceName)
            ?? throw new InvalidOperationException($"The assembly has no resource named {IconResourceName}.");
        return new Icon(stream);
    }

    // Raised on the thread that replaced the settings, which is one of the API.
    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        PostToWindow(ApplyTheme);
    }

    // The theme of the user on everything of the window: its background, its frame and its webview. It runs
    // on the thread of the window, and does nothing when what it would apply is already there.
    private void ApplyTheme()
    {
        var theme = _settings.Current.Theme;
        var isSystemDark = Application.SystemColorMode == SystemColorMode.Dark;
        var colors = WindowColors.For(theme, isSystemDark);
        if (_appliedTheme == (theme, colors))
        {
            return;
        }

        _appliedTheme = (theme, colors);
        BackColor = colors.Background;
        _webView.ApplyTheme(theme, colors.Background);
        if (IsHandleCreated)
        {
            ApplyFrame(colors);
        }
    }

    // Where Windows does not know the request, the frame stays as it is. A window that is not shown yet
    // is drawn with its frame when it is shown; one that is shown is drawn again, where Windows does not.
    private void ApplyFrame(WindowColors colors)
    {
        var isApplied = ColorModeInterop.TryUseDarkFrame(Handle, colors.IsDark);
        if (isApplied && Visible && !ColorModeInterop.RedrawsFrameItself)
        {
            ColorModeInterop.RedrawFrame(Handle, isActive: ActiveForm == this);
        }
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
