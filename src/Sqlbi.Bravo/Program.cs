using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Sqlbi.Bravo.Core;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Security;
using Sqlbi.Bravo.Core.Services;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.Core.Settings;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using Sqlbi.Bravo.UI.Services;
using Sqlbi.Bravo.UI.Services.Interfaces;
using Sqlbi.Bravo.UI.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Windows.Input;

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
            builder.AddJsonFile(AppConstants.UserSettingsFilePath, optional: true);

            var config = builder.Build();
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
                services.AddSingleton<IApplicationInstanceService, ApplicationInstanceService>();
                services.AddSingleton<IGlobalSettingsProviderService, GlobalSettingsProviderService>();
                services.AddSingleton<IPowerBIDesktopService, PowerBIDesktopService>();
                services.AddSingleton<IPowerBICloudService, PowerBICloudService>();
                services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<ShellViewModel>();

                // Make these services transient as each ViewModel will need a unique one with a separate connection
                services.AddTransient<IAnalysisServicesEventWatcherService, AnalysisServicesEventWatcherService>();
                services.AddTransient<IDaxFormatterService, DaxFormatterService>();
                services.AddTransient<IAnalyzeModelService, AnalyzeModelService>();
                // Make these ViewModels transient as need a new one for each instance (within each tab)
                services.AddTransient<DaxFormatterViewModel>();
                services.AddTransient<AnalyzeModelViewModel>();
                // Make TabItemViewModel as need a new one for each tab
                services.AddTransient<TabItemViewModel>();
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

            var loggingLevelSwitch = new LoggingLevelSwitch(initialMinimumLevel: LogEventLevel.Information);
            var loggerConfiguration = new LoggerConfiguration();
            loggerConfiguration.MinimumLevel.ControlledBy(loggingLevelSwitch);

            ConfigureLogging();
            ConfigureTelemetry();

            var logger = loggerConfiguration.CreateLogger();
            return logger;

            void ConfigureLogging()
            {
                var enabled = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) || Debugger.IsAttached;
                if (enabled == false)
                    return;

                var outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level}] {SourceContext}.{CallerMemberName}({EventId}|{ManagedThread}) - {Message:lj}{NewLine}{Exception}";

                loggingLevelSwitch.MinimumLevel = LogEventLevel.Verbose;
                loggerConfiguration.Enrich.WithManagedThread().Enrich.FromLogContext();
                loggerConfiguration.WriteTo.File(path: AppConstants.LogFilePath, restrictedToMinimumLevel: LogEventLevel.Verbose, outputTemplate, rollingInterval: RollingInterval.Day);

                if (Debugger.IsAttached)
                {
                    loggerConfiguration.WriteTo.Debug(restrictedToMinimumLevel: LogEventLevel.Verbose, outputTemplate);
                }
            }

            void ConfigureTelemetry()
            {
                var telemetryEnabled = config.GetValue($"{ nameof(AppSettings) }:{ nameof(AppSettings.TelemetryEnabled) }", defaultValue: AppConstants.ApplicationSettingsDefaultTelemetryEnabled);
                if (telemetryEnabled == false)
                    return;

                var telemetryChannel = new ServerTelemetryChannel();
                telemetryChannel.DeveloperMode = Debugger.IsAttached;
#if DEBUG
                telemetryChannel.DeveloperMode = true;
#endif

                var telemetryConfiguration = new TelemetryConfiguration();
                telemetryConfiguration.InstrumentationKey = AppConstants.TelemetryInstrumentationKey;
                telemetryConfiguration.DisableTelemetry = !telemetryEnabled;
                telemetryConfiguration.TelemetryChannel = telemetryChannel;

                var telemetryClient = new TelemetryClient(telemetryConfiguration);
                telemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
                telemetryClient.Context.Component.Version = AppConstants.ApplicationProductVersion;
                telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
                telemetryClient.Context.User.Id = $"{ Environment.MachineName }\\{ Environment.UserName }".ToHashSHA256();

                loggerConfiguration
                    .WriteTo.ApplicationInsights(telemetryClient, TelemetryConverter.Events, restrictedToMinimumLevel: LogEventLevel.Information)
                    //.Enrich.WithProperty("ApplicationName", AppConstants.ApplicationName)
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning);
            }
        }
    }
}