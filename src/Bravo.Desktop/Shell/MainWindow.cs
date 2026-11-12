using System;
using System.Windows.Forms;
using Bravo.Desktop.Interop;

namespace Bravo.Desktop.Shell;

internal sealed class MainWindow : Form, IMainWindow
{
    private volatile bool _closeRequested;

    public MainWindow()
    {
        Text = Product.Name;
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

    protected override void OnShown(EventArgs e)
    {
        if (_closeRequested)
        {
            Close();
            return;
        }

        base.OnShown(e);
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
