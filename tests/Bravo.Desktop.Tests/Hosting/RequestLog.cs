using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Bravo.Desktop.Tests.Hosting;

/// <summary>
/// The requests that the server has begun, in their order, each as its method and its path, and the ones that it
/// has answered, each also with the status of its response.
/// </summary>
internal sealed class RequestLog : IObserver<KeyValuePair<string, object?>>, IDisposable
{
    private const string RequestStarted = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start";
    private const string RequestStopped = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";

    private readonly ConcurrentQueue<string> _requests = new();
    private readonly ConcurrentQueue<string> _answers = new();
    private readonly IDisposable _subscription;

    public RequestLog(DiagnosticListener listener)
    {
        _subscription = listener.Subscribe(this);
    }

    public IReadOnlyCollection<string> Requests => _requests;

    public IReadOnlyCollection<string> Answers => _answers;

    public void OnNext(KeyValuePair<string, object?> value)
    {
        if (value.Value is not HttpContext context)
        {
            return;
        }

        if (value.Key == RequestStarted)
        {
            _requests.Enqueue($"{context.Request.Method} {context.Request.Path}");
        }
        else if (value.Key == RequestStopped)
        {
            _answers.Enqueue($"{context.Request.Method} {context.Request.Path} {context.Response.StatusCode}");
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
