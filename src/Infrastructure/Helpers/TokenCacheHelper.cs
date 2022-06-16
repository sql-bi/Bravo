namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Microsoft.Identity.Client;
    using Sqlbi.Bravo.Infrastructure.Security;
    using Sqlbi.Bravo.Models;
    using System;
    using System.IO;
    using System.Security.Cryptography;

    internal static class TokenCacheHelper
    {
        private static readonly object _tokenCacheLock = new();

        private static void BeforeAccessCallback(TokenCacheNotificationArgs args)
        {
            lock (_tokenCacheLock)
            {
                byte[]? cachedBytes = null;

                if (File.Exists(AppEnvironment.MsalTokenCacheFilePath))
                {
                    var encryptedBytes = File.ReadAllBytes(AppEnvironment.MsalTokenCacheFilePath);
                    if (encryptedBytes.Length > 0)
                    {
                        try
                        {
                            cachedBytes = Cryptography.Unprotect(encryptedBytes);
                        }
                        catch (CryptographicException ex)
                        {
                            AppEnvironment.AddDiagnostics(name: $"{nameof(TokenCacheHelper)}.{nameof(BeforeAccessCallback)}", ex, DiagnosticMessageSeverity.Warning);

                            // Delete the file in order to force a new authentication
                            File.Delete(AppEnvironment.MsalTokenCacheFilePath);
                        }
                    }
                }

                args.TokenCache.DeserializeMsalV3(cachedBytes);
            }
        }

        private static void AfterAccessCallback(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged) // if the access operation resulted in a cache update
            {
                lock (_tokenCacheLock)
                {
                    var cachedBytes = args.TokenCache.SerializeMsalV3();
                    var encryptedBytes = Cryptography.Protect(cachedBytes);

                    File.WriteAllBytes(AppEnvironment.MsalTokenCacheFilePath, encryptedBytes);
                }
            }
        }

        /// <summary>
        /// Registers a token cache to synchronize with the persistent storage.
        /// </summary>
        /// <param name="tokenCache">The application token cache, typically referenced as <see cref="IClientApplicationBase.UserTokenCache"/></param>
        /// <remarks>Call <see cref="UnregisterCache(ITokenCache)"/> to have the given token cache stop syncronizing.</remarks>
        public static void RegisterCache(ITokenCache tokenCache)
        {
            ArgumentNullException.ThrowIfNull(tokenCache);

            tokenCache.SetBeforeAccess(BeforeAccessCallback);
            tokenCache.SetAfterAccess(AfterAccessCallback);
        }

        /// <summary>
        /// Unregisters a token cache so it no longer synchronizes with on disk storage.
        /// </summary>
        public static void UnregisterCache(ITokenCache tokenCache)
        {
            ArgumentNullException.ThrowIfNull(tokenCache);

            tokenCache.SetBeforeAccess(beforeAccess: null);
            tokenCache.SetAfterAccess(afterAccess: null);
        }
    }
}
