using Xunit;

namespace Bravo.Architecture.Tests.ProjectReferences;

public sealed class ProjectFileTests
{
    [Fact]
    public void Parse_ProjectReferences_ReturnsTheNamesOfTheReferencedProjects()
    {
        var xml = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <ProjectReference Include="..\Bravo.First\Bravo.First.csproj" />
                <ProjectReference Include="../Bravo.Second/Bravo.Second.csproj;../Bravo.Third/Bravo.Third.csproj" />
              </ItemGroup>
            </Project>
            """;

        var project = ProjectFile.Parse("Bravo.Sample", xml);

        Assert.Equal(["Bravo.First", "Bravo.Second", "Bravo.Third"], project.References);
    }

    [Fact]
    public void Parse_NoProjectReference_ReturnsNoReference()
    {
        var xml = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Some.Package" />
              </ItemGroup>
            </Project>
            """;

        var project = ProjectFile.Parse("Bravo.Sample", xml);

        Assert.Empty(project.References);
    }

    [Theory]
    [InlineData("net10.0", false)]
    [InlineData("net10.0-windows", true)]
    [InlineData("net10.0-windows10.0.19041.0", true)]
    public void Parse_TargetFramework_TellsWhetherTheProjectTargetsWindows(string targetFramework, bool expected)
    {
        var xml = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{targetFramework}</TargetFramework>
              </PropertyGroup>
            </Project>
            """;

        var project = ProjectFile.Parse("Bravo.Sample", xml);

        Assert.Equal(expected, project.TargetsWindows);
    }
}
