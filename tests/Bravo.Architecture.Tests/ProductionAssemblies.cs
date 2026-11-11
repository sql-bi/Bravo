using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bravo.Architecture.Tests;

internal static class ProductionAssemblies
{
    public static IEnumerable<Type> Types()
    {
        return ProductionProjects.AllowedReferences.Keys.Select(Load).SelectMany(assembly => assembly.GetTypes());
    }

    private static Assembly Load(string name)
    {
        return Assembly.Load(new AssemblyName(name));
    }
}
