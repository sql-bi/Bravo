namespace Bravo.Core.Errors;

/// <summary>
/// What a use case can report with a <see cref="BravoException"/>, as the user interface is told. The list is
/// closed and is part of the contract of the API: the user interface has a text for every member, and the
/// name of a member never changes meaning.
/// </summary>
public enum ErrorCode
{
    // No use case reports an error yet. The first member takes the value 1: zero, the value of a variable
    // that nobody set, is no code.
}
