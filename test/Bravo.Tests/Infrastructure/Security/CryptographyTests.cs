using Sqlbi.Bravo.Infrastructure.Security;
using System.Text;
using Xunit;

namespace Bravo.Tests.Infrastructure.Security
{
    public class CryptographyTests
    {
        [Fact]
        public void Cryptography_ProtectUnprotect_ComputeCorrectResult()
        {
            var expected = "MySecret^ìfd56486-+{6 ♠ ⌂¿EFE==";

            var userData = Encoding.Unicode.GetBytes(expected);
            var encryptedData = Cryptography.Protect(userData);
            var unprotectedData = Cryptography.Unprotect(encryptedData);

            var actual = Encoding.Unicode.GetString(unprotectedData);

            Assert.Equal(expected, actual);
        }
    }
}
