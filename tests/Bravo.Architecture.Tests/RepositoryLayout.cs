using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bravo.Architecture.Tests.ProjectReferences;
using Bravo.Architecture.Tests.Solution;

namespace Bravo.Architecture.Tests;

internal static class RepositoryLayout
{
    public const string SolutionFileName = "Bravo.slnx";

    public const string FilterFileName = "Bravo.Portable.slnf";

    public static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"No folder above {AppContext.BaseDirectory} contains {SolutionFileName}.");
    }

    public static IReadOnlyList<ProjectFile> LoadSourceProjects()
    {
        return [.. FindProjectFiles(FindRoot(), "src").Select(ProjectFile.Load)];
    }

    // The projects of src/ and tests/, each under its path from the root, written with '/'.
    public static Dictionary<string, ProjectFile> LoadProjects()
    {
        var root = FindRoot();
        return FindProjectFiles(root, "src")
            .Concat(FindProjectFiles(root, "tests"))
            .ToDictionary(path => SolutionFile.Normalize(Path.GetRelativePath(root, path)), ProjectFile.Load);
    }

    public static IReadOnlyList<string> LoadSolutionProjects()
    {
        return SolutionFile.ParseSolution(File.ReadAllText(Path.Combine(FindRoot(), SolutionFileName)));
    }

    public static IReadOnlyList<string> LoadFilterProjects()
    {
        return SolutionFile.ParseFilter(File.ReadAllText(Path.Combine(FindRoot(), FilterFileName)));
    }

    private static IEnumerable<string> FindProjectFiles(string root, string folder)
    {
        var directory = Path.Combine(root, folder);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory.EnumerateFiles(directory, "*.csproj", SearchOption.AllDirectories);
    }
}
