namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    using Hellang.Middleware.ProblemDetails;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Options;
    using Microsoft.Identity.Client;
    using Sqlbi.Bravo.Infrastructure.Authentication;
    using Sqlbi.Bravo.Infrastructure.Configuration.Options;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Telemetry;
    using Sqlbi.Bravo.Models;
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
                jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

            mvcBuilder.ConfigureApiBehaviorOptions((apiOptions) =>
            {
                var defaultFactory = apiOptions.InvalidModelStateResponseFactory;

                apiOptions.InvalidModelStateResponseFactory = (context) =>
                {
                    var content = JsonSerializer.Serialize(new ValidationProblemDetails(context.ModelState));
                    AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(MvcCoreMvcBuilderExtensions.ConfigureApiBehaviorOptions) }.{ nameof(apiOptions.InvalidModelStateResponseFactory) }", content,  DiagnosticMessageSeverity.Error);

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
            // AspNetCore data protection errors (from telemetry)
            // --
            // System.Security.Cryptography.CryptographicException: An exception occurred while trying to decrypt the element.
            // System.Security.Cryptography.CryptographicException: Key {KeyId:B} is ineligible to be the default key because its {MethodName} method failed.
            // System.Security.Cryptography.CryptographicException: An exception occurred while processing the key element '{Element}'.
            // ---

            // We are using a custom authentication scheme that doesn't need data protection APIs.
            // Currently it seems to be not possible to add authentication without adding data protection services
            // This can be worked around by replicating the code from AddAuthentication() without the call to AddDataProtection()
            // See https://github.com/dotnet/aspnetcore/issues/43624
            
            // var builder = services.AddAuthentication(defaultScheme: AppEnvironment.ApiAuthenticationSchema);

            services.AddAuthenticationCore();
            //services.AddDataProtection();
            services.AddWebEncoders();
            services.TryAddSingleton<ISystemClock, SystemClock>();

            var builder = new AuthenticationBuilder(services);
            services.Configure<AuthenticationOptions>((options) => options.DefaultScheme = AppEnvironment.ApiAuthenticationSchema);
            builder.AddScheme<AppAuthenticationSchemeOptions, AppAuthenticationHandler>(AppEnvironment.ApiAuthenticationSchema, (options) => options.Validate());

            return services;
        }

        public static IServiceCollection AddAndConfigureCors(this IServiceCollection services)
        {
            services.AddCors((options) =>
            {
                options.AddDefaultPolicy((policy) =>
                {
                    policy.AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin();
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
                    if (AppEnvironment.IsDiagnosticLevelVerbose)
                        AppEnvironment.AddDiagnostics(name: nameof(BravoException), exception);

                    context.RequestServices.GetRequiredService<ITelemetryService>().TrackException(exception);

                    var problemDetailsFactory = context.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                    var problemDetails = problemDetailsFactory.CreateProblemDetails(context,
                        statusCode: StatusCodes.Status400BadRequest,
                        detail: exception.ProblemDetail,
                        instance: exception.ProblemInstance);

                    return problemDetails;
                });

                options.Map<MsalException>((context, exception) =>
                {
                    if (AppEnvironment.IsDiagnosticLevelVerbose)
                        AppEnvironment.AddDiagnostics(name: nameof(MsalException), exception);

                    context.RequestServices.GetRequiredService<ITelemetryService>().TrackException(exception);

                    var problemDetailsFactory = context.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                    var problemDetails = problemDetailsFactory.CreateProblemDetails(context,
                        statusCode: StatusCodes.Status400BadRequest,
                        detail: exception.ErrorCode,
                        instance: $"{ (int)BravoProblem.SignInMsalExceptionOccurred }");

                    return problemDetails;
                });

                options.Map<AMO.AsClientException>((context, exception) => exception.IsOrHasInner<MsalException>(), mapping: (context, exception) =>
                {
                    if (AppEnvironment.IsDiagnosticLevelVerbose)
                        AppEnvironment.AddDiagnostics(name: $"{nameof(AMO.AsClientException)}::{nameof(MsalException)}", exception);

                    context.RequestServices.GetRequiredService<ITelemetryService>().TrackException(exception);

                    var msalException = exception.Find<MsalException>(); BravoUnexpectedException.ThrowIfNull(msalException);
                    var problemDetailsFactory = context.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                    var problemDetails = problemDetailsFactory.CreateProblemDetails(context,
                        statusCode: StatusCodes.Status400BadRequest,
                        detail: msalException.ErrorCode,
                        instance: $"{ (int)BravoProblem.SignInMsalExceptionOccurred }");

                    return problemDetails;
                });

                options.Map<AMO.ConnectionException>((context, exception) =>
                {
                    if (AppEnvironment.IsDiagnosticLevelVerbose)
                        AppEnvironment.AddDiagnostics(name: nameof(AMO.ConnectionException), exception);

                    context.RequestServices.GetRequiredService<ITelemetryService>().TrackException(exception);

                    var problemDetailsFactory = context.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                    var problemDetails = problemDetailsFactory.CreateProblemDetails(context,
                        statusCode: StatusCodes.Status400BadRequest,
                        detail: exception.Message,
                        instance: $"{ (int)BravoProblem.AnalysisServicesConnectionFailed }");

                    return problemDetails;
                });

                options.Map<OperationCanceledException>((context, exception) =>
                {
                    if (AppEnvironment.IsDiagnosticLevelVerbose)
                        AppEnvironment.AddDiagnostics(name: nameof(OperationCanceledException), exception);

                    context.RequestServices.GetRequiredService<ITelemetryService>().TrackException(exception);

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
                    if (AppEnvironment.IsDiagnosticLevelVerbose)
                        AppEnvironment.AddDiagnostics(name: $"{nameof(Exception)}::{nameof(SocketException)}", exception);

                    context.RequestServices.GetRequiredService<ITelemetryService>().TrackException(exception);

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
                    AppEnvironment.AddDiagnostics(name: "UnhandledException", exception);

                    context.RequestServices.GetRequiredService<ITelemetryService>().TrackException(exception);

                    return StatusCodeProblemDetails.Create(StatusCodes.Status500InternalServerError);
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
                if ((Debugger.IsAttached && AppEnvironment.DeploymentMode == AppDeploymentMode.Packaged) == false)
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
    }
}
