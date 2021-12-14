using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Sqlbi.Bravo.Infrastructure.Security
{
    internal static class Cryptography
    {
        /// <summary>
        ///  Encrypts the data in a specified byte array.
        /// </summary>
        /// <remarks>The protected data is associated with the current user and only threads running under the current user context can unprotect the data.</remarks>
        public static byte[] Protect(byte[] userData) => ProtectedData.Protect(userData, optionalEntropy: null, DataProtectionScope.CurrentUser);

        /// <summary>
        ///  Decrypts the data in a specified byte array. 
        /// </summary>
        /// <remarks>The protected data is associated with the current user and only threads running under the current user context can unprotect the data.</remarks>
        public static byte[] Unprotect(byte[] encryptedData) => ProtectedData.Unprotect(encryptedData, optionalEntropy: null, DataProtectionScope.CurrentUser);

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

        /// <summary>
        /// Encodes the provided values into an MD5 hashed string
        /// </summary>
        public static string MD5Hash(long longValue, DateTime datetimeValue)
        {
            var datetimeLongValue = new DateTimeOffset(datetimeValue).ToUnixTimeMilliseconds();

            var buffers = new byte[][]
            {
                BitConverter.GetBytes(longValue),
                BitConverter.GetBytes(datetimeLongValue)
            };

            var hash = MD5Hash(buffers);
            return hash;
        }
    }
}
