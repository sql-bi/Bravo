using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Bravo.Core.Policies;
using Bravo.Platform.Windows.Policies;
using Microsoft.Win32;
using Xunit;

namespace Bravo.Platform.Windows.Tests.Policies;

/// <summary>
/// The templates of <c>policies/</c>, which an administrator deploys: every policy of 1.0.x is there with its
/// key, its value and the values that it writes, and the reader reads what they write.
/// </summary>
public sealed partial class PolicyTemplateTests : IDisposable
{
    private const string TemplateKey = "Template";

    private static readonly string s_folder = Path.Combine(AppContext.BaseDirectory, "policies");

    private readonly TemporaryRegistryKey _registry = new();

    public void Dispose()
    {
        _registry.Dispose();
    }

    /// <summary>
    /// The policies of the templates of 1.0.14, with the values that they write: on and off, or the channels.
    /// </summary>
    public static TheoryData<string, int[]> PoliciesOf1014 => new()
    {
        { "TelemetryEnabled", [1, 0] },
        { "UseSystemBrowserForAuthentication", [1, 0] },
        { "UpdateChannel", [0, 2] },
        { "UpdateCheckEnabled", [1, 0] },
        { "CustomTemplatesEnabled", [1, 0] },
    };

    [Theory]
    [MemberData(nameof(PoliciesOf1014))]
    public void Template_PolicyOf1014_WritesTheValuesOf1014UnderTheKeyOf1014(string name, int[] values)
    {
        var policy = FindPolicy(name);

        Assert.Equal(RegistryPolicyReader.KeyName, (string?)policy.Attribute("key"));
        Assert.Equal(name, ReadValueName(policy));
        Assert.Equal(values, ReadWrittenValues(policy));
    }

    [Fact]
    public void Template_Policies_AreTheOnesOf1014()
    {
        var names = LoadTemplate("bravo.admx").Descendants("policy").Select(policy => (string?)policy.Attribute("name"));

        string[] expected = ["TelemetryEnabled", "UseSystemBrowserForAuthentication", "UpdateChannel", "UpdateCheckEnabled", "CustomTemplatesEnabled"];
        Assert.Equal(expected, names);
    }

    // What an administrator gets from Enabled and Disabled is what the application reads, a switch on and off.
    [Theory]
    [InlineData("TelemetryEnabled")]
    [InlineData("UseSystemBrowserForAuthentication")]
    [InlineData("UpdateCheckEnabled")]
    [InlineData("CustomTemplatesEnabled")]
    public void Read_ValuesThatTheTemplateWrites_AreTheSwitchOnAndOff(string name)
    {
        var policy = FindPolicy(name);
        var property = typeof(ApplicationPolicies).GetProperty(name)!;
        var read = new List<object?>();

        foreach (var value in ReadWrittenValues(policy))
        {
            _registry.SetValue(TemplateKey, name, value, RegistryValueKind.DWord);
            read.Add(property.GetValue(RegistryPolicyReader.Read([_registry.PolicyKey(TemplateKey)])));
        }

        Assert.Equal<object?>([true, false], read);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Read_ChannelThatTheTemplateWrites_IsNotRead(int channel)
    {
        Assert.Contains(channel, ReadWrittenValues(FindPolicy("UpdateChannel")));
        _registry.SetValue(TemplateKey, "UpdateChannel", channel, RegistryValueKind.DWord);

        Assert.Equal(ApplicationPolicies.None, RegistryPolicyReader.Read([_registry.PolicyKey(TemplateKey)]));
    }

    // A text or a presentation that the language file lacks makes the editors of Group Policy refuse the template.
    [Theory]
    [InlineData("bravo")]
    [InlineData("sqlbi")]
    public void Template_EveryTextItNames_IsInItsLanguageFile(string name)
    {
        var template = LoadTemplate($"{name}.admx");
        var resources = LoadTemplate(Path.Combine("en-US", $"{name}.adml"));

        var named = ResourceReference().Matches(template.ToString()).Select(match => match.Value).Distinct();
        var defined = resources.Descendants("string").Select(text => $"$(string.{(string?)text.Attribute("id")})")
            .Concat(resources.Descendants("presentation").Select(presentation => $"$(presentation.{(string?)presentation.Attribute("id")})"));

        Assert.Empty(named.Except(defined));
    }

    [Theory]
    [InlineData("bravo")]
    [InlineData("sqlbi")]
    public void Template_LanguageFile_HasTheRevisionThatTheTemplateRequires(string name)
    {
        var template = LoadTemplate($"{name}.admx");
        var resources = LoadTemplate(Path.Combine("en-US", $"{name}.adml"));

        var required = new Version((string)template.Descendants("resources").Single().Attribute("minRequiredRevision")!);
        var revision = new Version((string)resources.Root!.Attribute("revision")!);

        Assert.True(revision >= required, $"{name}.adml has revision {revision}, {name}.admx requires {required}.");
    }

    private static XElement FindPolicy(string name)
    {
        return LoadTemplate("bravo.admx").Descendants("policy").Single(policy => (string?)policy.Attribute("name") == name);
    }

    // A switch names its value on the policy; the channel on its list.
    private static string? ReadValueName(XElement policy)
    {
        return (string?)policy.Attribute("valueName") ?? (string?)policy.Descendants("enum").Single().Attribute("valueName");
    }

    // The decimal values that Enabled, then Disabled, or each item of the list write.
    private static int[] ReadWrittenValues(XElement policy)
    {
        return [.. policy.Descendants("decimal").Select(value => (int)value.Attribute("value")!)];
    }

    private static XDocument LoadTemplate(string relativePath)
    {
        return XDocument.Load(Path.Combine(s_folder, relativePath));
    }

    [GeneratedRegex(@"\$\((string|presentation)\.[A-Za-z0-9_]+\)")]
    private static partial Regex ResourceReference();
}
