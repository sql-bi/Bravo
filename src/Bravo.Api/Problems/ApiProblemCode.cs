namespace Bravo.Api.Problems;

/// <summary>
/// The codes that the API itself gives to an error answer. An error that a use case foresees has a code of
/// <see cref="Bravo.Core.Errors.ErrorCode"/>: the two lists together are the codes of the contract.
/// </summary>
internal enum ApiProblemCode
{
    /// <summary>
    /// The request cannot be read: a body that is not what the route expects, a parameter of the wrong type.
    /// </summary>
    InvalidRequest,

    /// <summary>
    /// The request was read, and a value in it is not allowed: the member <c>errors</c> says which.
    /// </summary>
    ValidationFailed,

    /// <summary>
    /// The token of the API is missing or wrong.
    /// </summary>
    Unauthorized,

    NotFound,

    MethodNotAllowed,

    UnsupportedMediaType,

    /// <summary>
    /// The host failed in a way that nobody foresaw.
    /// </summary>
    Unexpected,

    /// <summary>
    /// The answer has a status that has no code of its own: the status is all that tells what happened.
    /// </summary>
    HttpError,
}
