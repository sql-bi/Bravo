using System;

namespace Bravo.Architecture.Tests;

internal static class CompilerGeneratedTypes
{
    // The C# compiler gives closures, state machines and the like a name with '<', which source code cannot use.
    public static bool Contains(Type type)
    {
        for (var current = type; current is not null; current = current.DeclaringType)
        {
            if (current.Name.Contains('<', StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
