using System.Collections.Generic;
using Bravo.Core.Activation;

namespace Bravo.Api.Events;

/// <summary>
/// What a start of the application asked for on its command line.
/// </summary>
internal sealed record ActivationEvent
{
    public string? ServerName { get; init; }

    public string? DatabaseName { get; init; }

    /// <summary>
    /// What was wrong in the command line, in English. Empty for a start with no error.
    /// </summary>
    public required IReadOnlyList<string> Errors { get; init; }

    public static ActivationEvent FromRequest(ActivationRequest request)
    {
        return new ActivationEvent
        {
            ServerName = request.ServerName,
            DatabaseName = request.DatabaseName,
            Errors = request.CommandLineErrors,
        };
    }
}
