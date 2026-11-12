using System.Collections.Immutable;

namespace Bravo.Core.Activation;

/// <summary>
/// What a start of the application asks for. Every start is one request: the one that opens the window, and
/// the ones that a later process hands over to the running instance.
/// </summary>
public sealed class ActivationRequest
{
    /// <summary>
    /// A start with no arguments.
    /// </summary>
    public static ActivationRequest Empty { get; } = new();

    public string? ServerName { get; init; }

    public string? DatabaseName { get; init; }

    public int? ParentProcessId { get; init; }

    /// <summary>
    /// What was wrong in the command line. A request with errors is delivered like any other.
    /// </summary>
    public ImmutableArray<string> CommandLineErrors { get; init; } = [];
}
