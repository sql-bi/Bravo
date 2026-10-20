using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using Sqlbi.Bravo.Infrastructure.Services.PowerBI;
using Xunit;
using Xunit.Abstractions;

namespace Bravo.Tests.Infrastructure.Services.PowerBI;

public class PowerBIDesktopTests
{
    private readonly ITestOutputHelper _output;

    public PowerBIDesktopTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("Contoso \u002D Power BI Desktop")]
    [InlineData("Contoso \u2013 Power BI Desktop")]
    [InlineData("Contoso \u2014 Power BI Desktop")]
    [InlineData("Contoso\u2013Power BI Desktop")]
    [InlineData("Contoso\u00A0\u2014 Power BI Desktop")]
    public void GetReportName_TitleWithApplicationName_ReturnsTheReportName(string windowTitle)
    {
        Assert.Equal("Contoso", PowerBIDesktop.GetReportName(windowTitle));
    }

    [Fact]
    public void GetReportName_TitleWithoutApplicationName_ReturnsTheTitle()
    {
        Assert.Equal("Contoso", PowerBIDesktop.GetReportName("Contoso"));
    }

    [Fact]
    public void GetReportName_ReportNameEndingWithApplicationName_RemovesOnlyTheLastOne()
    {
        var reportName = PowerBIDesktop.GetReportName("Contoso - Power BI Desktop - Power BI Desktop");

        Assert.Equal("Contoso - Power BI Desktop", reportName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetReportName_EmptyTitle_ReturnsNull(string windowTitle)
    {
        Assert.Null(PowerBIDesktop.GetReportName(windowTitle));
    }

    [Fact]
    public void GetReportName_InstalledTitleFormats_ReturnsTheReportName()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Power BI Desktop", "bin");

        if (!Directory.Exists(path))
        {
            // Skip test if the BPIDesktop folder does not exist (i.e. build agent)
            return;
        }

        foreach (var file in Directory.EnumerateFiles(path, "Microsoft.PowerBI.Client.Windows.Resources.dll", SearchOption.AllDirectories))
        {
            _output.WriteLine("Resource file '{0}'", file);

            var assembly = Assembly.LoadFrom(file);
            var name = assembly.GetManifestResourceNames().SingleOrDefault((name) => name.StartsWith($"Microsoft.PowerBI.Client.Windows.PowerBIStringResources."));

            if (name is null)
                Assert.Fail($"Resource name not found in file '{file}'");

            using var stream = assembly.GetManifestResourceStream(name);

            if (name is null)
                Assert.Fail($"Resource stream not found in file '{file}'");

            using var reader = new ResourceReader(stream!);

            var found = false;
            foreach (DictionaryEntry entry in reader)
            {
                if (entry.Key is string key && key == "PowerBIWindowTitle" && entry.Value is string value)
                {
                    var unicodeValue = string.Join(" ", value.EnumerateRunes().Select((rune) => $"U+{rune.Value:X4}"));
                    _output.WriteLine("\t{0} > {1}", unicodeValue, value);

                    var formattedTitle = string.Format(/*System.Globalization.CultureInfo.CurrentCulture,*/ value, "Contoso", "Power BI Desktop");

                    var reportName = PowerBIDesktop.GetReportName(formattedTitle);

                    Assert.True(reportName == "Contoso", $"Unsupported 'PowerBIWindowTitle' format string in resource file '{file}'");

                    found = true;
                    break;
                }
            }

            if (!found)
                Assert.Fail($"Resource key 'PowerBIWindowTitle' not found in file '{file}'");
        }
    }
}
