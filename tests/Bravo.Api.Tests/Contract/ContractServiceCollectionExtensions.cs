using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Bravo.Api.Tests.Contract;

internal static class ContractServiceCollectionExtensions
{
    public const string DocumentName = "bravo";

    /// <summary>
    /// Adds what produces the OpenAPI document of the API. It is here, among the tests, because the
    /// application does not serve the document: a test produces it and compares it with the file of the
    /// repository.
    /// </summary>
    public static IServiceCollection AddApiContract(this IServiceCollection services)
    {
        return services.AddOpenApi(DocumentName, options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            options.CreateSchemaReferenceId = CreateSchemaReferenceId;
            options.AddSchemaTransformer(ContractSchema.ApplyRulesAsync);
            options.AddDocumentTransformer(ContractDocument.ApplyRulesAsync);
        });
    }

    // Any JSON value has no schema of its own: the name of the .NET type that holds it stays out.
    private static string? CreateSchemaReferenceId(JsonTypeInfo typeInfo)
    {
        var type = Nullable.GetUnderlyingType(typeInfo.Type) ?? typeInfo.Type;
        if (type == typeof(JsonElement))
        {
            return null;
        }

        return OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);
    }
}
