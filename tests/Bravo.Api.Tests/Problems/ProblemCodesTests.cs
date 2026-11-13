using System;
using System.Linq;
using Bravo.Api.Problems;
using Xunit;

namespace Bravo.Api.Tests.Problems;

public sealed class ProblemCodesTests
{
    // A name that the API and a use case both had would mean two things to the user interface.
    [Fact]
    public void All_TheCodesOfTheApiAndOfTheUseCases_ShareNoName()
    {
        var names = ProblemCodes.All;

        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }
}
