using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace Bravo.Architecture.Tests.Solution;

/// <summary>
/// The projects that a solution file or a solution filter lists, as paths from the root of the repository
/// written with <c>/</c>.
/// </summary>
internal static class SolutionFile
{
    public static IReadOnlyList<string> ParseSolution(string xml)
    {
        var paths = XDocument.Parse(xml)
            .Descendants()
            .Where(element => element.Name.LocalName == "Project")
            .Select(element => (string?)element.Attribute("Path") ?? "");

        return [.. paths.Select(Normalize)];
    }

    public static IReadOnlyList<string> ParseFilter(string json)
    {
        using var document = JsonDocument.Parse(json);
        var projects = document.RootElement.GetProperty("solution").GetProperty("projects");

        return [.. projects.EnumerateArray().Select(project => Normalize(project.GetString() ?? ""))];
    }

    public static string Normalize(string path)
    {
        return path.Trim().Replace('\\', '/');
    }
}
