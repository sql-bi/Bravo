using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Bravo.Architecture.Tests.ProjectReferences;

/// <summary>
/// The dependencies between the projects are decided in one table: a project references only the projects the
/// table allows, and a project that the table does not know is a violation.
/// </summary>
internal static class ProjectReferenceRule
{
    public static IEnumerable<string> FindViolations(
        IReadOnlyCollection<ProjectFile> projects,
        IReadOnlyDictionary<string, ImmutableArray<string>> allowedReferences)
    {
        foreach (var project in projects)
        {
            if (!allowedReferences.TryGetValue(project.Name, out var allowed))
            {
                yield return $"{project.Name}: the project is not in the table of allowed references";
                continue;
            }

            foreach (var reference in project.References.Except(allowed, StringComparer.Ordinal))
            {
                yield return $"{project.Name}: the reference to {reference} is not allowed";
            }
        }

        var projectNames = projects.Select(project => project.Name);
        foreach (var name in allowedReferences.Keys.Except(projectNames, StringComparer.Ordinal))
        {
            yield return $"{name}: the table of allowed references names a project that does not exist";
        }
    }
}
