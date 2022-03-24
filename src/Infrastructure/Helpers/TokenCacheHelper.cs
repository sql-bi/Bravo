namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Microsoft.Identity.Client;
    using Sqlbi.Bravo.Infrastructure.Security;
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using System.IO;
    using System.Security.Cryptography;

    internal static class TokenCacheHelper
    {
        private static readonly object _tokenCacheFileLock = new();

        private static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (_tokenCacheFileLock)
            {
                byte[]? msalV3State = null;

                if (File.Exists(AppEnvironment.MsalTokenCacheFilePath))
                {
                    var encryptedData = File.ReadAllBytes(AppEnvironment.MsalTokenCacheFilePath);
                    try
                    {
                        msalV3State = Cryptography.Unprotect(encryptedData);
                    }
                    catch (CryptographicException ex) when (ex.HResult == HRESULT.E_INVALID_DATA)
                    {
                        // The token file is corrupted, we delete the file in order to force a new authentication
                        File.Delete(AppEnvironment.MsalTokenCacheFilePath);
                    }
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

                    File.WriteAllBytes(AppEnvironment.MsalTokenCacheFilePath, encryptedData);
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
