using System;
using Bravo.Platform.Windows.SingleInstance;

namespace Bravo.Desktop.SingleInstance;

/// <summary>
/// What the user reads when a start finds a running instance and cannot reach it.
/// </summary>
internal static class HandOverFailureMessage
{
    public static string For(HandOverFailure failure)
    {
        var separator = Environment.NewLine + Environment.NewLine;
        return failure switch
        {
            HandOverFailure.AccessDenied =>
                $"{Product.Name} is already running with other privileges, for example as administrator, and "
                + $"this start could not reach it.{separator}Close the running window, or start {Product.Name} "
                + "the same way as the first time.",
            HandOverFailure.NotResponding =>
                $"{Product.Name} is already running, but it did not answer.{separator}Try again in a moment. If "
                + $"it happens again, close the running window and start {Product.Name} once more.",
            HandOverFailure.Refused =>
                $"{Product.Name} is already running and did not accept this start: it may be another "
                + $"version.{separator}Close the running window and start {Product.Name} again.",
            _ => throw new ArgumentOutOfRangeException(nameof(failure), failure, message: null),
        };
    }
}
