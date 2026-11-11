using System.Linq;
using System.Reflection;
using Bravo.Architecture.Tests.StaticState.Fixtures;
using Xunit;

namespace Bravo.Architecture.Tests.StaticState;

public sealed class StaticMutableStateRuleTests
{
    private const BindingFlags StaticFields = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    [Fact]
    public void FindViolations_MutableStatics_ReportsEachMember()
    {
        var violations = StaticMutableStateRule.FindViolations([typeof(MutableStatics)]);

        var type = typeof(MutableStatics).FullName;
        string[] expected =
        [
            $"{type}.s_reassignable: the static member can be assigned after its initialization",
            $"{type}.s_array: the static member is an array, whose elements can be replaced",
            $"{type}.s_list: the static member is a collection that can be changed",
            $"{type}.s_dictionary: the static member is a collection that can be changed",
            $"{type}.Settable: the static member can be assigned after its initialization",
        ];
        Assert.Equal(expected.Order(), violations.Order());
    }

    [Fact]
    public void FindViolations_ConstantsAndReadOnlyValues_ReturnsNone()
    {
        var violations = StaticMutableStateRule.FindViolations([typeof(ImmutableStatics)]);

        Assert.Empty(violations);
    }

    [Fact]
    public void FindViolations_LambdaCachedByTheCompiler_ReturnsNone()
    {
        var closures = typeof(ModelCounter).GetNestedTypes(BindingFlags.NonPublic);

        var violations = StaticMutableStateRule.FindViolations(closures);

        Assert.Contains(closures.SelectMany(type => type.GetFields(StaticFields)), field => !field.IsInitOnly);
        Assert.Empty(violations);
    }

    [Fact]
    public void FindViolations_LoggerMessageMethod_ReturnsNone()
    {
        var violations = StaticMutableStateRule.FindViolations([typeof(ModelCounter)]);

        Assert.NotEmpty(typeof(ModelCounter).GetFields(StaticFields));
        Assert.Empty(violations);
    }

    [Fact]
    public void FindViolations_ProductionAssemblies_ReturnsNone()
    {
        var violations = StaticMutableStateRule.FindViolations(ProductionAssemblies.Types());

        RuleAssert.NoViolations(violations);
    }
}
