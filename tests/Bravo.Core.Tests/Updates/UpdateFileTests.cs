using System;
using Bravo.Core.Updates;
using Xunit;

namespace Bravo.Core.Tests.Updates;

public sealed class UpdateFileTests
{
    private const string Folder = "https://github.com/sql-bi/Bravo/releases/download/v1.1.0/";

    private static readonly Uri s_download = new($"{Folder}Bravo.1.1.0.x64.msi");

    // The six files of a release, by the names that every installed version asks for.
    [Theory]
    [InlineData(Installation.PerMachine, true, "Bravo.1.1.0.x64.msi")]
    [InlineData(Installation.PerUser, true, "Bravo.1.1.0.x64-userinstaller.msi")]
    [InlineData(Installation.None, true, "Bravo.1.1.0.x64-portable.zip")]
    [InlineData(Installation.PerMachine, false, "Bravo.1.1.0.x64-frameworkdependent.msi")]
    [InlineData(Installation.PerUser, false, "Bravo.1.1.0.x64-frameworkdependent-userinstaller.msi")]
    [InlineData(Installation.None, false, "Bravo.1.1.0.x64-frameworkdependent-portable.zip")]
    public void GetAddress_KindOfTheCopy_NamesTheFileOfItsKindInTheFolderOfThePackage(
        Installation installation,
        bool includesRuntime,
        string fileName)
    {
        var address = UpdateFile.GetAddress(s_download, installation, includesRuntime);

        Assert.Equal(new Uri($"{Folder}{fileName}"), address);
    }

    [Fact]
    public void GetAddress_AddressWithAQuery_KeepsTheQuery()
    {
        var download = new Uri("https://downloads.bravo.test/releases/Bravo.1.1.0.x64.msi?source=feed");

        var address = UpdateFile.GetAddress(download, Installation.PerUser, includesRuntime: true);

        Assert.Equal("https://downloads.bravo.test/releases/Bravo.1.1.0.x64-userinstaller.msi?source=feed", address.AbsoluteUri);
    }
}
