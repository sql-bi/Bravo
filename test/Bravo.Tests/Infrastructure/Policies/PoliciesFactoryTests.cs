namespace Bravo.Tests.Infrastructure.Policies;

using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Policies;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// Exercises PoliciesFactory's parsing/precedence logic using an in-memory fake instead of the
/// real registry. Registry-adapter behavior itself is covered separately by
/// <see cref="RegistryPolicySourceTests"/>.
/// </summary>
public class PoliciesFactoryTests
{
    private sealed class FakePolicySource : IPolicySource
    {
        private readonly Dictionary<string, object> _values = new();

        public void Set(string name, object value) => _values[name] = value;

        public int? GetInt(string name)
            => _values.TryGetValue(name, out var value) && value is int intValue ? intValue : null;

        public string? GetString(string name)
            => _values.TryGetValue(name, out var value) ? value as string : null;
    }

    // MemberData deliberately carries only the (public) property name rather than a
    // Func<Policies, bool?> selector: Policies/IPolicies are internal, and a public test
    // method cannot declare a parameter of a less-accessible type (CS0051), even with
    // InternalsVisibleTo. The property is read via reflection inside the test body instead.
    public static IEnumerable<object[]> BoolPolicyNames() => new[]
    {
        new object[] { nameof(IPolicies.TelemetryEnabled) },
        new object[] { nameof(IPolicies.UpdateCheckEnabled) },
        new object[] { nameof(IPolicies.UseSystemBrowserForAuthentication) },
        new object[] { nameof(IPolicies.BuiltInTemplatesEnabled) },
        new object[] { nameof(IPolicies.CustomTemplatesEnabled) },
    };

    private static bool? GetBoolProperty(Policies policies, string propertyName)
        => (bool?)typeof(Policies).GetProperty(propertyName)!.GetValue(policies);

    [Fact]
    public void FromSource_EmptySource_AllPropertiesAreNull()
    {
        var policies = PoliciesFactory.FromSource(new FakePolicySource());

        Assert.Null(policies.TelemetryEnabled);
        Assert.Null(policies.UpdateChannel);
        Assert.Null(policies.UpdateCheckEnabled);
        Assert.Null(policies.UseSystemBrowserForAuthentication);
        Assert.Null(policies.BuiltInTemplatesEnabled);
        Assert.Null(policies.CustomTemplatesEnabled);
        Assert.Null(policies.CustomTemplatesOrganizationRepositoryPath);
    }

    [Theory]
    [MemberData(nameof(BoolPolicyNames))]
    public void FromSource_BoolPolicyValueOne_ReturnsTrue(string propertyName)
    {
        var source = new FakePolicySource();
        source.Set(propertyName, 1);

        var policies = PoliciesFactory.FromSource(source);

        Assert.True(GetBoolProperty(policies, propertyName));
    }

    [Theory]
    [MemberData(nameof(BoolPolicyNames))]
    public void FromSource_BoolPolicyValueZero_ReturnsFalse(string propertyName)
    {
        var source = new FakePolicySource();
        source.Set(propertyName, 0);

        var policies = PoliciesFactory.FromSource(source);

        Assert.False(GetBoolProperty(policies, propertyName));
    }

    [Theory]
    [MemberData(nameof(BoolPolicyNames))]
    public void FromSource_BoolPolicyOutOfRangeIntValue_ReturnsNull(string propertyName)
    {
        var source = new FakePolicySource();
        source.Set(propertyName, 42);

        var policies = PoliciesFactory.FromSource(source);

        Assert.Null(GetBoolProperty(policies, propertyName));
    }

    [Theory]
    [MemberData(nameof(BoolPolicyNames))]
    public void FromSource_BoolPolicyNonIntValue_ReturnsNull(string propertyName)
    {
        var source = new FakePolicySource();
        source.Set(propertyName, "not-a-number");

        var policies = PoliciesFactory.FromSource(source);

        Assert.Null(GetBoolProperty(policies, propertyName));
    }

    [Fact]
    public void FromSource_UpdateChannelDefinedEnumValue_ReturnsParsedValue()
    {
        var source = new FakePolicySource();
        source.Set("UpdateChannel", (int)UpdateChannelType.Dev);

        var policies = PoliciesFactory.FromSource(source);

        Assert.Equal(UpdateChannelType.Dev, policies.UpdateChannel);
    }

    [Fact]
    public void FromSource_UpdateChannelUndefinedEnumValue_ReturnsNull()
    {
        // 1 is not a defined UpdateChannelType member (Beta is reserved/commented out) - must not be misparsed
        var source = new FakePolicySource();
        source.Set("UpdateChannel", 1);

        var policies = PoliciesFactory.FromSource(source);

        Assert.Null(policies.UpdateChannel);
    }

    [Fact]
    public void FromSource_UpdateChannelNonIntValue_ReturnsNull()
    {
        var source = new FakePolicySource();
        source.Set("UpdateChannel", "Dev");

        var policies = PoliciesFactory.FromSource(source);

        Assert.Null(policies.UpdateChannel);
    }

    [Fact]
    public void FromSource_CustomTemplatesOrganizationRepositoryPathStringValueSet_ReturnsValue()
    {
        var source = new FakePolicySource();
        source.Set("CustomTemplatesOrganizationRepositoryPath", @"C:\Templates\Org");

        var policies = PoliciesFactory.FromSource(source);

        Assert.Equal(@"C:\Templates\Org", policies.CustomTemplatesOrganizationRepositoryPath);
    }

    [Fact]
    public void FromSource_CustomTemplatesOrganizationRepositoryPathNonStringValue_ReturnsNull()
    {
        var source = new FakePolicySource();
        source.Set("CustomTemplatesOrganizationRepositoryPath", 123);

        var policies = PoliciesFactory.FromSource(source);

        Assert.Null(policies.CustomTemplatesOrganizationRepositoryPath);
    }

    [Fact]
    public void Create_DoesNotThrow_AndReturnsAnInstance()
    {
        // Create() reads from the real HKLM/HKCU policy path, so its output depends on the
        // machine's actual group-policy configuration and cannot be asserted deterministically here.
        // This is a smoke test for the composition wiring (LocalMachine + CurrentUser -> Merge);
        // the precedence rule itself is covered deterministically by the Merge tests below.
        var policies = PoliciesFactory.Create();

        Assert.NotNull(policies);
    }

    private static readonly Policies AllNull = new(
        TelemetryEnabled: null,
        UpdateChannel: null,
        UpdateCheckEnabled: null,
        UseSystemBrowserForAuthentication: null,
        BuiltInTemplatesEnabled: null,
        CustomTemplatesEnabled: null,
        CustomTemplatesOrganizationRepositoryPath: null);

    [Fact]
    public void Merge_OnlyMachineValueSet_ReturnsMachineValue()
    {
        var machine = AllNull with { TelemetryEnabled = true };
        var user = AllNull;

        var merged = PoliciesFactory.Merge(machine, user);

        Assert.True(merged.TelemetryEnabled);
    }

    [Fact]
    public void Merge_OnlyUserValueSet_ReturnsUserValue()
    {
        var machine = AllNull;
        var user = AllNull with { TelemetryEnabled = false };

        var merged = PoliciesFactory.Merge(machine, user);

        Assert.False(merged.TelemetryEnabled);
    }

    [Fact]
    public void Merge_BothSet_MachineTakesPrecedenceOverUser()
    {
        var machine = AllNull with { TelemetryEnabled = true, UpdateChannel = UpdateChannelType.Stable, UpdateCheckEnabled = false };
        var user = AllNull with { TelemetryEnabled = false, UpdateChannel = UpdateChannelType.Dev, UpdateCheckEnabled = true };

        var merged = PoliciesFactory.Merge(machine, user);

        Assert.True(merged.TelemetryEnabled);
        Assert.Equal(UpdateChannelType.Stable, merged.UpdateChannel);
        Assert.False(merged.UpdateCheckEnabled);
    }

    [Fact]
    public void Merge_NeitherSet_ReturnsNull()
    {
        var merged = PoliciesFactory.Merge(AllNull, AllNull);

        Assert.Null(merged.TelemetryEnabled);
    }

    [Fact]
    public void Merge_EachPropertyResolvedIndependently()
    {
        // Guards against a copy-paste wiring mistake in Merge() (e.g. reading the wrong
        // property from machine/user) by exercising all 7 properties in a single assertion,
        // each with a distinct machine/user combination.
        var machine = new Policies(
            TelemetryEnabled: true,
            UpdateChannel: null,
            UpdateCheckEnabled: null,
            UseSystemBrowserForAuthentication: true,
            BuiltInTemplatesEnabled: null,
            CustomTemplatesEnabled: null,
            CustomTemplatesOrganizationRepositoryPath: null);

        var user = new Policies(
            TelemetryEnabled: false, // machine wins
            UpdateChannel: UpdateChannelType.Dev, // machine unset -> user wins
            UpdateCheckEnabled: true, // machine unset -> user wins
            UseSystemBrowserForAuthentication: false, // machine wins
            BuiltInTemplatesEnabled: false, // machine unset -> user wins
            CustomTemplatesEnabled: null, // neither set
            CustomTemplatesOrganizationRepositoryPath: @"C:\User\Path"); // machine unset -> user wins

        var merged = PoliciesFactory.Merge(machine, user);

        Assert.True(merged.TelemetryEnabled);
        Assert.Equal(UpdateChannelType.Dev, merged.UpdateChannel);
        Assert.True(merged.UpdateCheckEnabled);
        Assert.True(merged.UseSystemBrowserForAuthentication);
        Assert.False(merged.BuiltInTemplatesEnabled);
        Assert.Null(merged.CustomTemplatesEnabled);
        Assert.Equal(@"C:\User\Path", merged.CustomTemplatesOrganizationRepositoryPath);
    }
}
