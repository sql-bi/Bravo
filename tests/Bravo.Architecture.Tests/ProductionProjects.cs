using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Bravo.Architecture.Tests;

/// <summary>
/// The projects of <c>src/</c>, each with the projects it may reference. A project that is not here fails the
/// tests.
/// </summary>
internal static class ProductionProjects
{
    public static FrozenDictionary<string, ImmutableArray<string>> AllowedReferences { get; } =
        new Dictionary<string, ImmutableArray<string>>().ToFrozenDictionary(StringComparer.Ordinal);
}
