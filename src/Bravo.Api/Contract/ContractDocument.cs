using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Bravo.Api.Contract;

/// <summary>
/// What is set on the document as a whole, after its routes and its schemas were generated.
/// </summary>
internal static class ContractDocument
{
    public static Task ApplyRulesAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        Describe(document);

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
