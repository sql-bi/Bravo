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
        private static readonly byte[] DefaultEntropy = Encoding.Unicode.GetBytes($"{Environment.MachineName}|{Environment.UserName}|3ae*f-4aew1/L22");

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
            using var md5 = MD5.Create();
            md5.Initialize();

            var buffer = buffers.SelectMany((x) => x).ToArray(); // flatten byte arrays into a one-byte array (concatenate)
            var hashBytes = md5.ComputeHash(buffer);

            var hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            return hash;
        }

        public static string? SHA256Hash(string value)
        {
            if (value is null)
                return null;

            using var algorithm = new SHA256Managed();

            var stringBuilder = new StringBuilder();

            var buffer = Encoding.UTF8.GetBytes(value);
            var offset = 0;
            var count = Encoding.UTF8.GetByteCount(value);

            var bytes = algorithm.ComputeHash(buffer, offset, count);

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

        /// <summary>
        /// Converts SecureString to a DPAPI protected Base64 string
        /// </summary>
        public static string ToProtectedString(this SecureString secure)
        {
            var unsecuredChars = new char[secure.Length];
            try
            {
                var gch = GCHandle.Alloc(unsecuredChars, GCHandleType.Pinned);
                try
                {
                    var ptr = IntPtr.Zero;
                    try
                    {
                        ptr = Marshal.SecureStringToGlobalAllocUnicode(secure);
                        Marshal.Copy(ptr, unsecuredChars, 0, unsecuredChars.Length);
                        var unsecuredBytes = Encoding.Unicode.GetBytes(unsecuredChars);
                        try
                        {
                            var protectedBytes = Cryptography.Protect(unsecuredBytes);
                            return Convert.ToBase64String(protectedBytes);
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

        /// <summary>
        /// Converts the results of the ToProtectedString() method to SecureString
        /// </summary>
        /// <param name="protectedString">Protected string returned from method ToProtectedString()</param>
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
