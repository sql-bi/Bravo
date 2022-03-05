﻿namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    using Hellang.Middleware.ProblemDetails;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Hosting.Server;
    using Microsoft.AspNetCore.Hosting.Server.Features;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Microsoft.Identity.Client;
    using Sqlbi.Bravo.Infrastructure.Authentication;
    using Sqlbi.Bravo.Infrastructure.Configuration.Options;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Models;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using AMO = Microsoft.AnalysisServices;

    internal static class HostingExtensions
    {
        public static IMvcBuilder AddAndConfigureControllers(this IServiceCollection services)
        {
            var mvcBuilder = services.AddControllers();

            mvcBuilder.AddJsonOptions((jsonOptions) =>
            {
                // TODO: when all JsonStringEnumMemberConverter target enum types will be commented out then the NuGet Macross.Json.Extensions package can be removed
                jsonOptions.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumMemberConverter(
                        options: new JsonStringEnumMemberConverterOptions(deserializationFailureFallbackValue: null),
                        typeof(Sqlbi.Bravo.Infrastructure.Configuration.Settings.ThemeType),
                        typeof(Sqlbi.Bravo.Infrastructure.Configuration.Settings.DiagnosticLevelType),
                        typeof(Sqlbi.Bravo.Infrastructure.Configuration.Settings.UpdateChannelType),
                        typeof(Sqlbi.Bravo.Infrastructure.Messages.WebMessageType),
                        typeof(Sqlbi.Bravo.Models.PBIDesktopReportConnectionMode),
                        typeof(Sqlbi.Bravo.Models.PBICloudDatasetConnectionMode),
                        typeof(Sqlbi.Bravo.Models.PBICloudDatasetEndorsement)
                        //, typeof(Sqlbi.Bravo.Infrastructure.BravoProblem)
                        //, typeof(Sqlbi.Bravo.Models.DiagnosticMessageSeverity)
                        //, typeof(Sqlbi.Bravo.Models.DiagnosticMessageType)
                        //, typeof(Sqlbi.Bravo.Models.ExportData.ExportDataStatus)
                        //, typeof(Sqlbi.Bravo.Infrastructure.Contracts.PBICloud.CloudWorkspaceType)
                        //, typeof(Sqlbi.Bravo.Infrastructure.Contracts.PBICloud.CloudWorkspaceCapacitySkuType)
                        //, typeof(Sqlbi.Bravo.Infrastructure.Contracts.PBICloud.CloudSharedModelWorkspaceType)
                        //, typeof(Sqlbi.Bravo.Infrastructure.Contracts.PBICloud.CloudPromotionalStage)
                        //, typeof(Sqlbi.Bravo.Infrastructure.Contracts.PBICloud.CloudPermissions)
                        //, typeof(Sqlbi.Bravo.Infrastructure.Contracts.PBICloud.CloudOrganizationalGalleryItemStatus)
                        //, typeof(Sqlbi.Bravo.Infrastructure.Contracts.PBICloud.CloudEnvironmentType)
                        //, typeof(Dax.Formatter.Models.DaxFormatterLineStyle),
                        //, typeof(Dax.Formatter.Models.DaxFormatterSpacingStyle),
                        //, typeof(Sqlbi.Bravo.Models.FormatDax.DaxLineBreakStyle)
                        //, typeof(Dax.Template.Enums.AutoScanEnum)
                        //, typeof(Dax.Template.Enums.AutoNamingEnum)
                        //, typeof(Sqlbi.Bravo.Models.ManageDates.TypeStartFiscalYear)
                        //, typeof(Sqlbi.Bravo.Models.ManageDates.WeeklyType)
                        //, typeof(Sqlbi.Bravo.Models.ManageDates.TableValidation)
                        //, typeof(Sqlbi.Bravo.Infrastructure.TabularDatabaseFeature)
                        //, typeof(Sqlbi.Bravo.Models.ManageDates.DayOfWeek)
                        //, typeof(Sqlbi.Bravo.Models.ManageDates.QuarterWeekType)
                        //, typeof(Sqlbi.Bravo.Models.AnalyzeModel.TabularTableFeature)
                        //, typeof(Sqlbi.Bravo.Models.AnalyzeModel.TabularTableFeatureUnsupportedReason)
                        )
                    );
                jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

            mvcBuilder.ConfigureApiBehaviorOptions((apiOptions) =>
            {
                var defaultFactory = apiOptions.InvalidModelStateResponseFactory;

                apiOptions.InvalidModelStateResponseFactory = (context) =>
                {
                    AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"InvalidModelState", content: JsonSerializer.Serialize(context.ModelState), severity: DiagnosticMessageSeverity.Error);

                    // Invoke the defaultFactory delegate to preserve the default behavior
                    return defaultFactory(context);
                };
            });

            return mvcBuilder;
        }

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
            services.AddAuthentication(defaultScheme: AppEnvironment.ApiAuthenticationSchema)
                .AddScheme<AppAuthenticationSchemeOptions, AppAuthenticationHandler>(AppEnvironment.ApiAuthenticationSchema, (options) => options.Validate());

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
                // We include the details of the exception so that the UI can dispaly it and the user can view/copy/paste the stack trace of the exception
                options.IncludeExceptionDetails = (context, exception) => true;

                options.Map<BravoException>((context, exception) =>
                {
                    var problemDetailsFactory = context.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                    var problemDetails = problemDetailsFactory.CreateProblemDetails(context,
                        statusCode: StatusCodes.Status400BadRequest,
                        detail: exception.ProblemDetail,
                        instance: exception.ProblemInstance);

                    return problemDetails;
                });

                options.Map<MsalException>((context, exception) =>
                {
                    var problemDetailsFactory = context.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                    var problemDetails = problemDetailsFactory.CreateProblemDetails(context,
                        statusCode: StatusCodes.Status400BadRequest,
                        detail: exception.ErrorCode,
                        instance: $"{ (int)BravoProblem.SignInMsalExceptionOccurred }");

                    return problemDetails;
                });

                options.Map<AMO.ConnectionException>((context, exception) =>
                {
                    var problemDetailsFactory = context.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                    var problemDetails = problemDetailsFactory.CreateProblemDetails(context,
                        statusCode: StatusCodes.Status400BadRequest,
                        detail: exception.Message,
                        instance: $"{ (int)BravoProblem.AnalysisServicesConnectionFailed }");

                    return problemDetails;
                });

                options.Map<OperationCanceledException>((context, exception) =>
                {
                    var problemDetailsFactory = context.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                    var problemDetails = problemDetailsFactory.CreateProblemDetails(context,
                        statusCode: StatusCodes.Status400BadRequest,
                        detail: exception.Message,
                        instance: $"{ (int)BravoProblem.OperationCancelled }");

                    return problemDetails;
                });

                // Do not change the Map<> order, this is because a SocketException can exists as an inner exception also for other types of exceptions (i.e. AMO.ConnectionException)
                // This can result in a misleading message like a AMO.ConnectionException reported as a SocketException (NetworkError instead of AnalysisServicesConnectionFailed)
                options.Map<Exception>(predicate: (context, exception) => exception.IsOrHasInner<SocketException>(), mapping: (context, exception) =>
                {
                    var socketException = exception.Find<SocketException>(); BravoUnexpectedException.ThrowIfNull(socketException);
                    var problemDetailsFactory = context.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                    var problemDetails = problemDetailsFactory.CreateProblemDetails(context,
                        statusCode: StatusCodes.Status400BadRequest,
                        detail: $"[{ socketException.SocketErrorCode }] { exception.Message }",
                        instance: $"{ (int)BravoProblem.NetworkError }");

                    return problemDetails;
                });

                // Because exceptions are handled polymorphically, this will act as a "catch all" mapping, which is why it's added last
                options.Map<Exception>(mapping: (context, exception) =>
                {
                    AppEnvironment.AddDiagnostics(DiagnosticMessageType.Text, name: "UnhandledException", exception.ToString(), severity: DiagnosticMessageSeverity.Error);

                    var problemDetails = StatusCodeProblemDetails.Create(StatusCodes.Status500InternalServerError);
                    return problemDetails;
                });
            });

            return services;
        }

        public static IServiceCollection AddAndConfigureSwaggerGen(this IServiceCollection services)
        {
            services.AddSwaggerGen((options) =>
            {
                options.CustomSchemaIds((type) => type.ToString());

                // Include xml comments only if we are not debugging the MSIX packaged application, this is because a wrong path is generated for xml files
                if ((Debugger.IsAttached && AppEnvironment.IsPackagedAppInstance) == false)
                {
                    var xmlFile = $"{ Assembly.GetExecutingAssembly().GetName().Name }.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                }
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
