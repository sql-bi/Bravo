using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Bravo.Api.Json;

internal static class JsonServiceCollectionExtensions
{
    /// <summary>
    /// Sets how every route reads and writes JSON. The OpenAPI document describes these same rules.
    /// </summary>
    public static IServiceCollection AddApiJson(this IServiceCollection services)
    {
        return services.ConfigureHttpJsonOptions(options =>
        {
            var json = options.SerializerOptions;

            // An enum is the name of its value, in both directions: a number is refused.
            json.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));

            // A number is a JSON number: the default of the web also reads one from a string.
            json.NumberHandling = JsonNumberHandling.Strict;

            // A member without a value is absent. Null is never written, and is read as absent only where
            // the member is optional: for a member that always has a value it is an error.
            json.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            json.RespectNullableAnnotations = true;
        });
    }
}
