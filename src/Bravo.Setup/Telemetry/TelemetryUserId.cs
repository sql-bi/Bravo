using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Bravo.Setup.Telemetry
{
    /// <summary>
    /// The identifier that telemetry counts a user by: a hash of the names of the machine and of the account,
    /// computed as 1.0.x computes it, so that an installation of this version counts as the same user.
    /// </summary>
    internal static class TelemetryUserId
    {
        public static string Create(string machineName, string userName)
        {
            if (machineName == null)
            {
                throw new ArgumentNullException(nameof(machineName));
            }

            if (userName == null)
            {
                throw new ArgumentNullException(nameof(userName));
            }

            byte[] hash;
            using (var sha256 = SHA256.Create())
            {
                hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineName + @"\" + userName));
            }

            var hex = new StringBuilder(hash.Length * 2);
            foreach (var value in hash)
            {
                hex.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            }

            return hex.ToString();
        }
    }
}
