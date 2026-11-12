using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bravo.Core.Activation;

namespace Bravo.Platform.Windows.SingleInstance;

/// <summary>
/// An activation request as the payload of a hand-over: a JSON object in UTF-8. A change that an owner of
/// another version would misread needs a new version of the protocol.
/// </summary>
public static class ActivationPayload
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static byte[] Encode(ActivationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return JsonSerializer.SerializeToUtf8Bytes(request, s_options);
    }

    public static bool TryDecode(ReadOnlySpan<byte> payload, [NotNullWhen(true)] out ActivationRequest? request)
    {
        try
        {
            request = JsonSerializer.Deserialize<ActivationRequest>(payload, s_options);
        }
        catch (JsonException)
        {
            request = null;
            return false;
        }

        // A null in place of the list leaves it without a value.
        if (request is null || request.CommandLineErrors.IsDefault)
        {
            request = null;
            return false;
        }

        return true;
    }
}
