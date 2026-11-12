using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bravo.Core.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Xunit;

namespace Bravo.Api.Tests.Contract;

/// <summary>
/// The OpenAPI document is a file of the repository, which the client of the user interface is generated
/// from. A change of the API that the file does not show fails here.
/// </summary>
public sealed class OpenApiDocumentTests
{
    private const string FilePath = "src/Bravo.Api/openapi.json";

    private const string UpdateCommand = "dotnet test --project tests/Bravo.Api.Tests -c Release --explicit only";

    [Fact]
    public async Task Document_ProducedFromTheRoutes_IsTheFileOfTheRepository()
    {
        var produced = await ProduceDocumentAsync();

        var file = await File.ReadAllTextAsync(FindFile(), TestContext.Current.CancellationToken);

        var difference = FindFirstDifference(Normalize(file), produced);
        Assert.True(
            difference is null,
            $"""
            The API no longer matches {FilePath}.
            {difference}
            If the change is wanted, write the file again and commit it:
              {UpdateCommand}
            """);
    }

    [Fact]
    public async Task Document_NoSchema_SaysThatAValueCanBeNull()
    {
        using var document = JsonDocument.Parse(await ProduceDocumentAsync());

        var nullable = Descendants(document.RootElement).Where(AllowsNull).Select(schema => schema.GetRawText());

        Assert.Empty(nullable);
    }

    [Fact]
    public async Task Document_EveryEnum_IsDescribedAsAString()
    {
        using var document = JsonDocument.Parse(await ProduceDocumentAsync());

        var enums = Descendants(document.RootElement).Where(element => element.TryGetProperty("enum", out _)).ToList();

        Assert.NotEmpty(enums);
        Assert.All(enums, schema => Assert.Equal("string", schema.GetProperty("type").GetString()));
    }

    [Fact]
    public async Task Document_EveryRoute_HasANameATagAndTheAnswersThatAnyRouteCanGive()
    {
        using var document = JsonDocument.Parse(await ProduceDocumentAsync());

        var operations = document.RootElement.GetProperty("paths").EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject())
            .Select(operation => operation.Value)
            .ToList();

        Assert.NotEmpty(operations);
        Assert.All(operations, operation =>
        {
            Assert.False(string.IsNullOrEmpty(operation.GetProperty("operationId").GetString()));
            Assert.NotEqual(0, operation.GetProperty("tags").GetArrayLength());
            Assert.True(HasProblemAnswer(operation, "401") && HasProblemAnswer(operation, "500"), operation.GetRawText());
        });
    }

    // The validation and the descriptions of the document are generated for public types only, and neither
    // says anything about a type that is not public.
    [Fact]
    public async Task Document_EveryTypeOfTheApiThatTravels_IsPublic()
    {
        using var document = JsonDocument.Parse(await ProduceDocumentAsync());
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var schemaNames = schemas.EnumerateObject().Select(schema => schema.Name).ToHashSet();

        var travelling = typeof(ApiOptions).Assembly.GetTypes().Where(type => schemaNames.Contains(type.Name)).ToList();

        Assert.NotEmpty(travelling);
        Assert.All(travelling, type => Assert.True(type.IsPublic, $"{type.FullName} travels and is not public."));
    }

    /// <summary>
    /// Not a test: writes the document over the file of the repository. It runs only when asked for.
    /// </summary>
    [Fact(Explicit = true)]
    public async Task WriteTheFileOfTheRepository()
    {
        var produced = await ProduceDocumentAsync();

        await File.WriteAllTextAsync(FindFile(), produced, new UTF8Encoding(false), TestContext.Current.CancellationToken);
    }

    // The API as the application composes it, plus the services of the document, which no application adds.
    private static async Task<string> ProduceDocumentAsync()
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IUserSettingsStore, InMemoryUserSettingsStore>();
        builder.Services.AddApi();
        builder.Services.AddApiContract();

        await using var app = builder.Build();
        app.UseApi();
        await app.StartAsync(TestContext.Current.CancellationToken);

        var provider = app.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>(
            ContractServiceCollectionExtensions.DocumentName);
        var document = await provider.GetOpenApiDocumentAsync(TestContext.Current.CancellationToken);
        var json = await document.SerializeAsJsonAsync(
            OpenApiSpecVersion.OpenApi3_1,
            TestContext.Current.CancellationToken);

        return Normalize(json) + "\n";
    }

    // The file has the line endings of the checkout, the document the ones of the system.
    private static string Normalize(string text)
    {
        return text.ReplaceLineEndings("\n").TrimEnd('\n');
    }

    private static string? FindFirstDifference(string file, string produced)
    {
        var fileLines = file.Split('\n');
        var producedLines = produced.TrimEnd('\n').Split('\n');
        var line = Enumerable.Range(0, Math.Max(fileLines.Length, producedLines.Length))
            .Cast<int?>()
            .FirstOrDefault(index => LineAt(fileLines, index) != LineAt(producedLines, index));
        if (line is null)
        {
            return null;
        }

        return $"""
            First difference at line {line + 1}:
              file: {fileLines.ElementAtOrDefault(line.Value) ?? "(end)"}
              API:  {producedLines.ElementAtOrDefault(line.Value) ?? "(end)"}
            """;
    }

    private static string? LineAt(string[] lines, int? index)
    {
        return lines.ElementAtOrDefault(index!.Value);
    }

    // The type of a schema is a name or a list of names.
    private static bool AllowsNull(JsonElement schema)
    {
        return schema.TryGetProperty("type", out var type)
            && type.GetRawText().Contains("null", StringComparison.Ordinal);
    }

    private static bool HasProblemAnswer(JsonElement operation, string status)
    {
        return operation.GetProperty("responses").TryGetProperty(status, out var response)
            && response.GetProperty("content").TryGetProperty("application/problem+json", out _);
    }

    // Every object of the document, at any depth.
    private static IEnumerable<JsonElement> Descendants(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            yield return element;
        }

        var children = element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().Select(property => property.Value),
            JsonValueKind.Array => element.EnumerateArray(),
            _ => [],
        };
        foreach (var descendant in children.SelectMany(Descendants))
        {
            yield return descendant;
        }
    }

    private static string FindFile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Bravo.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException($"No folder above {AppContext.BaseDirectory} is the repository.");
        }

        return Path.Combine(directory.FullName, FilePath);
    }
}
