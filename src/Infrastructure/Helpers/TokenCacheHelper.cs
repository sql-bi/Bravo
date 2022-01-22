using Microsoft.Identity.Client;
using Sqlbi.Bravo.Infrastructure.Security;
using System.IO;

namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    internal static class TokenCacheHelper
    {
        private static readonly object _tokenCacheFileLock = new();

        private static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (_tokenCacheFileLock)
            {
                byte[]? msalV3State = null;

                if (File.Exists(AppConstants.MsalTokenCacheFilePath))
                {
                    var encryptedData = File.ReadAllBytes(AppConstants.MsalTokenCacheFilePath);
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

                    File.WriteAllBytes(AppConstants.MsalTokenCacheFilePath, encryptedData);
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
