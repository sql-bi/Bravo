using System.Windows.Forms;
using Bravo.Platform.Windows.SingleInstance;

namespace Bravo.Desktop.SingleInstance;

internal static class SecondInstance
{
    /// <summary>
    /// Ends a process that is not the owner of the instance: in silence when the owner took its request, with
    /// a message when it did not.
    /// </summary>
    /// <returns>The exit code of the process.</returns>
    public static int End(ArbitrationResult arbitration)
    {
        if (arbitration.Failure is not { } failure)
        {
            return ExitCode.Success;
        }

        var text = HandOverFailureMessage.For(failure);
        _ = MessageBox.Show(text, Product.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return ExitCode.ActivationNotDelivered;
    }
}
