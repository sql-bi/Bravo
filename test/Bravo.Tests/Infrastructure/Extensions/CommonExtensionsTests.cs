namespace Bravo.Tests.Infrastructure.Extensions
{
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Xunit;

    public class CommonExtensionsTests
    {
        [Fact]
        public void TryParseTo_IntTest()
        {
            var expected = UpdateChannelType.Dev;
            var actual = EnumExtensions.TryParseTo<UpdateChannelType>((int)expected);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TryParseTo_IntNullTest1()
        {
            var @enum = EnumExtensions.TryParseTo<UpdateChannelType>((int?)null);
            Assert.Null(@enum);
        }

        [Fact]
        public void TryParseTo_IntNullTest2()
        {
            var @enum = EnumExtensions.TryParseTo<UpdateChannelType>(int.MaxValue);
            Assert.Null(@enum);
        }
    }
}
