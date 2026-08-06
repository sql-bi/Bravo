using System;
using System.Net;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Services;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Provides a builder for creating and configuring a <see cref="BravoApplication"/> instance.
/// </summary>
/// <remarks>
/// Everything is configured by the time the builder is returned, and <see cref="Build"/> is the only
/// method: the registrations are composed through <see cref="BravoServiceCollectionExtensions"/>, not
/// by mutating the builder from outside. Should a caller — a test, typically — ever need to override
/// a registration, expose <see cref="WebApplicationBuilder.Services"/> as a pass-through property
/// then; it is deliberately not exposed while nothing needs it.
/// </remarks>
internal sealed class BravoApplicationBuilder
{
    private readonly WebApplicationBuilder _innerBuilder;

    internal BravoApplicationBuilder(BravoApplicationInitializationContext context)
    {
        // CreateEmptyBuilder, not CreateBuilder: no configuration sources, no environment variables and
        // no implicit logging providers. Everything the host needs is declared explicitly below.
        _innerBuilder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
            ContentRootPath = AppContext.BaseDirectory,
        });

        ConfigureHosting(_innerBuilder);
        ConfigureLogging(_innerBuilder);
        ConfigureServices(_innerBuilder, context);
    }

    /// <summary>
    /// Builds the host, configures its request pipeline, and returns the <see cref="BravoApplication"/>.
    /// </summary>
    public BravoApplication Build()
    {
        var innerApplication = _innerBuilder.Build();

        ConfigurePipeline(innerApplication);

        return new BravoApplication(innerApplication);
    }

    private static void ConfigureHosting(WebApplicationBuilder builder)
    {
        builder.WebHost.UseKestrel((serverOptions) =>
        {
#if DEBUG
            const int port = 5000;
#else
            const int port = 0; // Use dynamic port assignment
#endif
            serverOptions.Listen(IPAddress.Loopback, port);
            serverOptions.AllowSynchronousIO = true; // required by ImportVpax
        });

#if DEBUG
        // Validating the whole graph costs startup time, so it is a development-only guard.
        builder.Host.UseDefaultServiceProvider((context, options) =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
        });
#endif
    }

    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        builder.Logging.AddEventSourceLogger();
        builder.Logging.AddEventLog();
        builder.Logging.AddFilter<EventLogLoggerProvider>((level) => level >= LogLevel.Warning);
#if DEBUG
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
#endif
    }

    private static void ConfigureServices(WebApplicationBuilder builder, BravoApplicationInitializationContext context)
    {
        builder.Services.AddOptions<StartupSettings>()
            .Configure((settings) => settings.FromCommandLineArguments()); //.ValidateDataAnnotations();
        builder.Services.AddSingleton<IServerAddressProvider, ServerAddressProvider>();

        builder.Services.AddBravoInitializationServices(context);
        builder.Services.AddBravoRestApi();
        builder.Services.AddBravoServices();
    }

    private static void ConfigurePipeline(WebApplication application)
    {
#if DEBUG
        application.UseSwagger();
        application.UseSwaggerUI();
#endif
        application.UseProblemDetails();
        // Keep this order: routing, CORS, authentication, authorization, then endpoint mapping.
        // See https://learn.microsoft.com/aspnet/core/fundamentals/middleware#middleware-order
        application.UseRouting();
        application.UseCors();
        application.UseAuthentication();
        application.UseAuthorization();
#if DEBUG
        // Keep local development endpoints reachable without a token.
        application.MapControllers();
#else
        application.MapControllers().RequireAuthorization();
#endif
    }
}
