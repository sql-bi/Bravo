using Microsoft.Identity.Client;
using Sqlbi.Bravo.Infrastructure.Security;
using System.IO;

namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    internal static class TokenCacheHelper
    {
        private static readonly object _tokenCacheFileLock = new();
        private static readonly string _tokenCacheFilePath;

        static TokenCacheHelper()
        {
            _tokenCacheFilePath = AppConstants.DefaultTokenCacheFilePath;

            //try
            //{
            //    // For packaged desktop apps (MSIX packages, also called desktop bridge) the executing assembly folder is read-only. 
            //    // In that case we need to use Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path + "\msalcache.bin" which is a per-app read/write folder for packaged apps.
            //    // See https://docs.microsoft.com/windows/msix/desktop/desktop-to-uwp-behind-the-scenes
            //    CacheFilePath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path, ".msalcache.bin");
            //}
            //catch (InvalidOperationException)
            //{
            //    // Fall back for an unpackaged desktop app
            //    CacheFilePath = AppConstants.TokenCacheFilePath;
            //}
        }

        private static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (_tokenCacheFileLock)
            {
                byte[]? msalV3State = null;

                if (File.Exists(_tokenCacheFilePath))
                {
                    var encryptedData = File.ReadAllBytes(_tokenCacheFilePath);
                    msalV3State = Cryptography.Unprotect(encryptedData);
                }

                args.TokenCache.DeserializeMsalV3(msalV3State);
            }
        }

        private static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged) // if the access operation resulted in a cache update
            {
                lock (_tokenCacheFileLock)
                {
                    var msalV3State = args.TokenCache.SerializeMsalV3();
                    var encryptedData = Cryptography.Protect(msalV3State);

                    File.WriteAllBytes(_tokenCacheFilePath, encryptedData);
                }
            }
        }

        public static void EnableSerialization(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }
    }
}
