namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using AutoUpdaterDotNET;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using Sqlbi.Bravo.Models;
    using System;
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

        public static void CheckForUpdate(UpdateChannelType updateChannel, bool synchronousCallback, bool throwOnError, Action<BravoUpdate> updateCallback)
        {
            if (AppEnvironment.IsPackagedAppInstance)
                throw new BravoUnexpectedException("CheckForUpdate of a packaged app instance");

            AutoUpdater.AppCastURL = updateChannel switch
            {
                // TODO: CheckForUpdate - add update channel URLs
                _ => string.Format("https://cdn.sqlbi.com/updates/BravoAutoUpdater.xml?nocache={0}", DateTimeOffset.Now.ToUnixTimeSeconds()),
            };
            AutoUpdater.HttpUserAgent = "AutoUpdater";
            AutoUpdater.Synchronous = synchronousCallback;
            //AutoUpdater.PersistenceProvider = new JsonFilePersistenceProvider(jsonPath: Path.Combine(AppEnvironment.ApplicationDataPath, "autoupdater.json"));
            AutoUpdater.CheckForUpdateEvent += OnUpdate;
            AutoUpdater.InstalledVersion = Version.Parse(AppEnvironment.ApplicationFileVersion);
            AutoUpdater.Start();

            void OnUpdate(UpdateInfoEventArgs updateInfo)
            {
                try
                {
                    if (updateInfo.Error is not null)
                    {
                        TelemetryHelper.TrackException(updateInfo.Error);

                        if (throwOnError)
                            throw updateInfo.Error;
                    }
                    else if (updateInfo.IsUpdateAvailable)
                    {
                        var bravoUpdate = BravoUpdate.CreateFrom(updateChannel, updateInfo);
                        updateCallback(bravoUpdate);
                    }
                }
                finally
                {
                    AutoUpdater.CheckForUpdateEvent -= OnUpdate;
                }
            }
        }
    }
}
