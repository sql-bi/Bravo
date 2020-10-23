using Sqlbi.Bravo.Core.Security.Cryptography;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Sqlbi.Bravo.Core.Helpers
{
    internal static class SecurityHelpers
    {
        public static string ToHashSHA256(this string value)
        {
            if (value == null) 
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
                            var protectedBytes = DataProtection.Protect(unsecuredBytes);
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
        /// <param name="value">Protected string returned from method ToProtectedString()</param>
        public static SecureString ToSecureString(this string value)
        {
            var protectedBytes = Convert.FromBase64String(value);
            var unprotectedBytes = DataProtection.Unprotect(protectedBytes);
            try
            {
                var unprotectedChars = Encoding.Unicode.GetChars(unprotectedBytes);
                try
                {
                    var secureString = new SecureString();

                    foreach (var @char in unprotectedChars)
                        secureString.AppendChar(@char);

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
