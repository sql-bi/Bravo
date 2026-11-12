using Xunit;

namespace Bravo.Architecture.Tests.Solution;

public sealed class SolutionFileTests
{
    [Fact]
    public void ParseSolution_ProjectsInFolders_ReturnsThePathsWithForwardSlashes()
    {
        var xml = """
            <Solution>
              <Folder Name="/src/">
                <Project Path="src/Bravo.First/Bravo.First.csproj" />
              </Folder>
              <Folder Name="/tests/">
                <Project Path="tests\Bravo.First.Tests\Bravo.First.Tests.csproj" />
              </Folder>
            </Solution>
            """;

        var projects = SolutionFile.ParseSolution(xml);

        Assert.Equal(["src/Bravo.First/Bravo.First.csproj", "tests/Bravo.First.Tests/Bravo.First.Tests.csproj"], projects);
    }

    [Fact]
    public void ParseFilter_Projects_ReturnsThePathsWithForwardSlashes()
    {
        var json = """
            {
              "solution": {
                "path": "Bravo.slnx",
                "projects": [
                  "src/Bravo.First/Bravo.First.csproj",
                  "tests\\Bravo.First.Tests\\Bravo.First.Tests.csproj"
                ]
              }
            }
            """;

        var projects = SolutionFile.ParseFilter(json);

        Assert.Equal(["src/Bravo.First/Bravo.First.csproj", "tests/Bravo.First.Tests/Bravo.First.Tests.csproj"], projects);
    }
}
