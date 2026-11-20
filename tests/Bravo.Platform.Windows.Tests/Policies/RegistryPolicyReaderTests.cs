using System;
using Bravo.Core.Policies;
using Bravo.Platform.Windows.Policies;
using Microsoft.Win32;
using Xunit;

namespace Bravo.Platform.Windows.Tests.Policies;

public sealed class RegistryPolicyReaderTests : IDisposable
{
    private const string First = "First";
    private const string Second = "Second";

    private readonly TemporaryRegistryKey _registry = new();

    public void Dispose()
    {
        _registry.Dispose();
    }

    // The values that the templates write for Enabled and Disabled, and the two values without a template.
    public static TheoryData<string, int, ApplicationPolicies> SwitchesOf1014 => new()
    {
        { "TelemetryEnabled", 1, new() { TelemetryEnabled = true } },
        { "TelemetryEnabled", 0, new() { TelemetryEnabled = false } },
        { "UpdateCheckEnabled", 1, new() { UpdateCheckEnabled = true } },
        { "UpdateCheckEnabled", 0, new() { UpdateCheckEnabled = false } },
        { "UseSystemBrowserForAuthentication", 1, new() { UseSystemBrowserForAuthentication = true } },
        { "UseSystemBrowserForAuthentication", 0, new() { UseSystemBrowserForAuthentication = false } },
        { "BuiltInTemplatesEnabled", 1, new() { BuiltInTemplatesEnabled = true } },
        { "BuiltInTemplatesEnabled", 0, new() { BuiltInTemplatesEnabled = false } },
        { "CustomTemplatesEnabled", 1, new() { CustomTemplatesEnabled = true } },
        { "CustomTemplatesEnabled", 0, new() { CustomTemplatesEnabled = false } },
    };

    [Fact]
    public void Read_NoKeys_IsNoPolicy()
    {
        Assert.Equal(ApplicationPolicies.None, RegistryPolicyReader.Read([]));
    }

    [Fact]
    public void Read_KeyThatDoesNotExist_IsNoPolicy()
    {
        Assert.Equal(ApplicationPolicies.None, RegistryPolicyReader.Read([_registry.PolicyKey(First)]));
    }

    [Theory]
    [MemberData(nameof(SwitchesOf1014))]
    public void Read_SwitchOf1014_IsItsValue(string valueName, int value, ApplicationPolicies expected)
    {
        _registry.SetValue(First, valueName, value, RegistryValueKind.DWord);

        Assert.Equal(expected, RegistryPolicyReader.Read([_registry.PolicyKey(First)]));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void Read_SwitchOfAnotherNumber_IsNotConfigured(int value)
    {
        _registry.SetValue(First, "TelemetryEnabled", value, RegistryValueKind.DWord);

        Assert.Equal(ApplicationPolicies.None, RegistryPolicyReader.Read([_registry.PolicyKey(First)]));
    }

    private static readonly string[] s_multiStringOfOne = ["1"];
    private static readonly byte[] s_binaryOfOne = [1, 0, 0, 0];

    public static TheoryData<object, RegistryValueKind> ValuesOfAnotherKind => new()
    {
        { "1", RegistryValueKind.String },
        { "1", RegistryValueKind.ExpandString },
        { s_multiStringOfOne, RegistryValueKind.MultiString },
        { 1L, RegistryValueKind.QWord },
        { s_binaryOfOne, RegistryValueKind.Binary },
    };

    [Theory]
    [MemberData(nameof(ValuesOfAnotherKind))]
    public void Read_SwitchOfAnotherKind_IsNotConfigured(object value, RegistryValueKind kind)
    {
        _registry.SetValue(First, "UpdateCheckEnabled", value, kind);

        Assert.Equal(ApplicationPolicies.None, RegistryPolicyReader.Read([_registry.PolicyKey(First)]));
    }

    [Theory]
    [InlineData(@"\\contoso\templates")]
    [InlineData(@"%USERPROFILE%\Templates")]
    [InlineData("")]
    public void Read_FolderAsAString_IsTheTextAsWritten(string folder)
    {
        _registry.SetValue(First, "CustomTemplatesOrganizationRepositoryPath", folder, RegistryValueKind.String);

        var policies = RegistryPolicyReader.Read([_registry.PolicyKey(First)]);

        Assert.Equal(folder, policies.CustomTemplatesOrganizationRepositoryPath);
    }

    [Fact]
    public void Read_FolderAsAnExpandableString_IsNotConfigured()
    {
        _registry.SetValue(First, "CustomTemplatesOrganizationRepositoryPath", @"%USERPROFILE%\Templates", RegistryValueKind.ExpandString);

        Assert.Equal(ApplicationPolicies.None, RegistryPolicyReader.Read([_registry.PolicyKey(First)]));
    }

    // The channel is gone: whatever a policy of 1.0.x says of it changes nothing.
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Read_UpdateChannel_IsNotRead(int channel)
    {
        _registry.SetValue(First, "UpdateChannel", channel, RegistryValueKind.DWord);

        Assert.Equal(ApplicationPolicies.None, RegistryPolicyReader.Read([_registry.PolicyKey(First)]));
    }

    [Fact]
    public void Read_ValueInBothKeys_IsTheOneOfTheFirstKey()
    {
        _registry.SetValue(First, "TelemetryEnabled", 0, RegistryValueKind.DWord);
        _registry.SetValue(Second, "TelemetryEnabled", 1, RegistryValueKind.DWord);

        var policies = RegistryPolicyReader.Read([_registry.PolicyKey(First), _registry.PolicyKey(Second)]);

        Assert.Equal(new ApplicationPolicies { TelemetryEnabled = false }, policies);
    }

    // Value by value, as 1.0.x reads them: the first key does not hide what it does not set.
    [Fact]
    public void Read_ValuesInDifferentKeys_AreAllRead()
    {
        _registry.SetValue(First, "TelemetryEnabled", 0, RegistryValueKind.DWord);
        _registry.SetValue(Second, "UpdateCheckEnabled", 0, RegistryValueKind.DWord);
        _registry.SetValue(Second, "CustomTemplatesOrganizationRepositoryPath", @"\\contoso\templates", RegistryValueKind.String);

        var policies = RegistryPolicyReader.Read([_registry.PolicyKey(First), _registry.PolicyKey(Second)]);

        var expected = new ApplicationPolicies
        {
            TelemetryEnabled = false,
            UpdateCheckEnabled = false,
            CustomTemplatesOrganizationRepositoryPath = @"\\contoso\templates",
        };
        Assert.Equal(expected, policies);
    }

    [Fact]
    public void Read_ValueNotUnderstoodInTheFirstKey_IsTheOneOfTheSecondKey()
    {
        _registry.SetValue(First, "UpdateCheckEnabled", 2, RegistryValueKind.DWord);
        _registry.SetValue(Second, "UpdateCheckEnabled", 0, RegistryValueKind.DWord);

        var policies = RegistryPolicyReader.Read([_registry.PolicyKey(First), _registry.PolicyKey(Second)]);

        Assert.Equal(new ApplicationPolicies { UpdateCheckEnabled = false }, policies);
    }

    // The keys of the application are written only on a runner of GitHub Actions, which is elevated and thrown
    // away after the run: nowhere else does a test touch the policies of a machine.
    [Fact]
    public void Read_ApplicationKeys_ReadsTheComputerThenTheUserValueByValue()
    {
        Assert.SkipUnless(
            Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true",
            "It writes the policies of the computer and of the user: it runs on a runner of GitHub Actions only.");
        const string VendorKeyName = @"Software\Policies\SQLBI";
        Assert.Null(Registry.LocalMachine.OpenSubKey(VendorKeyName));
        Assert.Null(Registry.CurrentUser.OpenSubKey(VendorKeyName));

        try
        {
            using (var machine = Registry.LocalMachine.CreateSubKey(RegistryPolicyReader.KeyName))
            {
                machine.SetValue("TelemetryEnabled", 0, RegistryValueKind.DWord);
                machine.SetValue("UpdateCheckEnabled", 2, RegistryValueKind.DWord);
            }

            using (var user = Registry.CurrentUser.CreateSubKey(RegistryPolicyReader.KeyName))
            {
                user.SetValue("TelemetryEnabled", 1, RegistryValueKind.DWord);
                user.SetValue("UpdateCheckEnabled", 0, RegistryValueKind.DWord);
                user.SetValue("CustomTemplatesEnabled", 0, RegistryValueKind.DWord);
            }

            var policies = RegistryPolicyReader.Read(RegistryPolicyReader.ApplicationKeys);

            var expected = new ApplicationPolicies { TelemetryEnabled = false, UpdateCheckEnabled = false, CustomTemplatesEnabled = false };
            Assert.Equal(expected, policies);
        }
        finally
        {
            Registry.LocalMachine.DeleteSubKeyTree(VendorKeyName, throwOnMissingSubKey: false);
            Registry.CurrentUser.DeleteSubKeyTree(VendorKeyName, throwOnMissingSubKey: false);
        }
    }

    [Fact]
    public void Read_KeyThatCannotBeRead_FailsAndNamesTheKey()
    {
        _registry.SetValue(Second, "TelemetryEnabled", 0, RegistryValueKind.DWord);
        _registry.DenyReading(Second);
        var key = _registry.PolicyKey(Second);

        var exception = Assert.Throws<PolicyReadException>(() => RegistryPolicyReader.Read([_registry.PolicyKey(First), key]));

        Assert.Equal($@"HKEY_CURRENT_USER\{_registry.Name}\{Second}", exception.KeyName);
    }
}
