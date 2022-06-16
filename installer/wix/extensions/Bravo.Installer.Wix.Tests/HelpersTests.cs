namespace Bravo.Installer.WiX.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Sqlbi.Bravo.Installer.Wix;

    [TestClass]
    public class HelpersTests
    {
        [TestMethod]
        public void ToSHA256Hash_Test1()
        {
            var actual = Helpers.ToSHA256Hash("Bravo");
            var expected = "8123f58e72483f148509ae2da7feda62076dbe2ae3a045323bea4458a62d0952";

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ToSHA256Hash_Test2()
        {
            var actual = Helpers.ToSHA256Hash("LAPTOP-12C9A7VU\\SYSTEM");
            var expected = "e4f87d099028e128ea2b413f4fa6fc741426bef314de588b0920e89f22015bd0";

            Assert.AreEqual(expected, actual);
        }
    }
}
