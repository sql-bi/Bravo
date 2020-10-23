using Microsoft.Extensions.Options;
using Sqlbi.Bravo.Core.Settings;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services
{
    internal class GlobalSettingsProviderService : IGlobalSettingsProviderService
    {
        public GlobalSettingsProviderService(IOptions<AppSettings> applicationOption, IOptions<RuntimeSettings> runtimeOption)
        {
            Application = applicationOption.Value;
            Runtime = runtimeOption.Value;
        }

        public IAppSettings Application { get; private set; }

        public IRuntimeSettings Runtime { get; private set; }

        public async Task SaveAsync()
        {
            var file = new FileInfo(AppConstants.UserSettingsFilePath);
            Directory.CreateDirectory(file.DirectoryName);

            var value = new
            {
                AppSettings = Application
            };
            var options = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                WriteIndented = true,
            };

            var json = JsonSerializer.Serialize(value, options);
            File.WriteAllText(file.FullName, json);

            await Task.CompletedTask;
        }
    }
}