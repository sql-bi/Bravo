using System;

namespace Bravo.Core.Errors;

/// <summary>
/// An error that a use case foresees, such as a model that changed while it was being updated. The code tells
/// the user interface what happened; the message is for a person who reads it in English.
/// </summary>
public sealed class BravoException : Exception
{
    /// <param name="code">What happened: a member of the list, by which the user interface chooses its text.</param>
    /// <param name="message">What happened, in a sentence.</param>
    /// <exception cref="ArgumentOutOfRangeException">The code is not a member of the list.</exception>
    public BravoException(ErrorCode code, string message)
        : base(message)
    {
        Code = RequireListed(code);
    }

    /// <inheritdoc cref="BravoException(ErrorCode, string)"/>
    public BravoException(ErrorCode code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = RequireListed(code);
    }

    public ErrorCode Code { get; }

    // A number that was cast to the type is no code. The list and this check are one assembly, so a member
    // added later is known here.
    private static ErrorCode RequireListed(ErrorCode code)
    {
        if (!Enum.IsDefined(code))
        {
            throw new ArgumentOutOfRangeException(nameof(code), code, "The code is not a member of the list.");
        }

        return code;
    }
}
