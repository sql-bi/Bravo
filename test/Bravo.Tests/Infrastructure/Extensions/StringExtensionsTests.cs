namespace Bravo.Tests.Infrastructure.Extensions
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Models.FormatDax;
    using System;
    using System.Collections;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Xunit;
    using Xunit.Abstractions;

    public class StringExtensionsTests
    {
        private readonly ITestOutputHelper _output;

        public StringExtensionsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("", "", DaxLineBreakStyle.None)]
        [InlineData(null, null, DaxLineBreakStyle.None)]
        [InlineData("\n", "", DaxLineBreakStyle.InitialLineBreak)]
        [InlineData("\r\n", "", DaxLineBreakStyle.InitialLineBreak)]
        public void NormalizeDax_EmptyTest(string? expression, string? expectedExpression, DaxLineBreakStyle expectedLineBreakStyle)
        {
            var (actualExpression, actualLineBreakStyle) = expression.NormalizeDax();

            Assert.Equal(expectedExpression, actualExpression);
            Assert.Equal(expectedLineBreakStyle, actualLineBreakStyle);
        }

        [Theory]
        [InlineData("CALCULATE\r\n(\r\n[Amount]\r\n)", "CALCULATE\n(\n[Amount]\n)")]
        public void NormalizeDax_DefaultEolCharacterTest(string expression, string expectedExpression)
        {
            var (actualExpression, actualLineBreakStyle) = expression.NormalizeDax();

            Assert.Equal(expectedExpression, actualExpression);
            Assert.Equal(DaxLineBreakStyle.None, actualLineBreakStyle);
        }

        [Theory]
        [InlineData("CALCULATE([Amount])\n", "CALCULATE([Amount])")]
        [InlineData("CALCULATE([Amount])\n\n", "CALCULATE([Amount])\n\n")]
        [InlineData("CALCULATE([Amount])\n\n\n", "CALCULATE([Amount])\n\n\n")]
        [InlineData("CALCULATE([Amount]) ", "CALCULATE([Amount])")]
        [InlineData("CALCULATE([Amount])  ", "CALCULATE([Amount])  ")]
        [InlineData("CALCULATE([Amount])   ", "CALCULATE([Amount])   ")]
        [InlineData("CALCULATE([Amount])\n ", "CALCULATE([Amount])\n")]
        public void NormalizeDax_EolTrailingCharacterTest(string expression, string expectedExpression)
        {
            var (actualExpression, actualLineBreakStyle) = expression.NormalizeDax();

            Assert.Equal(expectedExpression, actualExpression);
            Assert.Equal(DaxLineBreakStyle.None, actualLineBreakStyle);
        }

        [Fact]
        public void AppendApplicationVersion_Test()
        {
            var actual = "Bravo for Power BI".AppendApplicationVersion();

            Assert.NotNull(actual);
            Assert.StartsWith("Bravo for Power BI", actual);
        }

        [Theory]
        [InlineData(null, "", 4)]
        [InlineData("", "", 4)]
        [InlineData("0.0.0.999-DEV", "0.0.0.999-DEV", 4)]
        [InlineData("0.9.4", "0.9.4", 4)]
        [InlineData("0.9.4-internal", "0.9.4-internal", 4)]
        [InlineData("0.9.4-internal-20220531.2-main-cf160fee710b983bbfb739e5eb1edd542a72b8b5 (0.9.8186.35439)", "0.9.4-internal-20220531.2-main", 4)]
        [InlineData("0.9.4-internal-20220531.2-main-cf160fee710b983bbfb739e5eb1edd542a72b8b5 (0.9.8186.35439)", "0.9.4-internal-20220531.2", 3)]
        [InlineData("0.9.4-internal-20220531.2-main-cf160fee710b983bbfb739e5eb1edd542a72b8b5 (0.9.8186.35439)", "0.9.4-internal", 2)]
        [InlineData("0.9.4-internal-20220531.2-main-cf160fee710b983bbfb739e5eb1edd542a72b8b5 (0.9.8186.35439)", "0.9.4", 1)]
        [InlineData("0.9.4-internal-20220531.2-main-cf160fee710b983bbfb739e5eb1edd542a72b8b5 (0.9.8186.35439)", "", 0)]
        [InlineData("0.9.4-internal-20220531.2-main-cf160fee710b983bbfb739e5eb1edd542a72b8b5 (0.9.8186.35439)", "", -1)]
        public void GetVersionParts_Test(string? value, string expected, int parts)
        {
            var actual = value.GetVersionParts(parts);

            Assert.NotNull(actual);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsPBIDesktopMainWindowTitle_Test()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Power BI Desktop", "bin");
            if (!Directory.Exists(path))
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Power BI Desktop", "bin");
            if (!Directory.Exists(path))
            {
                // TODO: search for windows store app
                //path = Path.Combine(path, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify), @"Microsoft\WindowsApps\PBIDesktopStore.exe");
                //if (!File.Exists(path)) return;

                return; // Skip test if the BPIDesktop folder does not exist (i.e. build agent)
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

                using var reader = new System.Resources.ResourceReader(stream!);

                var found = false;
                foreach (DictionaryEntry entry in reader)
                {
                    if (entry.Key is string key && key == "PowerBIWindowTitle" && entry.Value is string value)
                    {
                        var unicodeValue = string.Join(" ", value.EnumerateRunes().Select((rune) => $"U+{rune.Value:X4}"));
                        _output.WriteLine("\t{0} > {1}", unicodeValue, value);

                        var formattedTitle = string.Format(/*System.Globalization.CultureInfo.CurrentCulture,*/ value, "Contoso", "Power BI Desktop");
                        var isSupported = AppEnvironment.PBIDesktopMainWindowTitleSuffixes.Any(formattedTitle.EndsWith);

                        Assert.True(isSupported, $"Unsupported 'PowerBIWindowTitle' format string in resource file '{file}'");

                        found = true;
                        break;
                    }
                }

                if (!found)
                    Assert.Fail($"Resource key 'PowerBIWindowTitle' not found in file '{file}'");
            }
        }
    }
}
