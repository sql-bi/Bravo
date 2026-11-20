using System;
using System.Windows.Forms;
using Bravo.Platform.Windows.Policies;

namespace Bravo.Desktop.Policies;

internal static class PolicyReadFailureDialog
{
    /// <summary>
    /// Tells the user that the application does not start because its policies cannot be read, and where.
    /// </summary>
    /// <returns>The exit code of the process.</returns>
    public static int End(PolicyReadException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        _ = MessageBox.Show(CreateText(exception.KeyName), Product.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
        return ExitCode.PoliciesUnreadable;
    }

    internal static string CreateText(string keyName)
    {
        var separator = Environment.NewLine + Environment.NewLine;
        return $"{Product.Name} cannot start: the policies that your organization set for it cannot be read."
            + $"{separator}Ask your administrator to check the permissions of the registry key {keyName}.";
    }
}
