using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Api.Problems;
using Microsoft.AspNetCore.Mvc;
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
        AddProblemMembers(schema, type);
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

    // Every problem has a code, and the identifier of its request, which the .NET type keeps among its
    // extension members.
    private static void AddProblemMembers(OpenApiSchema schema, Type type)
    {
        if (!typeof(ProblemDetails).IsAssignableFrom(type))
        {
            return;
        }

        schema.Properties ??= new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);
        schema.Properties[ProblemCodes.MemberName] = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Description = "What went wrong, as a name that never changes meaning.",
        };
        schema.Properties["traceId"] = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Description = "The identifier of the request in the log of the host.",
        };

        schema.Required ??= new HashSet<string>(StringComparer.Ordinal);
        schema.Required.Add(ProblemCodes.MemberName);
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
