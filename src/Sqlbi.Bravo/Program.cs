using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Sqlbi.Bravo.Core;
using Sqlbi.Bravo.Core.Client.Http;
using Sqlbi.Bravo.Core.Client.Http.Interfaces;
using Sqlbi.Bravo.Core.Services;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.Core.Settings;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using Sqlbi.Bravo.UI.Services;
using Sqlbi.Bravo.UI.Services.Interfaces;
using Sqlbi.Bravo.UI.ViewModels;
using System;
using System.IO;
using System.Runtime;

namespace Sqlbi.Bravo
{
    public class Program
    {
        private const int E_FATAL = -1;

        [STAThread]
        public static int Main()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            //TryOptimizeJIT();

            var config = CreateConfiguration();
            Log.Logger = CreateLogger(config);

            try
            {
                using var host = CreateHost(config);
                var app = new App(host);
                app.InitializeComponent();
                return app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Unhandled exception");
                return E_FATAL;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }         

        private static void TryOptimizeJIT()
        {
            try
            {
                var path = AppConstants.ProfileOptimizationProfileRootPath;
                Directory.CreateDirectory(path);
                ProfileOptimization.SetProfileRoot(path);
                ProfileOptimization.StartProfile(AppConstants.ProfileOptimizationProfileName);
            }
            catch
            {
                // ignore
            }
        }

        private static IConfigurationRoot CreateConfiguration()
        {
            var builder = new ConfigurationBuilder();

            builder.Sources.Clear();
            builder.SetBasePath(Environment.CurrentDirectory); 
            builder.AddJsonFile("hostsettings.json", optional: true);
            builder.AddJsonFile("appsettings.json", optional: true);
            builder.AddJsonFile(AppConstants.UserSettingsFilePath, optional: true);

            var config = builder.Build();
            
            var environment = config.GetValue<string>(nameof(Environment));
            if (environment != null)
            {
                builder.AddJsonFile($"appsettings.{ environment }.json", optional: true);
                config = builder.Build();
            }

            return config;
        }

        private static IHost CreateHost(IConfigurationRoot config)
        {
            var host = new HostBuilder()
                .ConfigureHostConfiguration((builder) => ConfigureConfiguration(builder, config))
                .ConfigureAppConfiguration((builder) => ConfigureConfiguration(builder, config))
                .ConfigureServices((context, services) => ConfigureServices(context, services))
                .ConfigureLogging((builder) => ConfigureLogging(builder))
                .Build();

            return host;

            static void ConfigureConfiguration(IConfigurationBuilder builder, IConfigurationRoot config) => builder.AddConfiguration(config);

            static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
            {
                services.AddOptions();
                services.AddOptions<RuntimeSettings>();
                services.Configure<AppSettings>(context.Configuration.GetSection(nameof(AppSettings)));
                services.AddHttpClient<IDaxFormatterHttpClient, DaxFormatterHttpClient>().ConfigurePrimaryHttpMessageHandler((provider) => new DaxFormatterHttpClientMessageHandler(provider));
                services.AddSingleton<IAnalysisServicesEventWatcherService, AnalysisServicesEventWatcherService>();
                services.AddSingleton<IApplicationInstanceService, ApplicationInstanceService>();
                services.AddSingleton<IGlobalSettingsProviderService, GlobalSettingsProviderService>();
                services.AddSingleton<IDaxFormatterService, DaxFormatterService>();
                services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
                services.AddSingleton<ShellViewModel>();
                services.AddSingleton<SideMenuViewModel>();
                services.AddSingleton<SettingsViewModel>();
                // TODO MATT: stop making this a singleton so can use in different tabs
                services.AddSingleton<DaxFormatterViewModel>();

                services.AddScoped<TabItemViewModel>();
            }

            static void ConfigureLogging(ILoggingBuilder logging)
            {
                logging.ClearProviders();
                logging.AddSerilog();
            }
        }

        private static Serilog.ILogger CreateLogger(IConfigurationRoot config)
        {
            // Sqlbi.Bravo.exe 1> c:\temp\self.log 2>&1
            Serilog.Debugging.SelfLog.Enable(Console.Error);

            var loggerConfiguration = new LoggerConfiguration()
                .ReadFrom.Configuration(config);

            ConfigureTelemetry();

            var logger = loggerConfiguration.CreateLogger();
            return logger;

            void ConfigureTelemetry()
            {
                var telemetryEnabled = config.GetValue($"{ nameof(AppSettings) }:{ nameof(AppSettings.TelemetryEnabled) }", defaultValue: AppConstants.ApplicationSettingsDefaultTelemetryEnabled);
                if (telemetryEnabled == false)
                    return;

#pragma warning disable CS0618 // Type or member is obsolete
                // https://github.com/microsoft/ApplicationInsights-dotnet/issues/1152
                var telemetryConfiguration = TelemetryConfiguration.Active;
                telemetryConfiguration.InstrumentationKey = AppConstants.TelemetryInstrumentationKey;
                telemetryConfiguration.DisableTelemetry = !telemetryEnabled;
#pragma warning restore CS0618 // Type or member is obsolete

                var telemetryLevel = config.GetValue($"{ nameof(AppSettings) }:{ nameof(AppSettings.TelemetryLevel) }", defaultValue: AppConstants.ApplicationSettingsDefaultTelemetryLevel);

                loggerConfiguration
                    .WriteTo.ApplicationInsights(telemetryConfiguration, TelemetryConverter.Events, telemetryLevel)
                    .Enrich.WithProperty("ApplicationName", AppConstants.ApplicationName)
                    .Enrich.WithProperty("Version", AppConstants.ApplicationProductVersion);
            }
        }
    }
}