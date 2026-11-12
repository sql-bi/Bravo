using System;
using System.Drawing;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xunit;

namespace Bravo.Desktop.Tests.WebView;

/// <summary>
/// A window on a thread of its own with a message loop, as the main window of the application has: a webview
/// is created and used on the thread of its window, and a test does not stay on one thread.
/// </summary>
internal sealed class WebViewTestWindow : IAsyncDisposable
{
    private readonly TaskCompletionSource<Form> _shown = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Thread _thread;
    private ExceptionDispatchInfo? _loopFailure;

    public WebViewTestWindow()
    {
        _thread = new Thread(RunMessageLoop) { IsBackground = true, Name = nameof(WebViewTestWindow) };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    /// <summary>
    /// Runs an action on the thread of the window, with the window, and returns its result. What the action
    /// awaits resumes on that thread.
    /// </summary>
    public async Task<T> InvokeAsync<T>(Func<Form, Task<T>> action)
    {
        var form = await _shown.Task.WaitAsync(TestContext.Current.CancellationToken);
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        form.BeginInvoke(() => CompleteAsync(action, form, completion));
        return await completion.Task.WaitAsync(TestContext.Current.CancellationToken);
    }

    public async Task InvokeAsync(Action<Form> action)
    {
        _ = await InvokeAsync(form =>
        {
            action(form);
            return Task.FromResult(true);
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_shown.Task.IsCompletedSuccessfully)
        {
            var form = await _shown.Task;
            try
            {
                form.BeginInvoke(form.Close);
            }
            catch (InvalidOperationException)
            {
                // The message loop has already ended.
            }
        }

        _thread.Join();
        _loopFailure?.Throw();
    }

    private static async void CompleteAsync<T>(Func<Form, Task<T>> action, Form form, TaskCompletionSource<T> completion)
    {
        try
        {
            completion.TrySetResult(await action(form));
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
    }

    private void RunMessageLoop()
    {
        try
        {
            // As the application does: an exception of a handler leaves the message loop, here to fail the test.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException, threadScope: true);

            // Outside every screen: the window is real, and nobody sees it.
            using var form = new Form
            {
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-20000, -20000),
                Size = new Size(800, 600),
            };
            form.Shown += (_, _) => _shown.TrySetResult(form);
            Application.Run(form);
        }
        catch (Exception exception)
        {
            _loopFailure = ExceptionDispatchInfo.Capture(exception);
            _shown.TrySetException(exception);
        }
    }
}
