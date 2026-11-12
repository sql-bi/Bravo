using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Bravo.Desktop.Tests.Hosting;

/// <summary>
/// Tells a test that the server has begun a request to a path. From then on the request is the server's to
/// answer, whatever the test does next: the test needs no pause to be sure of it.
/// </summary>
internal sealed class RequestStartedSignal : IObserver<KeyValuePair<string, object?>>, IDisposable
{
    private const string RequestStarted = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start";

    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly IDisposable _subscription;
    private readonly string _path;

    public RequestStartedSignal(DiagnosticListener listener, string path)
    {
        _path = path;
        _subscription = listener.Subscribe(this);
    }

    public Task Started => _started.Task;

    public void OnNext(KeyValuePair<string, object?> value)
    {
        if (value.Key == RequestStarted && value.Value is HttpContext context && context.Request.Path == _path)
        {
            _started.TrySetResult();
        }
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void Dispose()
    {
        _subscription.Dispose();
    }
}
