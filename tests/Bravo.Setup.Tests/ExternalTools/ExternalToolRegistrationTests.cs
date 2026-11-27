using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using Bravo.Setup.ExternalTools;
using Xunit;

namespace Bravo.Setup.Tests.ExternalTools
{
    public sealed class ExternalToolRegistrationTests : IDisposable
    {
        private const string Executable = @"C:\Program Files\SQLBI\Bravo\Bravo.exe";
        private const string IconDataPrefix = "image/png;base64,";

        private readonly TemporaryFolder _folder = new TemporaryFolder();

        public void Dispose()
        {
            _folder.Dispose();
        }

        [Fact]
        public void Register_WritesTheToolAsPowerBIDesktopReadsIt()
        {
            var tool = RegisterAndRead(_folder.Combine("bravo.pbitool.json"));

            Assert.Equal("Bravo", JsonTree.ReadString(tool, "name"));
            Assert.Equal(Executable, JsonTree.ReadString(tool, "path"));
            Assert.Equal("--server=\"%server%\" --database=\"%database%\"", JsonTree.ReadString(tool, "arguments"));
        }

        [Fact]
        public void Register_WritesADescriptionAndTheVersionOfTheFormat()
        {
            var tool = RegisterAndRead(_folder.Combine("bravo.pbitool.json"));

            Assert.Equal("1.0", JsonTree.ReadString(tool, "version"));
            Assert.Equal("Bravo for Power BI", JsonTree.ReadString(tool, "description"));
        }

        // The image that 1.0.x registers, under the media type that it registers it with.
        [Fact]
        public void Register_WritesTheIconAsADataUriWithoutItsPrefix()
        {
            var tool = RegisterAndRead(_folder.Combine("bravo.pbitool.json"));

            var iconData = JsonTree.ReadString(tool, "iconData");
            Assert.StartsWith(IconDataPrefix, iconData, StringComparison.Ordinal);
            Assert.Equal(51262, Convert.FromBase64String(iconData.Substring(IconDataPrefix.Length)).Length);
        }

        // The file is read by people too: quotes and backslashes are escaped with a backslash, a letter of any
        // alphabet is written as it is, and the file starts with no byte order mark.
        [Fact]
        public void Register_AnExecutableWithLettersOfAnotherAlphabet_WritesThemAsTheyAre()
        {
            var toolFile = _folder.Combine("bravo.pbitool.json");

            ExternalToolRegistration.Register(toolFile, @"D:\Programmi\Società\Bravo.exe");

            var text = File.ReadAllText(toolFile, Encoding.UTF8);
            Assert.Contains(@"""path"": ""D:\\Programmi\\Società\\Bravo.exe""", text, StringComparison.Ordinal);
            Assert.Contains(@"""arguments"": ""--server=\""%server%\"" --database=\""%database%\""""", text, StringComparison.Ordinal);
            Assert.Equal((byte)'{', File.ReadAllBytes(toolFile)[0]);
        }

        [Fact]
        public void Register_AFolderThatDoesNotExist_CreatesIt()
        {
            var toolFile = _folder.Combine("Microsoft Shared", "Power BI Desktop", "External Tools", "bravo.pbitool.json");

            ExternalToolRegistration.Register(toolFile, Executable);

            Assert.True(File.Exists(toolFile));
        }

        [Fact]
        public void Register_AFileThatIsAlreadyThere_ReplacesItAndLeavesNoOtherFile()
        {
            var toolFile = _folder.Combine("bravo.pbitool.json");
            File.WriteAllText(toolFile, @"{ ""name"": ""Bravo"", ""path"": ""C:\\Old\\Bravo.exe"" }");

            var tool = RegisterAndRead(toolFile);

            Assert.Equal(Executable, JsonTree.ReadString(tool, "path"));
            Assert.Equal(new[] { toolFile }, Directory.GetFiles(_folder.Path));
        }

        // A relative path would name a file of the current folder, which Windows Installer chooses.
        [Theory]
        [InlineData("bravo.pbitool.json", Executable)]
        [InlineData(@"C:\External Tools\bravo.pbitool.json", "Bravo.exe")]
        public void Register_APathThatIsNotFull_Throws(string toolFile, string executable)
        {
            Assert.Throws<ArgumentException>(() => ExternalToolRegistration.Register(toolFile, executable));
        }

        private static XElement Read(string toolFile)
        {
            return JsonTree.Parse(File.ReadAllBytes(toolFile));
        }

        private static XElement RegisterAndRead(string toolFile)
        {
            ExternalToolRegistration.Register(toolFile, Executable);

            return Read(toolFile);
        }

        [Fact]
        public void Unregister_TheRegistrationOfTheExecutable_RemovesTheFileAndLeavesTheToolsOfOthers()
        {
            var toolFile = _folder.Combine("bravo.pbitool.json");
            var otherToolFile = _folder.Combine("other.pbitool.json");
            ExternalToolRegistration.Register(toolFile, Executable);
            File.WriteAllText(otherToolFile, "{}");

            ExternalToolRegistration.Unregister(toolFile, Executable);

            Assert.Equal(new[] { otherToolFile }, Directory.GetFiles(_folder.Path));
        }

        // Windows names one file in many ways: the case of the letters and a doubled separator do not matter.
        [Theory]
        [InlineData(@"c:\program files\sqlbi\bravo\BRAVO.EXE")]
        [InlineData(@"C:\Program Files\SQLBI\\Bravo\Bravo.exe")]
        [InlineData(@"C:\Program Files\SQLBI\Bravo\.\Bravo.exe")]
        public void Unregister_TheExecutableWrittenInAnotherWay_RemovesTheFile(string executable)
        {
            var toolFile = _folder.Combine("bravo.pbitool.json");
            ExternalToolRegistration.Register(toolFile, Executable);

            ExternalToolRegistration.Unregister(toolFile, executable);

            Assert.False(File.Exists(toolFile));
        }

        // The registration of another installation, such as one made by hand for an installation per user.
        [Fact]
        public void Unregister_ARegistrationOfAnotherExecutable_LeavesTheFile()
        {
            var toolFile = _folder.Combine("bravo.pbitool.json");
            ExternalToolRegistration.Register(toolFile, @"C:\Users\someone\AppData\Local\Programs\SQLBI\Bravo\Bravo.exe");
            var registered = File.ReadAllBytes(toolFile);

            ExternalToolRegistration.Unregister(toolFile, Executable);

            Assert.Equal(registered, File.ReadAllBytes(toolFile));
        }

        // The last ones name no file that .NET Framework can read.
        [Theory]
        [InlineData("not json")]
        [InlineData("")]
        [InlineData("[]")]
        [InlineData("{}")]
        [InlineData(@"{ ""path"": 1 }")]
        [InlineData(@"{ ""path"": """" }")]
        [InlineData(@"{ ""path"": ""Bravo.exe"" }")]
        [InlineData(@"{ ""path"": ""<#!PATH!#>"" }")]
        [InlineData(@"{ ""path"": ""C:\\Program Files\\SQLBI\\Bravo\\Bravo<1>.exe"" }")]
        [InlineData(@"{ ""path"": ""C:\\Program Files\\SQLBI\\Bravo\\Bravo.exe:stream"" }")]
        public void Unregister_AFileThatIsNotARegistrationOfTheExecutable_LeavesTheFile(string content)
        {
            var toolFile = _folder.Combine("bravo.pbitool.json");
            File.WriteAllText(toolFile, content);

            ExternalToolRegistration.Unregister(toolFile, Executable);

            Assert.Equal(content, File.ReadAllText(toolFile));
        }

        [Theory]
        [InlineData("bravo.pbitool.json")]
        [InlineData(@"No such folder\bravo.pbitool.json")]
        public void Unregister_AFileThatIsNotThere_DoesNothing(string relativePath)
        {
            var toolFile = _folder.Combine(relativePath);

            ExternalToolRegistration.Unregister(toolFile, Executable);

            Assert.Empty(Directory.GetFileSystemEntries(_folder.Path));
        }
    }
}
