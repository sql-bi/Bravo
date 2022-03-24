namespace Bravo.Tests.Infrastructure.Security
{
    using Sqlbi.Bravo.Infrastructure.Security;
    using System;
    using System.Text;
    using Xunit;

    public class CryptographyTests
    {
        [Fact]
        public void ProtectUnprotect_SimpleTest()
        {
            var expected = "MySecret^ìfd56486-+{6 ♠ ⌂¿EFE==";

            var userData = Encoding.Unicode.GetBytes(expected);
            var encryptedData = Cryptography.Protect(userData);
            var unprotectedData = Cryptography.Unprotect(encryptedData);

            var actual = Encoding.Unicode.GetString(unprotectedData);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MD5Hash_SimpleTest()
        {
            var buffer = Encoding.UTF8.GetBytes("Bravo");

            var actual = Cryptography.MD5Hash(buffer);
            var expected = "01A2DA07BF36766155F48FC670D53FE8";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MD5Hash_Simple2DTest()
        {
            var buffers = new byte[][]
            {
                Encoding.UTF8.GetBytes("Bravo"),
                BitConverter.GetBytes(255),
            };

            var actual = Cryptography.MD5Hash(buffers);
            var expected = "E9F057A4A3A7B760F6D0C588DF6DAC91";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SHA256Hash_SimpleTest()
        {
            var actual = Cryptography.SHA256Hash("Bravo");
            var expected = "8123f58e72483f148509ae2da7feda62076dbe2ae3a045323bea4458a62d0952";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GenerateSimpleToken_SimpleTest()
        {
            var actual = Cryptography.GenerateSimpleToken();

            Assert.NotNull(actual);
            Assert.NotEmpty(actual);
            Assert.Equal(100, actual.Length);
            Assert.EndsWith("==", actual);
        }
    }
}
