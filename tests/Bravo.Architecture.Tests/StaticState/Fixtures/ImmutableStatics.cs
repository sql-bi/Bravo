using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Bravo.Architecture.Tests.StaticState.Fixtures;

internal static class ImmutableStatics
{
    internal const int Limit = 10;

    internal static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(1);

    internal static readonly ImmutableArray<string> s_names = ["a", "b"];

    internal static readonly FrozenSet<string> s_keys = FrozenSet.ToFrozenSet(["a", "b"]);

    internal static readonly IReadOnlyList<string> s_items = ["a", "b"];

    internal static readonly IEqualityComparer<string> s_comparer = StringComparer.Ordinal;

    internal static int Version { get; } = 3;
}
