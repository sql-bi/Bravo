using Microsoft.Web.WebView2.Core;

namespace Bravo.Desktop.WebView;

/// <summary>
/// What the application answers when a document asks the engine for a permission. The rule names every kind
/// that it answers: what it allows and what it denies does not change with the version of the SDK.
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

        return kind switch
        {
            // The page reads the clipboard when the user pastes.
            CoreWebView2PermissionKind.ClipboardRead => CoreWebView2PermissionState.Allow,

            // What the application knows, and the page does not use.
            CoreWebView2PermissionKind.UnknownPermission
                or CoreWebView2PermissionKind.Microphone
                or CoreWebView2PermissionKind.Camera
                or CoreWebView2PermissionKind.Geolocation
                or CoreWebView2PermissionKind.Notifications
                or CoreWebView2PermissionKind.OtherSensors
                or CoreWebView2PermissionKind.MultipleAutomaticDownloads
                or CoreWebView2PermissionKind.FileReadWrite
                or CoreWebView2PermissionKind.Autoplay
                or CoreWebView2PermissionKind.LocalFonts
                or CoreWebView2PermissionKind.MidiSystemExclusiveMessages
                or CoreWebView2PermissionKind.WindowManagement
                or CoreWebView2PermissionKind.PersistentStorage => CoreWebView2PermissionState.Deny,

            // A kind that the application does not know, from a later runtime or a later SDK. Denying it
            // could stop the page after an update, with nothing that the user could do.
            _ => null,
        };
    }
}
