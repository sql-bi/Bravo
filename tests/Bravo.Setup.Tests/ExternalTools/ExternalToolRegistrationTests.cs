using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Bravo.Setup.ExternalTools;
using Xunit;

namespace Bravo.Setup.Tests.ExternalTools;

public sealed class ExternalToolRegistrationTests : IDisposable
{
    private const string Executable = @"C:\Program Files\SQLBI\Bravo\Bravo.exe";

    private readonly TemporaryFolder _folder = new();
    private readonly ExternalToolRegistration _registration = new();

    public void Dispose()
    {
        _folder.Dispose();
    }

    [Fact]
    public void Register_WritesTheToolAsPowerBIDesktopReadsIt()
    {
        var tool = RegisterAndRead(_folder.Combine("bravo.pbitool.json"));

        Assert.Equal("Bravo", tool.GetProperty("name").GetString());
        Assert.Equal(Executable, tool.GetProperty("path").GetString());
        Assert.Equal("--server=\"%server%\" --database=\"%database%\"", tool.GetProperty("arguments").GetString());
    }

    [Fact]
    public void Register_WritesADescriptionAndTheVersionOfTheFormat()
    {
        var tool = RegisterAndRead(_folder.Combine("bravo.pbitool.json"));

        Assert.Equal("1.0", tool.GetProperty("version").GetString());
        Assert.Equal("Bravo for Power BI", tool.GetProperty("description").GetString());
    }

    // The image that 1.0.x registers, under the media type that it registers it with.
    [Fact]
    public void Register_WritesTheIconAsADataUriWithoutItsPrefix()
    {
        var tool = RegisterAndRead(_folder.Combine("bravo.pbitool.json"));

        var iconData = tool.GetProperty("iconData").GetString()!;
        Assert.StartsWith("image/png;base64,", iconData, StringComparison.Ordinal);
        Assert.Equal(51262, Convert.FromBase64String(iconData["image/png;base64,".Length..]).Length);
    }

    // The file is read by people too: quotes and backslashes are escaped with a backslash, a letter of any
    // alphabet is written as it is, and the file starts with no byte order mark.
    [Fact]
    public void Register_AnExecutableWithLettersOfAnotherAlphabet_WritesThemAsTheyAre()
    {
        var toolFile = _folder.Combine("bravo.pbitool.json");

        _registration.Register(toolFile, @"D:\Programmi\Società\Bravo.exe");

        var text = File.ReadAllText(toolFile, Encoding.UTF8);
        Assert.Contains(@"""path"": ""D:\\Programmi\\Società\\Bravo.exe""", text, StringComparison.Ordinal);
        Assert.Contains(@"""arguments"": ""--server=\""%server%\"" --database=\""%database%\""""", text, StringComparison.Ordinal);
        Assert.Equal((byte)'{', File.ReadAllBytes(toolFile)[0]);
    }

    [Fact]
    public void Register_AFolderThatDoesNotExist_CreatesIt()
    {
        var toolFile = _folder.Combine("Microsoft Shared", "Power BI Desktop", "External Tools", "bravo.pbitool.json");

        _registration.Register(toolFile, Executable);

        Assert.True(File.Exists(toolFile));
    }

    [Fact]
    public void Register_AFileThatIsAlreadyThere_ReplacesItAndLeavesNoOtherFile()
    {
        var toolFile = _folder.Combine("bravo.pbitool.json");
        File.WriteAllText(toolFile, """{ "name": "Bravo", "path": "C:\\Old\\Bravo.exe" }""");

        var tool = RegisterAndRead(toolFile);

        Assert.Equal(Executable, tool.GetProperty("path").GetString());
        Assert.Equal([toolFile], Directory.GetFiles(_folder.Path));
    }

    private JsonElement RegisterAndRead(string toolFile)
    {
        _registration.Register(toolFile, Executable);

        using var tool = JsonDocument.Parse(File.ReadAllBytes(toolFile));
        return tool.RootElement.Clone();
    }

    [Fact]
    public void Unregister_RemovesTheFileAndLeavesTheFolderWithTheToolsOfOthers()
    {
        var toolFile = _folder.Combine("bravo.pbitool.json");
        var otherToolFile = _folder.Combine("other.pbitool.json");
        _registration.Register(toolFile, Executable);
        File.WriteAllText(otherToolFile, "{}");

        _registration.Unregister(toolFile);

        Assert.Equal([otherToolFile], Directory.GetFiles(_folder.Path));
    }

    [Theory]
    [InlineData("bravo.pbitool.json")]
    [InlineData(@"No such folder\bravo.pbitool.json")]
    public void Unregister_AFileThatIsNotThere_DoesNothing(string relativePath)
    {
        var toolFile = _folder.Combine(relativePath);

        _registration.Unregister(toolFile);

        Assert.Empty(Directory.GetFileSystemEntries(_folder.Path));
    }
}
