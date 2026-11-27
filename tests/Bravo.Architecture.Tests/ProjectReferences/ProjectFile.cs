using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Bravo.Architecture.Tests.ProjectReferences;

/// <summary>
/// A project file as it is written: the target framework and the references are the declared ones, MSBuild
/// does not evaluate them.
/// </summary>
internal sealed record ProjectFile(string Name, string TargetFramework, IReadOnlyList<string> References)
{
    // .NET Framework runs on Windows alone: its name is "net" and digits, such as net472.
    public bool TargetsWindows =>
        TargetFramework.Contains("-windows", StringComparison.Ordinal)
        || (TargetFramework.StartsWith("net", StringComparison.Ordinal)
            && TargetFramework.Length > "net".Length
            && TargetFramework.Skip("net".Length).All(char.IsAsciiDigit));

    public static ProjectFile Load(string path)
    {
        return Parse(Path.GetFileNameWithoutExtension(path), File.ReadAllText(path));
    }

    public static ProjectFile Parse(string name, string xml)
    {
        var elements = XDocument.Parse(xml).Descendants().ToList();
        var targetFramework = elements
            .Where(element => element.Name.LocalName == "TargetFramework")
            .Select(element => element.Value.Trim())
            .FirstOrDefault("");
        var references = elements
            .Where(element => element.Name.LocalName == "ProjectReference")
            .SelectMany(element => ((string?)element.Attribute("Include") ?? "").Split(';'))
            .Select(GetProjectName)
            .Where(reference => reference.Length > 0);

        return new ProjectFile(name, targetFramework, [.. references]);
    }

    private static string GetProjectName(string path)
    {
        // Project files use the Windows separator, which Path does not split on other systems.
        return Path.GetFileNameWithoutExtension(path.Trim().Replace('\\', '/'));
    }
}
