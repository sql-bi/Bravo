namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using Sqlbi.Bravo.Models;
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using System.Xml.Linq;

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
                    updateCallback(bravoUpdate);
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

            var requestUri = string.Format("https://cdn.sqlbi.com/updates/BravoAutoUpdater.xml?nocache={0}", DateTimeOffset.Now.ToUnixTimeSeconds());
            var text = await httpClient.GetStringAsync(requestUri, cancellationToken).ConfigureAwait(false);
            var document = XDocument.Parse(text);
            var bravoUpdate = BravoUpdate.CreateFrom(updateChannel, document);

            return bravoUpdate;
        }
    }
}
