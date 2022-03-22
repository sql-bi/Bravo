namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using Sqlbi.Bravo.Models;
    using System;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    internal static class CommonHelper
    {
        public static User32.KeyState GetKeyState(Keys key)
        {
            var state = User32.KeyState.None;
            {
                var retval = User32.GetKeyState((int)key);

                if ((retval & 0x8000) == 0x8000)
                    state |= User32.KeyState.Down;

                if ((retval & 1) == 1)
                    state |= User32.KeyState.Toggled;
            }
            return state;
        }

        public static bool IsKeyDown(Keys key)
        {
            var state = GetKeyState(key);

            return state.HasFlag(User32.KeyState.Down);
        }

        public static void CheckForUpdate(UpdateChannelType updateChannel, Action<BravoUpdate> updateCallback, CancellationToken cancellationToken = default)
        {
            _ = Task.Factory.StartNew(async () =>
            {
                try
                {
                    var bravoUpdate = await CheckForUpdateAsync(updateChannel, cancellationToken);
                    if (bravoUpdate.IsNewerVersion)
                    {
                        updateCallback(bravoUpdate);
                    }
                }
                catch (Exception ex)
                {
                    AppEnvironment.AddDiagnostics($"{ nameof(CommonHelper) }.{ nameof(CheckForUpdate) }", ex);
                    TelemetryHelper.TrackException(ex);
                }
            });
        }

        public async static Task<BravoUpdate> CheckForUpdateAsync(UpdateChannelType updateChannel, CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient();

            var channelPath = updateChannel switch
            {
                UpdateChannelType.Stable => "bravo-public",
                UpdateChannelType.Dev => "bravo-internal", 
                _ => throw new BravoUnexpectedException($"Unexpected { nameof(UpdateChannelType) } '{ updateChannel }'")
            };

            var requestUri = $"http://artifacts.bravo.bi/{ channelPath }/currentversion.json?nocache={ DateTimeOffset.Now.ToUnixTimeSeconds() }";
            var json = await httpClient.GetStringAsync(requestUri, cancellationToken).ConfigureAwait(false);

            using var document = JsonDocument.Parse(json);

            var bravoUpdate = new BravoUpdate
            {
                UpdateChannel = updateChannel,
                InstalledVersion = AppEnvironment.ApplicationFileVersion,
                CurrentVersion = document.RootElement.GetProperty("version").GetString(),
                DownloadUrl = document.RootElement.GetProperty("download").GetString(),
                ChangelogUrl = document.RootElement.GetProperty("changelog").GetString(),
            };

            var installedVersion = Version.Parse(bravoUpdate.InstalledVersion);
            var currentVersion = Version.Parse(bravoUpdate.CurrentVersion!);

            bravoUpdate.IsNewerVersion = currentVersion > installedVersion;

            return bravoUpdate;
        }
    }
}
