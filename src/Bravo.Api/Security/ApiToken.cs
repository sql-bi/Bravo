using System;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Bravo.Api.Security;

/// <summary>
/// The secret that a client of the API shows with every request. It is created for this process, lives in
/// memory only, and must never be written to a file or to a log.
/// </summary>
public sealed class ApiToken
{
    private const int RandomByteCount = 32;

    private readonly byte[] _valueBytes;

    public ApiToken()
    {
        Value = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(RandomByteCount));
        _valueBytes = Encoding.ASCII.GetBytes(Value);
    }

    /// <summary>
    /// The token as a client sends it.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Whether <paramref name="candidate"/> is the token. The time of the answer does not tell how much of a
    /// wrong candidate is right.
    /// </summary>
    public bool Matches(ReadOnlySpan<char> candidate)
    {
        // The length of the token is not a secret.
        if (candidate.Length != _valueBytes.Length)
        {
            return false;
        }

        Span<byte> candidateBytes = stackalloc byte[_valueBytes.Length];
        Encoding.ASCII.GetBytes(candidate, candidateBytes);

        return CryptographicOperations.FixedTimeEquals(candidateBytes, _valueBytes);
    }
}
