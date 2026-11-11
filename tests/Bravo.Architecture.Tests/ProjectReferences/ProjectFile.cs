using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Bravo.Architecture.Tests.ProjectReferences;

/// <summary>
/// A project file as it is written: the references are the declared ones, MSBuild does not evaluate them.
/// </summary>
internal sealed record ProjectFile(string Name, IReadOnlyList<string> References)
{
    public static ProjectFile Load(string path)
    {
        return Parse(Path.GetFileNameWithoutExtension(path), File.ReadAllText(path));
    }

    public static ProjectFile Parse(string name, string xml)
    {
        var references = XDocument.Parse(xml)
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .SelectMany(element => ((string?)element.Attribute("Include") ?? "").Split(';'))
            .Select(GetProjectName)
            .Where(reference => reference.Length > 0);

        return new ProjectFile(name, [.. references]);
    }

    private static string GetProjectName(string path)
    {
        // Project files use the Windows separator, which Path does not split on other systems.
        return Path.GetFileNameWithoutExtension(path.Trim().Replace('\\', '/'));
    }
}
