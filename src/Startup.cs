using Dax.Formatter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Services;
using Sqlbi.Infrastructure.Configuration.Settings;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo
{
    internal class Startup
    {
        private const string CorsLocalhostOnlyPolicy = "AllowLocalWebAPI";
        private const string CorsLocalhostOrigin = "null";

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddJsonOptions((jsonOptions) =>
            {
                jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
            services.AddCors((corsOptions) =>
            {
                corsOptions.AddPolicy(CorsLocalhostOnlyPolicy, (policyBuilder) =>
                {
                    // for security, default to only accepting calls from the local machine
                    policyBuilder.AllowAnyMethod().AllowAnyHeader()
                        //.AllowAnyOrigin();

                        // TOFIX: CORS error
                        // Microsoft.AspNetCore.Hosting.Diagnostics: Information: Request starting HTTP/1.1 POST http://localhost:5000/api/GetModelFromDataset application/json 237
                        // Microsoft.AspNetCore.Cors.Infrastructure.CorsService: Information: CORS policy execution failed
                        // Microsoft.AspNetCore.Cors.Infrastructure.CorsService: Information: Request origin http://localhost:5000 does not have permission to access the resource.
                        .WithOrigins(CorsLocalhostOrigin); 
                });
            });
#if DEBUG || DEBUG_WWWROOT
            services.AddSwaggerGenCustom();
#endif
            services.AddHttpClient();
            // Options
            services.AddWritableOptions<UserSettings>(section: Configuration.GetSection(nameof(UserSettings)), file: "appsettings.json"); //.ValidateDataAnnotations();
            services.AddOptions<StartupSettings>().Configure((settings) => settings.FromCommandLineArguments()).ValidateDataAnnotations();
            // Services
            services.AddSingleton<IPBICloudAuthenticationService, PBICloudAuthenticationService>();
            services.AddSingleton<IPBIDesktopService, PBIDesktopService>();
            services.AddSingleton<IPBICloudService, PBICloudService>();
            services.AddSingleton<IDaxFormatterClient, DaxFormatterClient>();
            // services.AddHostedService<ApplicationInstanceHostedService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment environment)
        {
            if (environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
#if DEBUG || DEBUG_WWWROOT
            app.UseSwagger();
            app.UseSwaggerUI();
#endif
            app.UseRouting();
            app.UseCors(CorsLocalhostOnlyPolicy); // The call to UseCors must be placed after UseRouting, but before UseAuthorization and UseEndpoints

            // TODO: do we need https authz/authn ? 
            //app.UseAuthentication();
            //app.UseAuthorization(); e.g. FilterAttribute, IActionFilter .OnActionExecuting => if (filterContext.HttpContext.Request.IsLocal == false) filterContext.Result = new HttpForbiddenResult(); 

            app.UseEndpoints((endpoints) =>
            {
                endpoints.MapControllers();
                //endpoints.MapGet("/", async context =>
                //{
                //    await context.Response.WriteAsync($"Sqlbi.Bravo API on {Environment.MachineName}");
                //});
            });
        }
    }
}
