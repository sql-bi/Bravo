using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Bravo.Architecture.Tests.StaticState.Fixtures;

/// <summary>
/// Ordinary code for which the compiler and the logging generator emit static fields: the lambda is cached in
/// a static field that is assigned at its first use.
/// </summary>
internal sealed partial class ModelCounter(ILogger<ModelCounter> logger)
{
    private readonly ILogger<ModelCounter> _logger = logger;

    public int CountLongNames(IEnumerable<string> names)
    {
        var count = names.Count(name => name.Length > 10);
        LogCounted(count);
        return count;
    }
}
