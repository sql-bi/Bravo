using System.Collections.Generic;
using System.Text.Json;
using Bravo.Api.Problems;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Bravo.Api.Tests.Problems;

public sealed class ValidationErrorNamesTests
{
    [Theory]
    [InlineData("Address", "address")]
    [InlineData("Proxy.Address", "proxy.address")]
    [InlineData("Proxy.BypassList", "proxy.bypassList")]
    [InlineData("Tables[2].Name", "tables[2].name")]
    [InlineData("after", "after")]
    public void UseJsonNames_NameOfTheCode_BecomesTheNameThatTravels(string name, string expected)
    {
        var errors = new Dictionary<string, string[]> { [name] = ["Not allowed."] };
        var problem = new HttpValidationProblemDetails(errors);

        ValidationErrorNames.UseJsonNames(problem, JsonNamingPolicy.CamelCase);

        var renamed = Assert.Single(problem.Errors);
        Assert.Equal(expected, renamed.Key);
        Assert.Equal(["Not allowed."], renamed.Value);
    }

    [Fact]
    public void UseJsonNames_JsonWithTheNamesOfTheCode_LeavesTheNames()
    {
        var errors = new Dictionary<string, string[]> { ["Proxy.Address"] = ["Not allowed."] };
        var problem = new HttpValidationProblemDetails(errors);

        ValidationErrorNames.UseJsonNames(problem, namingPolicy: null);

        Assert.Equal("Proxy.Address", Assert.Single(problem.Errors).Key);
    }
}
