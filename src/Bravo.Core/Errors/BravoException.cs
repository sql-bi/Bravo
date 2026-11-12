using System;

namespace Bravo.Core.Errors;

/// <summary>
/// An error that a use case foresees, such as a model that changed while it was being updated. The code tells
/// the user interface what happened; the message is for a person who reads it in English.
/// </summary>
public sealed class BravoException : Exception
{
    /// <param name="code">
    /// A PascalCase name that never changes meaning: the user interface chooses by it what to tell the user.
    /// </param>
    /// <param name="message">What happened, in a sentence.</param>
    public BravoException(string code, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Code = code;
    }

    /// <inheritdoc cref="BravoException(string, string)"/>
    public BravoException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Code = code;
    }

    public string Code { get; }
}
