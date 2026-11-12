using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Bravo.Desktop.Tests.Hosting;

/// <summary>
/// The requests that the server has begun, in their order, each as its method and its path.
/// </summary>
internal sealed class RequestLog : IObserver<KeyValuePair<string, object?>>, IDisposable
{
    private const string RequestStarted = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start";

    private readonly ConcurrentQueue<string> _requests = new();
    private readonly IDisposable _subscription;

    public RequestLog(DiagnosticListener listener)
    {
        _subscription = listener.Subscribe(this);
    }

    public IReadOnlyCollection<string> Requests => _requests;

    public void OnNext(KeyValuePair<string, object?> value)
    {
        if (value.Key == RequestStarted && value.Value is HttpContext context)
        {
            _requests.Enqueue($"{context.Request.Method} {context.Request.Path}");
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
