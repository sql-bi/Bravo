using System.Diagnostics.CodeAnalysis;

namespace Bravo.Platform.Windows.SingleInstance;

/// <summary>
/// What a process is after the arbitration: the owner of the instance, a second process whose request the
/// owner took, or a second process that could not deliver it.
/// </summary>
public sealed class ArbitrationResult
{
    private ArbitrationResult(InstanceOwnership? ownership, HandOverFailure? failure)
    {
        Ownership = ownership;
        Failure = failure;
    }

    /// <summary>
    /// The ownership, which the caller disposes as its last act; <see langword="null"/> in a second process.
    /// </summary>
    public InstanceOwnership? Ownership { get; }

    /// <summary>
    /// Why the request was not delivered; <see langword="null"/> in the owner and after a delivery.
    /// </summary>
    public HandOverFailure? Failure { get; }

    [MemberNotNullWhen(true, nameof(Ownership))]
    public bool IsOwner => Ownership is not null;

    internal static ArbitrationResult Owner(InstanceOwnership ownership)
    {
        return new ArbitrationResult(ownership, failure: null);
    }

    internal static ArbitrationResult HandedOver()
    {
        return new ArbitrationResult(ownership: null, failure: null);
    }

    internal static ArbitrationResult Failed(HandOverFailure failure)
    {
        return new ArbitrationResult(ownership: null, failure);
    }
}
