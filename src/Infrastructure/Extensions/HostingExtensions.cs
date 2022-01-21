using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Sqlbi.Bravo.Infrastructure.Authentication;
using Sqlbi.Bravo.Infrastructure.Configuration.Options;
using Sqlbi.Bravo.Infrastructure.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;

namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    internal static class HostingExtensions
    {
        public static IServiceCollection AddAndConfigureAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization((options) =>
            {
                // Use the default policy since there is no need to use a custom policy

                //options.AddPolicy(policyName, (builder) =>
                //{
                //    builder.RequireAssertion((context) =>
                //    {
                //        if (context.Resource is HttpContext httpContext)
                //        {
                //            if (httpContext.Request.Headers.TryGetValue(HeaderNames.Authorization, out var token))
                //            {
                //                return AppConstants.ApiAuthenticationToken.Equals(token);
                //            }

                //            //var controller = httpContext.GetEndpoint()?.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>();
                //            //if (controller is not null)
                //            //{
                //            //}
                //        }

                //        return false;
                //    });
                //});
            });

            return services;
        }

        public static IServiceCollection AddAndConfigureAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication(defaultScheme: AppConstants.ApiAuthenticationSchema)
                .AddScheme<AppAuthenticationSchemeOptions, AppAuthenticationHandler>(AppConstants.ApiAuthenticationSchema, (options) => options.Validate());

            return services;
        }

        public static IServiceCollection AddAndConfigureCors(this IServiceCollection services, string policyName, string origin)
        {
            services.AddCors((options) =>
            {
                options.AddPolicy(policyName, (policyBuilder) =>
                {
                    // for security, default to only accepting calls from the local machine
                    policyBuilder.AllowAnyMethod().AllowAnyHeader()
                        //.AllowAnyOrigin();

                        // TOFIX: CORS error
                        // Microsoft.AspNetCore.Hosting.Diagnostics: Information: Request starting HTTP/1.1 POST http://localhost:5000/api/GetModelFromDataset application/json 237
                        // Microsoft.AspNetCore.Cors.Infrastructure.CorsService: Information: CORS policy execution failed
                        // Microsoft.AspNetCore.Cors.Infrastructure.CorsService: Information: Request origin http://localhost:5000 does not have permission to access the resource.
                        .WithOrigins(origin);
                });
            });

            return services;
        }

        public static IServiceCollection AddAndConfigureProblemDetails(this IServiceCollection services)
        {
            services.AddProblemDetails((options) =>
            {
#if DEBUG
                options.IncludeExceptionDetails = (context, exception) => true;
#endif
                options.Map<BravoException>((context, exception) =>
                {
                    var problemDetailsFactory = context.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                    var problemDetails = problemDetailsFactory.CreateProblemDetails(
                        context,
                        statusCode: StatusCodes.Status400BadRequest,
                        detail: exception.ProblemDetail,
                        instance: exception.ProblemInstance);

                    return problemDetails;
                });

                options.Map<Exception>(predicate: (context, exception) => exception.IsOrHasInner<SocketException>(), mapping: (context, exception) =>
                {
                    var socketException = exception.Find<SocketException>() ?? throw new BravoUnexpectedException("SocketException not found");
                    var problemDetailsFactory = context.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                    var problemDetails = problemDetailsFactory.CreateProblemDetails(
                        context,
                        statusCode: StatusCodes.Status400BadRequest,
                        detail: $"[{ socketException.SocketErrorCode }] { exception.Message }",
                        instance: $"{ (int)BravoProblem.NetworkError }");

                    return problemDetails;
                });
            });

            return services;
        }

        public static IServiceCollection AddAndConfigureSwaggerGen(this IServiceCollection services)
        {
            services.AddSwaggerGen((options) =>
            {
                // TODO: IncludeXmlComments fails due to a wrong xml file path while debugging the MSIX package
                if (Debugger.IsAttached && DesktopBridgeHelper.IsPackagedAppInstance)
                    return;

                var xmlFile = $"{ Assembly.GetExecutingAssembly().GetName().Name }.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            });

            return services;
        }

        public static void AddWritableOptions<T>(this IServiceCollection services, IConfigurationSection section, string file) where T : class, new()
        {
            services.Configure<T>(section);
            services.AddTransient<IWritableOptions<T>>((provider) =>
            {
                var configuration = (IConfigurationRoot)provider.GetRequiredService<IConfiguration>();
                var environment = provider.GetRequiredService<IWebHostEnvironment>();
                var options = provider.GetRequiredService<IOptionsMonitor<T>>();

                return new WritableOptions<T>(environment, configuration, options, section.Key, file);
            });
        }

        /// <summary>
        /// Get the listening addresses used by the Kesterl HTTP server
        /// </summary>
        /// <remarks>The port binding happens only when IWebHost.Run() is called and it is not accessible on Startup.Configure() because port has not been yet assigned on this stage.</remarks>
        public static Uri[] GetListeningAddresses(this IHost host)
        {
            var server = host.Services.GetService<IServer>();
            if (server is not null)
            {
                var feature = server.Features.Get<IServerAddressesFeature>();
                if (feature is not null)
                {
                    var uris = feature.Addresses.Select((address) => new Uri(address, UriKind.Absolute)).ToArray();
                    return uris;
                }
            }

            return Array.Empty<Uri>();
        }
    }
}
