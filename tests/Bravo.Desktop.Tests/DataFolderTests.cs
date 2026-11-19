using System;
using System.IO;
using Bravo.Desktop.Development;
using Xunit;

namespace Bravo.Desktop.Tests;

public sealed class DataFolderTests
{
    private static readonly string s_localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    [Fact]
    public void For_ReleaseBuild_IsBravoUnderTheLocalApplicationData()
    {
        var folder = DataFolder.For(BuildIdentity.Release("1.1.0"), DevelopmentOptions.None);

        Assert.Equal(Path.Combine(s_localData, "Bravo"), folder.Path);
    }

    [Fact]
    public void For_DevelopmentBuild_IsAFolderOfItsOwnUnderTheLocalApplicationData()
    {
        var folder = DataFolder.For(BuildIdentity.Development("1.1.0"), DevelopmentOptions.None);

        Assert.Equal(Path.Combine(s_localData, "Bravo.Development"), folder.Path);
    }

    [Fact]
    public void For_DevelopmentBuildGivenAFolder_IsThatFolder()
    {
        var development = new DevelopmentOptions { DataFolderPath = @"C:\Tests\data" };

        var folder = DataFolder.For(BuildIdentity.Development("1.1.0"), development);

        Assert.Equal(@"C:\Tests\data", folder.Path);
    }
}
