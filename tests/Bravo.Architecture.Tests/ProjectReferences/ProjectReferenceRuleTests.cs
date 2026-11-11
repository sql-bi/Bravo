using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Bravo.Architecture.Tests.ProjectReferences;

public sealed class ProjectReferenceRuleTests
{
    [Fact]
    public void FindViolations_ProjectMissingFromTheTable_ReportsTheProject()
    {
        var allowed = new Dictionary<string, ImmutableArray<string>>();

        var violations = ProjectReferenceRule.FindViolations([Project("Bravo.Core")], allowed);

        Assert.Equal(["Bravo.Core: the project is not in the table of allowed references"], violations);
    }

    [Fact]
    public void FindViolations_ReferenceOutsideTheAllowedOnes_ReportsTheReference()
    {
        var allowed = new Dictionary<string, ImmutableArray<string>>
        {
            ["Bravo.Core"] = [],
            ["Bravo.Shell"] = [],
        };
        ProjectFile[] projects = [Project("Bravo.Core", "Bravo.Shell"), Project("Bravo.Shell")];

        var violations = ProjectReferenceRule.FindViolations(projects, allowed);

        Assert.Equal(["Bravo.Core: the reference to Bravo.Shell is not allowed"], violations);
    }

    [Fact]
    public void FindViolations_ReferencesWithinTheAllowedOnes_ReturnsNone()
    {
        var allowed = new Dictionary<string, ImmutableArray<string>>
        {
            ["Bravo.Core"] = [],
            ["Bravo.Shell"] = ["Bravo.Core"],
        };
        ProjectFile[] projects = [Project("Bravo.Core"), Project("Bravo.Shell", "Bravo.Core")];

        var violations = ProjectReferenceRule.FindViolations(projects, allowed);

        Assert.Empty(violations);
    }

    [Fact]
    public void FindViolations_TableEntryWithoutProject_ReportsTheEntry()
    {
        var allowed = new Dictionary<string, ImmutableArray<string>>
        {
            ["Bravo.Core"] = [],
            ["Bravo.Removed"] = [],
        };

        var violations = ProjectReferenceRule.FindViolations([Project("Bravo.Core")], allowed);

        Assert.Equal(["Bravo.Removed: the table of allowed references names a project that does not exist"], violations);
    }

    [Fact]
    public void FindViolations_ProjectsOfTheRepository_ReturnsNone()
    {
        var projects = RepositoryLayout.LoadSourceProjects();

        var violations = ProjectReferenceRule.FindViolations(projects, ProductionProjects.AllowedReferences);

        RuleAssert.NoViolations(violations);
    }

    private static ProjectFile Project(string name, params string[] references)
    {
        var items = references.Select(
            reference => $"""<ProjectReference Include="..\{reference}\{reference}.csproj" />""");
        var xml = $"""<Project Sdk="Microsoft.NET.Sdk"><ItemGroup>{string.Concat(items)}</ItemGroup></Project>""";
        return ProjectFile.Parse(name, xml);
    }
}
