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
        var files = PowerBIDesktopInstallation.GetBinFolders()
            .SelectMany((folder) => Directory.EnumerateFiles(folder, "Microsoft.PowerBI.Client.Windows.Resources.dll", SearchOption.AllDirectories))
            .ToArray();

        if (files.Length == 0)
        {
            // Skip test if Power BI Desktop is not installed (i.e. build agent)
            return;
        }

        foreach (var file in files)
        {
            _output.WriteLine("Resource file '{0}'", file);

            // From bytes: the Store package folder can be read but its files cannot be loaded as images. Each load also gets
            // its own context, so the MSI installation and the Store package can contain the same assembly identity.
            var assembly = Assembly.Load(File.ReadAllBytes(file));
            var name = assembly.GetManifestResourceNames().SingleOrDefault((name) => name.StartsWith("Microsoft.PowerBI.Client.Windows.PowerBIStringResources."));

            if (name is null)
                Assert.Fail($"Resource name not found in file '{file}'");

            using var stream = assembly.GetManifestResourceStream(name);

            if (stream is null)
                Assert.Fail($"Resource stream not found in file '{file}'");

            using var reader = new ResourceReader(stream);

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
