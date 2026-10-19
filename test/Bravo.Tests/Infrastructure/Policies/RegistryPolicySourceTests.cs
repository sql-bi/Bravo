using System;
using Microsoft.Win32;
using Sqlbi.Bravo.Infrastructure.Policies;
using Xunit;

namespace Bravo.Tests.Infrastructure.Policies;

/// <summary>
/// Verifies registry value types and handle ownership with an isolated key under HKEY_CURRENT_USER.
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
        using var source = new RegistryPolicySource(key: null);

        Assert.Null(source.GetInt("AnyValue"));
    }

    [Fact]
    public void GetInt_ValueNotSet_ReturnsNull()
    {
        using var source = new RegistryPolicySource(_testKey);

        Assert.Null(source.GetInt("Missing"));
    }

    [Fact]
    public void GetInt_DWordValue_ReturnsInt()
    {
        _testKey.SetValue("TelemetryEnabled", 1, RegistryValueKind.DWord);
        using var source = new RegistryPolicySource(_testKey);

        Assert.Equal(1, source.GetInt("TelemetryEnabled"));
    }

    [Fact]
    public void GetInt_StringValue_ReturnsNull()
    {
        _testKey.SetValue("TelemetryEnabled", "1", RegistryValueKind.String);
        using var source = new RegistryPolicySource(_testKey);

        Assert.Null(source.GetInt("TelemetryEnabled"));
    }

    [Fact]
    public void GetString_NullKey_ReturnsNull()
    {
        using var source = new RegistryPolicySource(key: null);

        Assert.Null(source.GetString("AnyValue"));
    }

    [Fact]
    public void GetString_StringValue_ReturnsString()
    {
        _testKey.SetValue("CustomTemplatesOrganizationRepositoryPath", @"C:\Templates\Org", RegistryValueKind.String);
        using var source = new RegistryPolicySource(_testKey);

        Assert.Equal(@"C:\Templates\Org", source.GetString("CustomTemplatesOrganizationRepositoryPath"));
    }

    [Fact]
    public void GetString_DWordValue_ReturnsNull()
    {
        _testKey.SetValue("CustomTemplatesOrganizationRepositoryPath", 123, RegistryValueKind.DWord);
        using var source = new RegistryPolicySource(_testKey);

        Assert.Null(source.GetString("CustomTemplatesOrganizationRepositoryPath"));
    }

    [Fact]
    public void Dispose_ClosesTheOwnedRegistryKey()
    {
        var source = new RegistryPolicySource(_testKey);

        source.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _testKey.GetValue("AnyValue"));
    }
}
