using System.Collections.Generic;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Policies;
using Xunit;

namespace Bravo.Tests.Infrastructure.Policies;

public class RegistryPolicyReaderTests
{
    public static IEnumerable<object[]> BoolPolicyNames() => new[]
    {
        new object[] { nameof(PolicySnapshot.TelemetryEnabled) },
        new object[] { nameof(PolicySnapshot.UpdateCheckEnabled) },
        new object[] { nameof(PolicySnapshot.UseSystemBrowserForAuthentication) },
        new object[] { nameof(PolicySnapshot.BuiltInTemplatesEnabled) },
        new object[] { nameof(PolicySnapshot.CustomTemplatesEnabled) },
    };

    private static bool? GetBoolProperty(PolicySnapshot policies, string propertyName)
        => (bool?)typeof(PolicySnapshot).GetProperty(propertyName)!.GetValue(policies);

    [Fact]
    public void FromSource_EmptySource_AllPropertiesAreNull()
    {
        var policies = RegistryPolicyReader.FromSource(new FakePolicySource());

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

        var policies = RegistryPolicyReader.FromSource(source);

        Assert.True(GetBoolProperty(policies, propertyName));
    }

    [Theory]
    [MemberData(nameof(BoolPolicyNames))]
    public void FromSource_BoolPolicyValueZero_ReturnsFalse(string propertyName)
    {
        var source = new FakePolicySource();
        source.Set(propertyName, 0);

        var policies = RegistryPolicyReader.FromSource(source);

        Assert.False(GetBoolProperty(policies, propertyName));
    }

    [Theory]
    [MemberData(nameof(BoolPolicyNames))]
    public void FromSource_BoolPolicyOutOfRangeIntValue_ReturnsNull(string propertyName)
    {
        var source = new FakePolicySource();
        source.Set(propertyName, 42);

        var policies = RegistryPolicyReader.FromSource(source);

        Assert.Null(GetBoolProperty(policies, propertyName));
    }

    [Theory]
    [MemberData(nameof(BoolPolicyNames))]
    public void FromSource_BoolPolicyNonIntValue_ReturnsNull(string propertyName)
    {
        var source = new FakePolicySource();
        source.Set(propertyName, "not-a-number");

        var policies = RegistryPolicyReader.FromSource(source);

        Assert.Null(GetBoolProperty(policies, propertyName));
    }

    [Fact]
    public void FromSource_UpdateChannelDefinedEnumValue_ReturnsParsedValue()
    {
        var source = new FakePolicySource();
        source.Set("UpdateChannel", (int)UpdateChannelType.Dev);

        var policies = RegistryPolicyReader.FromSource(source);

        Assert.Equal(UpdateChannelType.Dev, policies.UpdateChannel);
    }

    [Fact]
    public void FromSource_UpdateChannelUndefinedEnumValue_ReturnsNull()
    {
        // 1 is not a defined UpdateChannelType member (Beta is reserved/commented out) - must not be misparsed
        var source = new FakePolicySource();
        source.Set("UpdateChannel", 1);

        var policies = RegistryPolicyReader.FromSource(source);

        Assert.Null(policies.UpdateChannel);
    }

    [Fact]
    public void FromSource_UpdateChannelNonIntValue_ReturnsNull()
    {
        var source = new FakePolicySource();
        source.Set("UpdateChannel", "Dev");

        var policies = RegistryPolicyReader.FromSource(source);

        Assert.Null(policies.UpdateChannel);
    }

    [Fact]
    public void FromSource_CustomTemplatesOrganizationRepositoryPathStringValueSet_ReturnsValue()
    {
        var source = new FakePolicySource();
        source.Set("CustomTemplatesOrganizationRepositoryPath", @"C:\Templates\Org");

        var policies = RegistryPolicyReader.FromSource(source);

        Assert.Equal(@"C:\Templates\Org", policies.CustomTemplatesOrganizationRepositoryPath);
    }

    [Fact]
    public void FromSource_CustomTemplatesOrganizationRepositoryPathNonStringValue_ReturnsNull()
    {
        var source = new FakePolicySource();
        source.Set("CustomTemplatesOrganizationRepositoryPath", 123);

        var policies = RegistryPolicyReader.FromSource(source);

        Assert.Null(policies.CustomTemplatesOrganizationRepositoryPath);
    }

    private static readonly PolicySnapshot AllNull = new(
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

        var merged = RegistryPolicyReader.Merge(machine, user);

        Assert.True(merged.TelemetryEnabled);
    }

    [Fact]
    public void Merge_OnlyUserValueSet_ReturnsUserValue()
    {
        var machine = AllNull;
        var user = AllNull with { TelemetryEnabled = false };

        var merged = RegistryPolicyReader.Merge(machine, user);

        Assert.False(merged.TelemetryEnabled);
    }

    [Fact]
    public void Merge_BothSet_MachineTakesPrecedenceOverUser()
    {
        var machine = AllNull with { TelemetryEnabled = true, UpdateChannel = UpdateChannelType.Stable, UpdateCheckEnabled = false };
        var user = AllNull with { TelemetryEnabled = false, UpdateChannel = UpdateChannelType.Dev, UpdateCheckEnabled = true };

        var merged = RegistryPolicyReader.Merge(machine, user);

        Assert.True(merged.TelemetryEnabled);
        Assert.Equal(UpdateChannelType.Stable, merged.UpdateChannel);
        Assert.False(merged.UpdateCheckEnabled);
    }

    [Fact]
    public void Merge_NeitherSet_ReturnsNull()
    {
        var merged = RegistryPolicyReader.Merge(AllNull, AllNull);

        Assert.Null(merged.TelemetryEnabled);
    }

    [Fact]
    public void Merge_EachPropertyResolvedIndependently()
    {
        // Guards against a copy-paste wiring mistake in Merge() (e.g. reading the wrong
        // property from machine/user) by exercising all 7 properties in a single assertion,
        // each with a distinct machine/user combination.
        var machine = new PolicySnapshot(
            TelemetryEnabled: true,
            UpdateChannel: null,
            UpdateCheckEnabled: null,
            UseSystemBrowserForAuthentication: true,
            BuiltInTemplatesEnabled: null,
            CustomTemplatesEnabled: null,
            CustomTemplatesOrganizationRepositoryPath: null);

        var user = new PolicySnapshot(
            TelemetryEnabled: false, // machine wins
            UpdateChannel: UpdateChannelType.Dev, // machine unset -> user wins
            UpdateCheckEnabled: true, // machine unset -> user wins
            UseSystemBrowserForAuthentication: false, // machine wins
            BuiltInTemplatesEnabled: false, // machine unset -> user wins
            CustomTemplatesEnabled: null, // neither set
            CustomTemplatesOrganizationRepositoryPath: @"C:\User\Path"); // machine unset -> user wins

        var merged = RegistryPolicyReader.Merge(machine, user);

        Assert.True(merged.TelemetryEnabled);
        Assert.Equal(UpdateChannelType.Dev, merged.UpdateChannel);
        Assert.True(merged.UpdateCheckEnabled);
        Assert.True(merged.UseSystemBrowserForAuthentication);
        Assert.False(merged.BuiltInTemplatesEnabled);
        Assert.Null(merged.CustomTemplatesEnabled);
        Assert.Equal(@"C:\User\Path", merged.CustomTemplatesOrganizationRepositoryPath);
    }
}
