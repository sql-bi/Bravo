using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Bravo.Architecture.Tests;

internal static class RuleAssert
{
    // Assert.Empty cuts each item at fifty characters, and a violation of the repository must be read in full.
    public static void NoViolations(IEnumerable<string> violations)
    {
        var lines = violations.ToList();
        if (lines.Count > 0)
        {
            Assert.Fail(string.Join(Environment.NewLine, lines));
        }
    }
}
