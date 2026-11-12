using System;
using System.Windows.Forms;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// Tells the user that the application is ending because of an error.
/// </summary>
internal sealed class FatalExceptionDialog : IFatalExceptionHandler
{
    public void Handle(Exception exception)
    {
        _ = MessageBox.Show(CreateText(exception), Product.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    internal static string CreateText(Exception exception)
    {
        var separator = Environment.NewLine + Environment.NewLine;
        var error = $"{exception.GetType().FullName}: {exception.Message}";
        return $"{Product.Name} met an unexpected error and has to close.{separator}{error}";
    }
}
