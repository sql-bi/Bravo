using System.Collections.Generic;
using Bravo.Architecture.Tests.ProjectReferences;
using Xunit;

namespace Bravo.Architecture.Tests.Solution;

public sealed class SolutionMembershipRuleTests
{
    private const string Core = "src/Bravo.Core/Bravo.Core.csproj";
    private const string Shell = "src/Bravo.Shell/Bravo.Shell.csproj";
    private const string CoreTests = "tests/Bravo.Core.Tests/Bravo.Core.Tests.csproj";

    [Fact]
    public void FindViolations_ProjectInNoFile_ReportsTheSolutionAndTheFilter()
    {
        var projects = new Dictionary<string, ProjectFile> { [CoreTests] = Project("net10.0") };

        var violations = SolutionMembershipRule.FindViolations(projects, [], []);

        string[] expected =
        [
            $"{CoreTests}: the project is not listed in Bravo.slnx",
            $"{CoreTests}: the project does not target Windows and is not listed in Bravo.Portable.slnf",
        ];
        Assert.Equal(expected, violations);
    }

    [Fact]
    public void FindViolations_PortableProjectMissingFromTheFilter_ReportsTheFilter()
    {
        var projects = new Dictionary<string, ProjectFile> { [Core] = Project("net10.0") };

        var violations = SolutionMembershipRule.FindViolations(projects, [Core], []);

        Assert.Equal([$"{Core}: the project does not target Windows and is not listed in Bravo.Portable.slnf"], violations);
    }

    [Fact]
    public void FindViolations_WindowsProjectInTheFilter_ReportsTheFilter()
    {
        var projects = new Dictionary<string, ProjectFile> { [Shell] = Project("net10.0-windows") };

        var violations = SolutionMembershipRule.FindViolations(projects, [Shell], [Shell]);

        Assert.Equal([$"{Shell}: the project targets Windows and is listed in Bravo.Portable.slnf"], violations);
    }

    [Fact]
    public void FindViolations_PathInAnotherCase_ReportsTheProject()
    {
        var projects = new Dictionary<string, ProjectFile> { [Shell] = Project("net10.0-windows") };

        var violations = SolutionMembershipRule.FindViolations(projects, [Shell.ToUpperInvariant()], []);

        Assert.Equal([$"{Shell}: the project is not listed in Bravo.slnx"], violations);
    }

    [Fact]
    public void FindViolations_ProjectsListedWhereTheyBelong_ReturnsNone()
    {
        var projects = new Dictionary<string, ProjectFile>
        {
            [Core] = Project("net10.0"),
            [Shell] = Project("net10.0-windows"),
            [CoreTests] = Project("net10.0"),
        };

        var violations = SolutionMembershipRule.FindViolations(projects, [Core, Shell, CoreTests], [Core, CoreTests]);

        Assert.Empty(violations);
    }

    [Fact]
    public void FindViolations_ProjectsOfTheRepository_ReturnsNone()
    {
        var violations = SolutionMembershipRule.FindViolations(
            RepositoryLayout.LoadProjects(),
            RepositoryLayout.LoadSolutionProjects(),
            RepositoryLayout.LoadFilterProjects());

        RuleAssert.NoViolations(violations);
    }

    private static ProjectFile Project(string targetFramework)
    {
        var xml = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{targetFramework}</TargetFramework>
              </PropertyGroup>
            </Project>
            """;
        return ProjectFile.Parse("Bravo.Sample", xml);
    }
}
