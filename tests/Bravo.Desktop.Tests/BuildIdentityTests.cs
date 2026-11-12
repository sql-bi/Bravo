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
    public void Release_WindowTitle_IsTheNameOfTheProductAndTheVersion()
    {
        var identity = BuildIdentity.Release("1.1.0-beta.1");

        Assert.Equal("Bravo for Power BI - v1.1.0-beta.1", identity.WindowTitle);
    }

    [Fact]
    public void Development_WindowTitle_SaysThatTheBuildIsADevelopmentBuild()
    {
        var identity = BuildIdentity.Development("1.1.0");

        Assert.Equal("Bravo for Power BI - v1.1.0 [development build]", identity.WindowTitle);
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
    [InlineData("1.1.0", "1.1.0")]
    [InlineData("1.1.0-beta.1", "1.1.0-beta.1")]
    [InlineData("1.1.0-beta.1+7a173c94d1", "1.1.0-beta.1")]
    public void FromAssembly_InformationalVersion_IsTheVersionWithoutTheMetadataOfTheBuild(
        string informationalVersion,
        string expected)
    {
        var assembly = DefineAssembly(informationalVersion, releaseBuildMark: null);

        var identity = BuildIdentity.FromAssembly(assembly);

        Assert.Equal(expected, identity.Version);
    }

    // An assembly in memory with the attributes that a build writes: the result does not depend on how the
    // assembly of the application was built for this run.
    private static AssemblyBuilder DefineAssembly(string informationalVersion, string? releaseBuildMark)
    {
        var name = new AssemblyName("Bravo.Tests.BuildIdentity");
        var assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);

        var versionConstructor = typeof(AssemblyInformationalVersionAttribute).GetConstructor([typeof(string)])!;
        assembly.SetCustomAttribute(new CustomAttributeBuilder(versionConstructor, [informationalVersion]));

        if (releaseBuildMark is not null)
        {
            var metadataConstructor = typeof(AssemblyMetadataAttribute).GetConstructor([typeof(string), typeof(string)])!;
            object[] arguments = [BuildIdentity.ReleaseBuildMetadataName, releaseBuildMark];
            assembly.SetCustomAttribute(new CustomAttributeBuilder(metadataConstructor, arguments));
        }

        return assembly;
    }
}
