using Microsoft.Extensions.Logging;
using Serilog.Events;
using Sqlbi.Bravo.Core.Helpers;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using Sqlbi.Bravo.UI.Framework.Commands;
using Sqlbi.Bravo.UI.Framework.Interfaces;
using Sqlbi.Bravo.UI.Framework.ViewModels;
using Sqlbi.Bravo.UI.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class SettingsViewModel : BaseViewModel
    {
        private readonly IGlobalSettingsProviderService _settings;
        private readonly ILogger<SettingsViewModel> _logger;
        private readonly IThemeSelectorService _themeSelector;

        public SettingsViewModel(IGlobalSettingsProviderService settings, ILogger<SettingsViewModel> logger, IThemeSelectorService themeSelector)
        {
            _settings = settings;
            _logger = logger;
            _themeSelector = themeSelector;

            _logger.Trace();
            SaveCommand = new RelayCommand<object>(execute: async (parameter) => await SaveAsync(parameter));

            SetThemeCommand = new RelayCommand<string>(themeName =>
            {
                Mouse.OverrideCursor = Cursors.Wait;
                try
                {
                    _themeSelector.SetTheme(themeName);
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            });

    var assemblyLocation = Assembly.GetExecutingAssembly().Location;
    var version = FileVersionInfo.GetVersionInfo(assemblyLocation).FileVersion;
    VersionDescription = $"Bravo - {version}";
        }

public string VersionDescription { get; private set; }

public string Theme
{
    get => _settings.Application.ThemeName;
    set
    {
        System.Diagnostics.Debug.WriteLine($"Setting Theme to '{value}'");
        _settings.Application.ThemeName = value;
        OnPropertyChanged(nameof(Theme));
    }
}

public IEnumerable<LogEventLevel> TelemetryLevels => Enum.GetValues(typeof(LogEventLevel)).Cast<LogEventLevel>();

public bool TelemetryEnabled
{
    get => _settings.Application.TelemetryEnabled;
    set => _settings.Application.TelemetryEnabled = value;
}

public LogEventLevel TelemetryLevel
{
    get => _settings.Application.TelemetryLevel;
    set => _settings.Application.TelemetryLevel = value;
}

public bool ProxyUseSystem
{
    get => _settings.Application.ProxyUseSystem;
    set => _settings.Application.ProxyUseSystem = value;
}

public string ProxyAddress
{
    get => _settings.Application.ProxyAddress;
    set => _settings.Application.ProxyAddress = value;
}

public string ProxyUser
{
    get => _settings.Application.ProxyUser;
    set => _settings.Application.ProxyUser = value;
}

public bool ProxyIsEnabled => !ProxyUseSystem;

public bool UIShellBringToForegroundOnParentProcessMainWindowScreen
{
    get => _settings.Application.UIShellBringToForegroundOnParentProcessMainWindowScreen;
    set => _settings.Application.UIShellBringToForegroundOnParentProcessMainWindowScreen = value;
}

public ICommand SaveCommand { get; set; }

public bool SaveCommandIsRunning { get; set; }

public ICommand SetThemeCommand { get; set; }

private async Task SaveAsync(object parameter)
{
    _logger.Trace();

    _settings.Application.ProxyPassword = ProxyUseSystem ? default : (parameter as ISecurePassword).SecurePassword.ToProtectedString();

    await ExecuteCommandAsync(() => SaveCommandIsRunning, _settings.SaveAsync);
}
    }
}