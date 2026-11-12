using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Bravo.Architecture.Tests;

/// <summary>
/// Assemblies read as metadata. Nothing in them is loaded or run, so a portable test can inspect an assembly
/// that was built for Windows.
/// </summary>
internal sealed class MetadataAssemblies : IDisposable
{
    private readonly MetadataLoadContext _context;
    private readonly ImmutableArray<Assembly> _assemblies;

    private MetadataAssemblies(MetadataLoadContext context, ImmutableArray<Assembly> assemblies)
    {
        _context = context;
        _assemblies = assemblies;
    }

    // The types are usable until the instance is disposed.
    public IEnumerable<Type> Types => _assemblies.SelectMany(assembly => assembly.GetTypes());

    public static MetadataAssemblies Read(IReadOnlyCollection<string> paths)
    {
        var resolver = new PathAssemblyResolver(FindResolvableFiles(paths));
        var context = new MetadataLoadContext(resolver);
        return new MetadataAssemblies(context, [.. paths.Select(context.LoadFromAssemblyPath)]);
    }

    public Type GetType(Type type)
    {
        return Types.Single(candidate => candidate.FullName == type.FullName);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    // What a type of the assemblies can name: the assemblies themselves, what this process can load, the files
    // beside each assembly and the installed frameworks. One file for each name: two copies of an assembly
    // cannot be read together.
    private static IEnumerable<string> FindResolvableFiles(IReadOnlyCollection<string> paths)
    {
        var trusted = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "";
        var loadable = trusted.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        var directories = paths.Select(path => Path.GetDirectoryName(path)!).Concat(FindInstalledFrameworks());
        var files = directories
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.dll"));

        return paths.Concat(loadable).Concat(files).DistinctBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase);
    }

    // The frameworks installed beside the runtime of this process, in its version: an application for Windows
    // runs on the Windows Desktop one, which a portable process does not load.
    private static IEnumerable<string> FindInstalledFrameworks()
    {
        var runtime = new DirectoryInfo(RuntimeEnvironment.GetRuntimeDirectory());
        var frameworks = runtime.Parent?.Parent?.EnumerateDirectories() ?? [];
        return frameworks.Select(framework => Path.Combine(framework.FullName, runtime.Name)).Where(Directory.Exists);
    }
}
