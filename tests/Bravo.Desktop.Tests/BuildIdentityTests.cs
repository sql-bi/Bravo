using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace Bravo.Desktop.Tests;

public sealed class BuildIdentityTests
{
    // The identifier that the instances of 1.1.0 already use: changing it makes two releases two instances.
    private const string ReleaseApplicationId = "Bravo.BEA7871EE59F4086A72C06950412689B";

    [Fact]
    public void Release_Identity_KeepsTheIdentifierAndTheDataFolderOfTheInstalledApplication()
    {
        var identity = BuildIdentity.Release("1.1.0");

        Assert.False(identity.IsDevelopmentBuild);
        Assert.Equal(ReleaseApplicationId, identity.ApplicationId);
        Assert.Equal("Bravo", identity.DataFolderName);
    }

    [Fact]
    public void Development_Identity_HasAnIdentifierAndADataFolderOfItsOwn()
    {
        var release = BuildIdentity.Release("1.1.0");

        var development = BuildIdentity.Development("1.1.0");

        Assert.True(development.IsDevelopmentBuild);
        Assert.NotEqual(release.ApplicationId, development.ApplicationId);
        Assert.NotEqual(release.DataFolderName, development.DataFolderName);
    }

    [Fact]
    public void Release_GetWindowTitle_IsTheNameOfTheProductAndTheVersion()
    {
        var identity = BuildIdentity.Release("1.1.0-beta.1");

        Assert.Equal("Bravo for Power BI - v1.1.0-beta.1", identity.GetWindowTitle(versionHidden: false));
    }

    // The title of 1.0.x for a user who hid the version.
    [Fact]
    public void Release_GetWindowTitleWithTheVersionHidden_IsTheNameOfTheProduct()
    {
        var identity = BuildIdentity.Release("1.1.0");

        Assert.Equal("Bravo for Power BI", identity.GetWindowTitle(versionHidden: true));
    }

    [Fact]
    public void Development_GetWindowTitle_SaysThatTheBuildIsADevelopmentBuild()
    {
        var identity = BuildIdentity.Development("1.1.0");

        Assert.Equal("Bravo for Power BI - v1.1.0 [development build]", identity.GetWindowTitle(versionHidden: false));
    }

    [Fact]
    public void Development_GetWindowTitleWithTheVersionHidden_StillSaysThatTheBuildIsADevelopmentBuild()
    {
        var identity = BuildIdentity.Development("1.1.0");

        Assert.Equal("Bravo for Power BI [development build]", identity.GetWindowTitle(versionHidden: true));
    }

    [Fact]
    public void FromAssembly_AssemblyThatTheReleasePipelineMarked_IsAReleaseBuild()
    {
        var assembly = DefineAssembly("1.1.0", releaseBuildMark: "true");

        var identity = BuildIdentity.FromAssembly(assembly);

        Assert.False(identity.IsDevelopmentBuild);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    [InlineData("")]
    public void FromAssembly_AssemblyWithoutTheMarkOfTheReleasePipeline_IsADevelopmentBuild(string? releaseBuildMark)
    {
        var assembly = DefineAssembly("1.1.0", releaseBuildMark);

        var identity = BuildIdentity.FromAssembly(assembly);

        Assert.True(identity.IsDevelopmentBuild);
    }

    [Theory]
    [InlineData("1.1.0")]
    [InlineData("1.1.0-beta.1")]
    [InlineData("1.1.0-beta.1.g7a173c94d1")]
    public void FromAssembly_VersionThatTheBuildWrote_IsTheVersion(string version)
    {
        var assembly = DefineAssembly(version, releaseBuildMark: null);

        var identity = BuildIdentity.FromAssembly(assembly);

        Assert.Equal(version, identity.Version);
    }

    [Fact]
    public void FromAssembly_AssemblyWithoutAVersion_HasTheVersionZero()
    {
        var assembly = DefineAssembly(version: null, releaseBuildMark: null);

        var identity = BuildIdentity.FromAssembly(assembly);

        Assert.Equal("0.0.0", identity.Version);
    }

    // The informational version of a build has four numbers: the version that a user reads has three.
    [Fact]
    public void FromAssembly_AssemblyOfTheApplication_HasTheSemanticVersionOfTheBuild()
    {
        var identity = BuildIdentity.FromAssembly(typeof(BuildIdentity).Assembly);

        Assert.Matches(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$", identity.Version);
    }

    [Fact]
    public void FromAssembly_FileVersionThatTheBuildWrote_IsTheFileVersion()
    {
        var assembly = DefineAssembly("1.1.0", releaseBuildMark: null);
        SetFileVersion(assembly, "1.1.0.188");

        var identity = BuildIdentity.FromAssembly(assembly);

        Assert.Equal(new Version(1, 1, 0, 188), identity.FileVersion);
    }

    [Fact]
    public void FromAssembly_AssemblyWithoutAFileVersion_HasTheFileVersionZero()
    {
        var assembly = DefineAssembly("1.1.0", releaseBuildMark: null);

        var identity = BuildIdentity.FromAssembly(assembly);

        Assert.Equal(new Version(0, 0, 0, 0), identity.FileVersion);
    }

    // The update feed is compared on four numbers: the fourth orders a preview before its release.
    [Fact]
    public void FromAssembly_AssemblyOfTheApplication_HasAFileVersionOfFourNumbers()
    {
        var identity = BuildIdentity.FromAssembly(typeof(BuildIdentity).Assembly);

        Assert.True(identity.FileVersion.Revision >= 0);
        Assert.NotEqual(new Version(0, 0, 0, 0), identity.FileVersion);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    [InlineData(null, false)]
    public void FromAssembly_MarkOfASelfContainedBuild_TellsWhetherTheBuildCarriesTheRuntime(
        string? selfContainedMark,
        bool includesRuntime)
    {
        var assembly = DefineAssembly("1.1.0", releaseBuildMark: "true");
        if (selfContainedMark is not null)
        {
            SetMetadata(assembly, BuildIdentity.SelfContainedMetadataName, selfContainedMark);
        }

        var identity = BuildIdentity.FromAssembly(assembly);

        Assert.Equal(includesRuntime, identity.IncludesRuntime);
    }

    // An assembly in memory with the metadata that a build writes: the result does not depend on how the
    // assembly of the application was built for this run.
    private static AssemblyBuilder DefineAssembly(string? version, string? releaseBuildMark)
    {
        var name = new AssemblyName("Bravo.Tests.BuildIdentity");
        var assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);

        if (version is not null)
        {
            SetMetadata(assembly, BuildIdentity.VersionMetadataName, version);
        }

        if (releaseBuildMark is not null)
        {
            SetMetadata(assembly, BuildIdentity.ReleaseBuildMetadataName, releaseBuildMark);
        }

        return assembly;
    }

    private static void SetMetadata(AssemblyBuilder assembly, string name, string value)
    {
        var metadataConstructor = typeof(AssemblyMetadataAttribute).GetConstructor([typeof(string), typeof(string)])!;
        object[] arguments = [name, value];
        assembly.SetCustomAttribute(new CustomAttributeBuilder(metadataConstructor, arguments));
    }

    private static void SetFileVersion(AssemblyBuilder assembly, string fileVersion)
    {
        var fileVersionConstructor = typeof(AssemblyFileVersionAttribute).GetConstructor([typeof(string)])!;
        object[] arguments = [fileVersion];
        assembly.SetCustomAttribute(new CustomAttributeBuilder(fileVersionConstructor, arguments));
    }
}
