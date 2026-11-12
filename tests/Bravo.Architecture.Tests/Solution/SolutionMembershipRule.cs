using System;
using System.Collections.Generic;
using System.Linq;
using Bravo.Architecture.Tests.ProjectReferences;

namespace Bravo.Architecture.Tests.Solution;

/// <summary>
/// A project that no solution lists is never built or tested, and nothing fails. Every project is in the
/// solution; the filter that is built on Linux lists the projects that do not target Windows, and only those.
/// </summary>
internal static class SolutionMembershipRule
{
    public static IEnumerable<string> FindViolations(
        IReadOnlyDictionary<string, ProjectFile> projects,
        IReadOnlyCollection<string> solutionProjects,
        IReadOnlyCollection<string> filterProjects)
    {
        foreach (var (path, project) in projects.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            if (!solutionProjects.Contains(path, StringComparer.Ordinal))
            {
                yield return $"{path}: the project is not listed in {RepositoryLayout.SolutionFileName}";
            }

            var isInFilter = filterProjects.Contains(path, StringComparer.Ordinal);
            if (project.TargetsWindows && isInFilter)
            {
                yield return $"{path}: the project targets Windows and is listed in {RepositoryLayout.FilterFileName}";
            }

            if (!project.TargetsWindows && !isInFilter)
            {
                yield return $"{path}: the project does not target Windows and is not listed in {RepositoryLayout.FilterFileName}";
            }
        }
    }
}
