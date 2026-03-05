namespace Sqlbi.Bravo.Infrastructure.Security
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Cryptography;
    using System.Text;

    internal static class Cryptography
    {
        // !!!
        // !!! Do not use Environment.MachineName for entropy string as the DataProtectionScope is DataProtectionScope.CurrentUser only.
        // !!! 
        // This prevent an error where a user with a roaming profile cannot decrypt the data from another computer on the network (e.g. MSAL.NET token cache)
        // See CryptProtectData function (dpapi.h) https://docs.microsoft.com/en-us/windows/win32/api/dpapi/nf-dpapi-cryptprotectdata
        private static readonly byte[] DefaultEntropy = Encoding.Unicode.GetBytes($"{Environment.UserName}|3ae*f-4aew1/L22");

        /// <summary>
        ///  Encrypts the data in a specified byte array.
        /// </summary>
        /// <remarks>The protected data is associated with the current user and only threads running under the current user context can unprotect the data.</remarks>
        public static byte[] Protect(byte[] userData, byte[]? entropy = null) => ProtectedData.Protect(userData, entropy ?? DefaultEntropy, DataProtectionScope.CurrentUser);

        /// <summary>
        ///  Decrypts the data in a specified byte array. 
        /// </summary>
        /// <remarks>The protected data is associated with the current user and only threads running under the current user context can unprotect the data.</remarks>
        public static byte[] Unprotect(byte[] encryptedData, byte[]? entropy = null) => ProtectedData.Unprotect(encryptedData, entropy ?? DefaultEntropy, DataProtectionScope.CurrentUser);

        /// <summary>
        /// Encodes the provided buffers into an MD5 hashed string
        /// </summary>
        public static string MD5Hash(byte[][] buffers)
        {
            var buffer = buffers.SelectMany((x) => x).ToArray(); // flatten byte arrays into a one-byte array (concatenate)
            var hash = MD5Hash(buffer);

            return hash;
        }

        /// <summary>
        /// Encodes the provided buffers into an MD5 hashed string
        /// </summary>
        public static string MD5Hash(byte[] buffer)
        {
            using var md5 = MD5.Create();
            md5.Initialize();

            var hashBytes = md5.ComputeHash(buffer);
            var hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);

            return hash;
        }

        public static string? SHA256Hash(string value)
        {
            if (value is null)
                return null;

            using var algorithm = SHA256.Create();
            var stringBuilder = new StringBuilder();

            var buffer = Encoding.UTF8.GetBytes(value);
            var count = Encoding.UTF8.GetByteCount(value);
            var bytes = algorithm.ComputeHash(buffer, offset: 0, count);

            foreach (var @byte in bytes)
                stringBuilder.Append(@byte.ToString("x2"));

            return stringBuilder.ToString();
        }

        public static string GenerateSimpleToken()
        {
            var token = $"{Guid.NewGuid()}-{Guid.NewGuid()}";
            var tokenBytes = Encoding.UTF8.GetBytes(token);

            token = Convert.ToBase64String(tokenBytes, Base64FormattingOptions.None);
            return token;
        }
    }

    internal static class CriptographyExtensions
    {
        public static string? ToSHA256Hash(this string value) => Cryptography.SHA256Hash(value);

        public static string ToProtectedString(this SecureString secureString)
        {
            var unsecuredChars = new char[secureString.Length];
            try
            {
                var gch = GCHandle.Alloc(unsecuredChars, GCHandleType.Pinned);
                try
                {
                    var ptr = IntPtr.Zero;
                    try
                    {
                        ptr = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                        Marshal.Copy(ptr, unsecuredChars, 0, unsecuredChars.Length);
                        var unsecuredBytes = Encoding.Unicode.GetBytes(unsecuredChars);
                        try
                        {
                            var protectedBytes = Cryptography.Protect(unsecuredBytes);
                            var protectedString = Convert.ToBase64String(protectedBytes);

                            return protectedString;
                        }
                        finally
                        {
                            Array.Clear(unsecuredBytes, 0, unsecuredBytes.Length);
                        }
                    }
                    finally
                    {
                        Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                    }
                }
                finally
                {
                    gch.Free();
                }
            }
            finally
            {
                Array.Clear(unsecuredChars, 0, unsecuredChars.Length);
            }
        }

        public static SecureString ToSecureString(this string protectedString)
        {
            var protectedBytes = Convert.FromBase64String(protectedString);
            var unprotectedBytes = Cryptography.Unprotect(protectedBytes);
            try
            {
                var unprotectedChars = Encoding.Unicode.GetChars(unprotectedBytes);
                try
                {
                    var secureString = new SecureString();

                    foreach (var @char in unprotectedChars)
                        secureString.AppendChar(@char);

                    secureString.MakeReadOnly();
                    return secureString;
                }
                finally
                {
                    Array.Clear(unprotectedChars, 0, unprotectedChars.Length);
                }
            }
            finally
            {
                Array.Clear(unprotectedBytes, 0, unprotectedBytes.Length);
            }
        }
    }
}
