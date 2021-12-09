using Dax.Formatter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo
{
    internal class Startup
    {
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
            });
            services.AddCors((corsOptions) =>
            {
                corsOptions.AddPolicy("AllowLocalWebAPI", (policyBuilder) =>
                {
                    policyBuilder.AllowAnyOrigin() //.WithOrigins("null") //for security, default to only accepting calls from the local machine
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });
#if DEBUG
            services.AddSwaggerGenCustomized();
#endif
            services.AddSingleton<IAnalyzeModelService, AnalyzeModelService>();
            services.AddSingleton<IPBIDesktopService, PBIDesktopService>();
            services.AddSingleton<IDaxFormatterClient, DaxFormatterClient>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment environment)
        {
            app.UseCors("AllowLocalWebAPI");

            if (environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
#if DEBUG
                app.UseSwagger();
                app.UseSwaggerUI();
#endif
            }

            app.UseRouting();
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
