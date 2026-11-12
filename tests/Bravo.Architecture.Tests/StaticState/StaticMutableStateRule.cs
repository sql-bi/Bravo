using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bravo.Architecture.Tests.StaticState;

/// <summary>
/// Static state that can change is shared by every user of a type and outlives a test: a static field is a
/// constant, or a read-only field whose value cannot be changed in place.
/// </summary>
internal static class StaticMutableStateRule
{
    private const BindingFlags DeclaredStaticFields =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private const string BackingFieldSuffix = ">k__BackingField";

    public static IEnumerable<string> FindViolations(IEnumerable<Type> types)
    {
        foreach (var type in types.Where(type => !CompilerGeneratedTypes.Contains(type)))
        {
            foreach (var field in type.GetFields(DeclaredStaticFields))
            {
                var reason = FindReason(field);
                if (reason is not null)
                {
                    yield return $"{type.FullName}.{GetSourceName(field)}: {reason}";
                }
            }
        }
    }

    private static string? FindReason(FieldInfo field)
    {
        if (field.IsLiteral)
        {
            return null;
        }

        if (!field.IsInitOnly)
        {
            return "the static member can be assigned after its initialization";
        }

        if (field.FieldType.IsArray)
        {
            return "the static member is an array, whose elements can be replaced";
        }

        if (IsMutableCollection(field.FieldType))
        {
            return "the static member is a collection that can be changed";
        }

        return null;
    }

    // The value of an auto-property is in a field that the compiler names "<Property>k__BackingField".
    private static string GetSourceName(FieldInfo field)
    {
        var name = field.Name;
        if (name.StartsWith('<') && name.EndsWith(BackingFieldSuffix, StringComparison.Ordinal))
        {
            return name[1..^BackingFieldSuffix.Length];
        }

        return name;
    }

    private static bool IsMutableCollection(Type type)
    {
        var typeNamespace = type.Namespace ?? "";
        if (typeNamespace is "System.Collections.Immutable" or "System.Collections.Frozen")
        {
            return false;
        }

        var isSystemCollection = typeNamespace == "System.Collections"
            || typeNamespace.StartsWith("System.Collections.", StringComparison.Ordinal);
        if (!isSystemCollection || !IsEnumerable(type))
        {
            return false;
        }

        // IEnumerable, IReadOnlyList, ReadOnlyCollection and the like offer no member that changes the collection.
        var name = type.Name;
        return !name.StartsWith("IEnumerable", StringComparison.Ordinal)
            && !name.StartsWith("IReadOnly", StringComparison.Ordinal)
            && !name.StartsWith("ReadOnly", StringComparison.Ordinal);
    }

    // By name: a type read as metadata is not assignable to the IEnumerable of this process.
    private static bool IsEnumerable(Type type)
    {
        return type.GetInterfaces().Append(type).Any(candidate => candidate.FullName == typeof(IEnumerable).FullName);
    }
}
