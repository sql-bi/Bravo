using Bravo.Desktop.Development;
using Bravo.Desktop.Policies;
using Bravo.Platform.Windows.Policies;
using Microsoft.Win32;
using Xunit;

namespace Bravo.Desktop.Tests.Policies;

public sealed class PolicyKeysTests
{
    [Fact]
    public void For_ReleaseBuild_IsTheKeyOfTheComputerThenTheOneOfTheUser()
    {
        var keys = PolicyKeys.For(BuildIdentity.Release("1.1.0"), DevelopmentOptions.None);

        RegistryPolicyKey[] expected =
        [
            new(Registry.LocalMachine, @"Software\Policies\SQLBI\Bravo\OptionSettings"),
            new(Registry.CurrentUser, @"Software\Policies\SQLBI\Bravo\OptionSettings"),
        ];
        Assert.Equal(expected, keys);
    }

    [Fact]
    public void For_DevelopmentBuildWithoutAKey_IsNoKey()
    {
        Assert.Empty(PolicyKeys.For(BuildIdentity.Development("1.1.0"), DevelopmentOptions.None));
    }

    [Fact]
    public void For_DevelopmentBuildWithAKey_IsThatKeyUnderTheUser()
    {
        var development = new DevelopmentOptions { PolicyKeyName = @"Software\Bravo.Tests\Policies" };

        var keys = PolicyKeys.For(BuildIdentity.Development("1.1.0"), development);

        Assert.Equal([new RegistryPolicyKey(Registry.CurrentUser, @"Software\Bravo.Tests\Policies")], keys);
    }
}
