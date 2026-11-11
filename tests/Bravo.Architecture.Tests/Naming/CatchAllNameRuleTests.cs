using System;
using Bravo.Architecture.Tests.Naming.Fixtures;
using Xunit;

namespace Bravo.Architecture.Tests.Naming;

public sealed class CatchAllNameRuleTests
{
    [Theory]
    [InlineData(typeof(TextHelper), "Helper")]
    [InlineData(typeof(TextHelpers), "Helpers")]
    [InlineData(typeof(ClockUtil), "Util")]
    [InlineData(typeof(ClockUtils), "Utils")]
    [InlineData(typeof(PathUtility), "Utility")]
    [InlineData(typeof(PathUtilities), "Utilities")]
    [InlineData(typeof(ModelCommon), "Common")]
    [InlineData(typeof(CacheHelper<>), "Helper")]
    [InlineData(typeof(Outer.NestedUtils), "Utils")]
    public void FindViolations_TypeNameWithCatchAllSuffix_ReportsTheSuffix(Type type, string suffix)
    {
        var violations = CatchAllNameRule.FindViolations([type]);

        Assert.Equal([$"{type.FullName}: the type name ends with '{suffix}'"], violations);
    }

    [Theory]
    [InlineData(typeof(Fixtures.Helpers.DaxFormatter), "Helpers")]
    [InlineData(typeof(Fixtures.Utils.DaxFormatter), "Utils")]
    [InlineData(typeof(Fixtures.Utilities.DaxFormatter), "Utilities")]
    [InlineData(typeof(Fixtures.Common.DaxFormatter), "Common")]
    public void FindViolations_NamespaceWithCatchAllSegment_ReportsTheSegment(Type type, string segment)
    {
        var violations = CatchAllNameRule.FindViolations([type]);

        Assert.Equal([$"{type.FullName}: the namespace has the segment '{segment}'"], violations);
    }

    [Theory]
    [InlineData(typeof(DaxFormatter))]
    [InlineData(typeof(HelperText))]
    [InlineData(typeof(CommonPrefix))]
    public void FindViolations_SpecificName_ReturnsNone(Type type)
    {
        var violations = CatchAllNameRule.FindViolations([type]);

        Assert.Empty(violations);
    }

    [Fact]
    public void FindViolations_ProductionAssemblies_ReturnsNone()
    {
        var violations = CatchAllNameRule.FindViolations(ProductionAssemblies.Types());

        RuleAssert.NoViolations(violations);
    }
}
