using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bravo.Architecture.Tests;

/// <summary>
/// The assemblies of the projects of <c>src/</c>. The build of the tests writes their paths to a file, because
/// the test project cannot reference the projects that target Windows.
/// </summary>
internal static class ProductionAssemblies
{
    private const string ListFileName = "ProductionAssemblies.txt";

    public static MetadataAssemblies Read()
    {
        var built = ReadBuiltAssemblies();
        var paths = new List<string>();
        foreach (var project in RepositoryLayout.LoadSourceProjects())
        {
            if (built.TryGetValue(project.Name, out var path))
            {
                paths.Add(path);
            }
            else if (OperatingSystem.IsWindows() || !project.TargetsWindows)
            {
                throw new InvalidOperationException(
                    $"{project.Name}: the build of the tests did not build the project. Reference it in their project file.");
            }
        }

        return MetadataAssemblies.Read(paths);
    }

    // A line reads "path of the project file|path of its assembly".
    private static Dictionary<string, string> ReadBuiltAssemblies()
    {
        return File.ReadLines(Path.Combine(AppContext.BaseDirectory, ListFileName))
            .Select(line => line.Split('|'))
            .ToDictionary(parts => Path.GetFileNameWithoutExtension(parts[0]), parts => parts[1], StringComparer.Ordinal);
    }
}
