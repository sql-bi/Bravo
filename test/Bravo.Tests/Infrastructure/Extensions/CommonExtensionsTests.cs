using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Xunit;

namespace Bravo.Tests.Infrastructure.Extensions;

public class CommonExtensionsTests
{
    [Fact]
    public void TryParseTo_IntTest()
    {
        var expected = DiagnosticLevelType.Verbose;
        var actual = EnumExtensions.TryParseTo<DiagnosticLevelType>((int)expected);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryParseTo_IntNullTest1()
    {
        var @enum = EnumExtensions.TryParseTo<DiagnosticLevelType>((int?)null);
        Assert.Null(@enum);
    }

    [Fact]
    public void TryParseTo_IntNullTest2()
    {
        var @enum = EnumExtensions.TryParseTo<DiagnosticLevelType>(int.MaxValue);
        Assert.Null(@enum);
    }
}
