using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bravo.Architecture.Tests.ProjectReferences;

namespace Bravo.Architecture.Tests;

internal static class RepositoryLayout
{
    private const string SolutionFileName = "Bravo.slnx";

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
        var source = Path.Combine(FindRoot(), "src");
        if (!Directory.Exists(source))
        {
            return [];
        }

        var paths = Directory.EnumerateFiles(source, "*.csproj", SearchOption.AllDirectories);
        return [.. paths.Select(ProjectFile.Load)];
    }
}
