using System;
using Microsoft.Web.WebView2.Core;

namespace Bravo.Desktop.WebView;

/// <summary>
/// What the application answers when a document asks the engine for a permission.
/// </summary>
internal static class WebViewPermissionRule
{
    /// <summary>
    /// Decides the answer to a request.
    /// </summary>
    /// <param name="kind">What is asked for, as the engine names it.</param>
    /// <param name="address">The address of the document that asks.</param>
    /// <returns>
    /// The answer of the application, or <see langword="null"/> when the application does not decide: the
    /// engine then asks the user, and keeps the answer.
    /// </returns>
    public static CoreWebView2PermissionState? Decide(CoreWebView2PermissionKind kind, string? address)
    {
        if (!PageAddress.Contains(address))
        {
            return CoreWebView2PermissionState.Deny;
        }

        // The page reads the clipboard when the user pastes.
        if (kind == CoreWebView2PermissionKind.ClipboardRead)
        {
            return CoreWebView2PermissionState.Allow;
        }

        if (Enum.IsDefined(kind))
        {
            return CoreWebView2PermissionState.Deny;
        }

        // A kind that this version cannot name is one that a later runtime added. Denying it could stop
        // the page after an update of the runtime, with nothing that the user could do.
        return null;
    }
}
