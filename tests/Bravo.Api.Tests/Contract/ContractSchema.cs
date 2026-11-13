using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Bravo.Api.Tests.Contract;

/// <summary>
/// Makes each schema of the document say what the JSON of the API does, where the generated schema says
/// something else or nothing.
/// </summary>
internal static class ContractSchema
{
    public static Task ApplyRulesAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;

        DescribeEnumAsString(schema, type);
        RemoveNull(schema);
        WriteDescriptionOnOneLine(schema);

        return Task.CompletedTask;
    }

    // An enum travels as the name of its value. The generated schema lists the names and gives no type.
    private static void DescribeEnumAsString(OpenApiSchema schema, Type type)
    {
        var enumType = Nullable.GetUnderlyingType(type) ?? type;
        if (enumType.IsEnum)
        {
            schema.Type = JsonSchemaType.String;
        }
    }

    // A member without a value is absent, never null: what is optional is what the schema does not require.
    private static void RemoveNull(OpenApiSchema schema)
    {
        if (schema.Type is { } type && type != JsonSchemaType.Null)
        {
            schema.Type = type & ~JsonSchemaType.Null;
        }
    }

    // A description comes from a comment of the code, with its line breaks and its indentation.
    private static void WriteDescriptionOnOneLine(OpenApiSchema schema)
    {
        if (schema.Description is { } description)
        {
            var words = description.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            schema.Description = string.Join(' ', words);
        }
    }
}
