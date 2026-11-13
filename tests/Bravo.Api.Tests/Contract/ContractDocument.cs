using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Api.Problems;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Bravo.Api.Tests.Contract;

/// <summary>
/// What is set on the document as a whole, after its routes and its schemas were generated.
/// </summary>
internal static class ContractDocument
{
    /// <summary>
    /// The name of the schema with the codes of an error answer, which the schema of every problem refers to.
    /// </summary>
    public const string ProblemCodeSchemaId = "ProblemCode";

    private const string ProblemMediaType = "application/problem+json";

    public static Task ApplyRulesAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        Describe(document);
        AddProblemMembers(document);

        var schemas = document.Components?.Schemas?.Values.OfType<OpenApiSchema>() ?? [];
        foreach (var schema in schemas)
        {
            RemoveNullAlternatives(schema);
        }

        return Task.CompletedTask;
    }

    // The same text wherever the document is produced: by default the title is the name of the process, and
    // the servers are the addresses it listens on.
    private static void Describe(OpenApiDocument document)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Bravo for Power BI",
            Description = "The API of the host, which the user interface calls on the loopback address.",

            // The standard asks for a version. The routes have none: host and user interface ship together.
            Version = "unversioned",
        };
        document.Servers = [];
    }

    // Every problem has a code, and the identifier of its request, which the .NET type keeps among its
    // extension members. The codes are a closed list, in a schema of its own: the client that is generated
    // from the document knows every code that an answer can carry.
    private static void AddProblemMembers(OpenApiDocument document)
    {
        var codes = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Enum = [.. ProblemCodes.All.Select(code => JsonValue.Create(code))],
            Description = "What went wrong, as a name that never changes meaning. No error answer has a code "
                + "outside this list.",
        };
        document.AddComponent(ProblemCodeSchemaId, codes);

        foreach (var problem in FindProblemSchemas(document))
        {
            problem.Properties ??= new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);
            problem.Properties[ProblemCodes.MemberName] = new OpenApiSchemaReference(ProblemCodeSchemaId, document);
            problem.Properties["traceId"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Description = "The identifier of the request in the log of the host.",
            };

            problem.Required ??= new HashSet<string>(StringComparer.Ordinal);
            problem.Required.Add(ProblemCodes.MemberName);
        }
    }

    // A problem is what an error answer carries: the schemas are found through the answers of the routes.
    private static List<OpenApiSchema> FindProblemSchemas(OpenApiDocument document)
    {
        return document.Paths.Values
            .SelectMany(path => path.Operations?.Values.AsEnumerable() ?? [])
            .SelectMany(operation => operation.Responses?.Values.AsEnumerable() ?? [])
            .SelectMany(response => response.Content?.AsEnumerable() ?? [])
            .Where(content => content.Key == ProblemMediaType)
            .Select(content => content.Value.Schema)
            .Select(schema => schema is OpenApiSchemaReference reference ? reference.Target : schema)
            .OfType<OpenApiSchema>()
            .Distinct()
            .ToList();
    }

    // An optional member whose type has a schema of its own is generated as a choice between null and that
    // schema. Null never travels: the member is that schema, and is optional because it is not required.
    private static void RemoveNullAlternatives(OpenApiSchema schema)
    {
        var properties = schema.Properties?.ToArray() ?? [];
        foreach (var (name, property) in properties)
        {
            if (property.OneOf is [{ Type: JsonSchemaType.Null }, var value])
            {
                schema.Properties![name] = value;
            }
        }
    }
}
