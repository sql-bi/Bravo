using System.Collections.Generic;

namespace Bravo.Architecture.Tests.StaticState.Fixtures;

internal static class MutableStatics
{
    internal static int s_reassignable = 1;

    internal static readonly int[] s_array = [1, 2];

    internal static readonly List<string> s_list = ["a"];

    internal static readonly IDictionary<string, int> s_dictionary = new Dictionary<string, int>();

    internal static int Settable { get; set; }
}
