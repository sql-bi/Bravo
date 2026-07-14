namespace Bravo.Tests.Infrastructure.Policies;

using Microsoft.Win32;
using Sqlbi.Bravo.Infrastructure.Policies;
using System;
using Xunit;

/// <summary>
/// Covers only the thin RegistryKey-to-IPolicySource bridging contract (missing key, missing
/// value, type mismatches). Uses a real, isolated key under HKEY_CURRENT_USER (writable without
/// admin rights) since RegistryKey is a sealed BCL type that cannot be faked. The
/// parsing/precedence logic that matters is covered by PoliciesTests against a fake source instead.
/// </summary>
public class RegistryPolicySourceTests : IDisposable
{
    private readonly string _testKeyPath = @"SOFTWARE\Bravo.Tests\Policies\" + Guid.NewGuid().ToString("N");
    private readonly RegistryKey _testKey;

    public RegistryPolicySourceTests()
    {
        _testKey = Registry.CurrentUser.CreateSubKey(_testKeyPath, writable: true)!;
    }

    public void Dispose()
    {
        _testKey.Dispose();
        Registry.CurrentUser.DeleteSubKeyTree(_testKeyPath, throwOnMissingSubKey: false);
    }

    [Fact]
    public void GetInt_NullKey_ReturnsNull()
    {
        var source = new RegistryPolicySource(key: null);

        Assert.Null(source.GetInt("AnyValue"));
    }

    [Fact]
    public void GetInt_ValueNotSet_ReturnsNull()
    {
        var source = new RegistryPolicySource(_testKey);

        Assert.Null(source.GetInt("Missing"));
    }

    [Fact]
    public void GetInt_DWordValue_ReturnsInt()
    {
        _testKey.SetValue("TelemetryEnabled", 1, RegistryValueKind.DWord);
        var source = new RegistryPolicySource(_testKey);

        Assert.Equal(1, source.GetInt("TelemetryEnabled"));
    }

    [Fact]
    public void GetInt_StringValue_ReturnsNull()
    {
        _testKey.SetValue("TelemetryEnabled", "1", RegistryValueKind.String);
        var source = new RegistryPolicySource(_testKey);

        Assert.Null(source.GetInt("TelemetryEnabled"));
    }

    [Fact]
    public void GetString_NullKey_ReturnsNull()
    {
        var source = new RegistryPolicySource(key: null);

        Assert.Null(source.GetString("AnyValue"));
    }

    [Fact]
    public void GetString_StringValue_ReturnsString()
    {
        _testKey.SetValue("CustomTemplatesOrganizationRepositoryPath", @"C:\Templates\Org", RegistryValueKind.String);
        var source = new RegistryPolicySource(_testKey);

        Assert.Equal(@"C:\Templates\Org", source.GetString("CustomTemplatesOrganizationRepositoryPath"));
    }

    [Fact]
    public void GetString_DWordValue_ReturnsNull()
    {
        _testKey.SetValue("CustomTemplatesOrganizationRepositoryPath", 123, RegistryValueKind.DWord);
        var source = new RegistryPolicySource(_testKey);

        Assert.Null(source.GetString("CustomTemplatesOrganizationRepositoryPath"));
    }
}
