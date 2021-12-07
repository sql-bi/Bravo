using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sqlbi.Bravo.Services;

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
            services.AddControllers();
            services.AddSwaggerGen();
            services.AddCors((corsOptions) =>
            {
                corsOptions.AddPolicy("AllowLocalWebAPI", (policyBuilder) =>
                {
                    policyBuilder.AllowAnyOrigin() //.WithOrigins("null") //for security, default to only accepting calls from the local machine
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });

            services.AddSingleton<IAnalyzeModelService, AnalyzeModelService>();
            services.AddSingleton<IPBIDesktopService, PBIDesktopService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment environment)
        {
            app.UseCors("AllowLocalWebAPI");

            if (environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
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
