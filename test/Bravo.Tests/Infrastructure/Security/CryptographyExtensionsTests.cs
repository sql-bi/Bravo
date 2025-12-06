namespace Bravo.Tests.Infrastructure.Security
{
    using Sqlbi.Bravo.Infrastructure.Security;
    using System.Net;
    using Xunit;

    public class CryptographyExtensionsTests
    {
        [Theory]
        [InlineData("MyValue123456789$", "5f45bfb8ab5c9ea6fe0762974f7bbbe3602155c66d1139496fc2246c360a874b")]
        [InlineData("1234567890??=", "cb0f4399a2850ee589414b82de85b736ced269035c48e4275be850a3162ba284")]
        [InlineData("abcdefghiABCDEFGHI??=", "1bca6736f96f84e35fa921938f45ba981a6e3f6aa02bcf46763009d3614cf89d")]
        [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
        [InlineData(null, null)]
        public void ToSHA256Hash_SimpleTest(string value, string expected)
        {
            var actual = value.ToSHA256Hash();
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("MySecret^ìfd56486-+{6 ♠ ⌂¿EFE==")]
        [InlineData("AsDfJkIl123456")]
        [InlineData("123==")]
        [InlineData("")]
        public void ToProtectedStringToSecureString_SimpleTest(string expected)
        {
            var secureString = new NetworkCredential(string.Empty, expected).SecurePassword;
            var protectedString = secureString.ToProtectedString();
            var secureStringActual = protectedString.ToSecureString();
            var actual = new NetworkCredential(string.Empty, secureStringActual).Password;

            Assert.Equal(expected, actual);
        }
    }
}
