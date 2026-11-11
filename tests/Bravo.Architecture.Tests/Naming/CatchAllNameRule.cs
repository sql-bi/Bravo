using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Bravo.Architecture.Tests.Naming;

/// <summary>
/// A type or a namespace called Helper, Utils or Common says where code was put, not what it does: behavior
/// belongs to the type it is about.
/// </summary>
internal static class CatchAllNameRule
{
    private static readonly ImmutableArray<string> s_typeNameSuffixes =
        ["Helper", "Helpers", "Util", "Utils", "Utility", "Utilities", "Common"];

    private static readonly ImmutableArray<string> s_namespaceSegments = ["Helpers", "Utils", "Utilities", "Common"];

    public static IEnumerable<string> FindViolations(IEnumerable<Type> types)
    {
        foreach (var type in types.Where(type => !CompilerGeneratedTypes.Contains(type)))
        {
            var suffix = FindCatchAllSuffix(type);
            if (suffix is not null)
            {
                yield return $"{type.FullName}: the type name ends with '{suffix}'";
            }

            var segment = FindCatchAllSegment(type);
            if (segment is not null)
            {
                yield return $"{type.FullName}: the namespace has the segment '{segment}'";
            }
        }
    }

    private static string? FindCatchAllSuffix(Type type)
    {
        // Metadata names a generic type "Name`1".
        var name = type.Name.Split('`')[0];
        return s_typeNameSuffixes.FirstOrDefault(suffix => name.EndsWith(suffix, StringComparison.Ordinal));
    }

    private static string? FindCatchAllSegment(Type type)
    {
        var segments = (type.Namespace ?? "").Split('.');
        return s_namespaceSegments.FirstOrDefault(segment => segments.Contains(segment, StringComparer.Ordinal));
    }
}
