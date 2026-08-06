using System;
using System.Windows.Forms;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sqlbi.Bravo.Infrastructure;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Represents the Bravo application.
/// </summary>
internal sealed class BravoApplication : IDisposable
{
    private readonly WebApplication _innerApplication;

    public static BravoApplicationBuilder CreateBuilder(BravoApplicationInitializationContext context)
    {
        return new BravoApplicationBuilder(context);
    }

    internal BravoApplication(WebApplication innerApplication)
    {
        _innerApplication = innerApplication;
    }

    /// <summary>
    /// Runs the application and starts the message loop.
    /// </summary>
    public void Run()
    {
        _innerApplication.Start();
        try
        {
            // Create the main window through DI without registering it.
            // Its lifetime is owned by the WinForms message loop rather than the service container.
            using var window = ActivatorUtilities.CreateInstance<AppWindow>(_innerApplication.Services);
/*
            // If the host stops independently of the UI, the message loop must also end.
            // Otherwise the window could outlive the backend it depends on.
            // ApplicationStopping is raised on a background thread, so closing the window
            // is marshaled to the UI thread.
            // The registration is disposed before StopAsync() is invoked below, preventing
            // the normal window-close shutdown path from re-entering.
            var lifetime = _innerApplication.Services.GetRequiredService<IHostApplicationLifetime>();
            using var stopRegistration = lifetime.ApplicationStopping.Register(() =>
            {
                if (window.IsHandleCreated)
                    window.BeginInvoke(window.Close);
            });
*/
            Application.Run(window);
        }
        finally
        {
            _innerApplication.StopAsync().GetAwaiter().GetResult();
        }
    }

    public void Dispose()
        => _innerApplication.DisposeAsync().AsTask().GetAwaiter().GetResult();
}
