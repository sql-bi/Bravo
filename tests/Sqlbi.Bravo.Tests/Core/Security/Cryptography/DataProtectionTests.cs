using Sqlbi.Bravo.Core.Security.Cryptography;
using System.Text;
using Xunit;

namespace Sqlbi.Bravo.Tests.Core.Security.Cryptography
{
    public class DataProtectionTests
    {
        [Fact]
        public void DataProtection_ProtectUnprotect_ComputeCorrectResult()
        {
            var expected = "MySecret^ìfd56486-+{6 ♠ ⌂¿EFE==";

            var userData = Encoding.Unicode.GetBytes(expected);
            var encryptedData = DataProtection.Protect(userData);
            var unprotectedData = DataProtection.Unprotect(encryptedData);

            var actual = Encoding.Unicode.GetString(unprotectedData);

            Assert.Equal(expected, actual);
        }
    }
}
