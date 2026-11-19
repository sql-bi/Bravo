using System;
using System.IO;
using Bravo.Core.Updates;
using Bravo.Platform.Windows.Updates;
using Xunit;

namespace Bravo.Platform.Windows.Tests.Updates;

public sealed class RegistryInstallationReaderTests
{
    private const string Folder = @"C:\Program Files\Bravo\";

    [Theory]
    [InlineData(@"C:\Program Files\Bravo\")]
    [InlineData(@"C:\Program Files\Bravo")]
    [InlineData(@"c:\program files\bravo\")]
    [InlineData(@" C:\Program Files\Bravo\ ")]
    public void Classify_MachineValueThatNamesTheFolder_IsPerMachine(string machineFolder)
    {
        Assert.Equal(Installation.PerMachine, RegistryInstallationReader.Classify(machineFolder, null, Folder));
    }

    [Fact]
    public void Classify_UserValueThatNamesTheFolder_IsPerUser()
    {
        var userFolder = @"C:\Users\Someone\AppData\Local\Programs\Bravo\";

        var installation = RegistryInstallationReader.Classify(@"C:\Program Files\Bravo\", userFolder, userFolder);

        Assert.Equal(Installation.PerUser, installation);
    }

    // Both packages can be installed: the copy that runs is the one whose folder is named.
    [Fact]
    public void Classify_BothValuesNameTheFolder_IsPerMachine()
    {
        Assert.Equal(Installation.PerMachine, RegistryInstallationReader.Classify(Folder, Folder, Folder));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", " ")]
    [InlineData(@"C:\Program Files\Bravo\Other\", @"C:\Users\Someone\AppData\Local\Programs\Bravo\")]
    [InlineData("C:\\Program Files\\Bra\0vo\\", null)]
    public void Classify_NoValueThatNamesTheFolder_IsNoInstallation(string? machineFolder, string? userFolder)
    {
        Assert.Equal(Installation.None, RegistryInstallationReader.Classify(machineFolder, userFolder, Folder));
    }

    // Reads the registry of the machine and writes nothing: no package ever installs a new temporary folder.
    [Fact]
    public void Read_FolderThatNoPackageInstalled_IsNoInstallation()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var installation = new RegistryInstallationReader(folder).Read();

        Assert.Equal(Installation.None, installation);
    }
}
