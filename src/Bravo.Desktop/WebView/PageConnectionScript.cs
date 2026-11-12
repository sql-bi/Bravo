using System;
using System.Text.Json;

namespace Bravo.Desktop.WebView;

/// <summary>
/// The script that gives the page the address and the token of the API. The webview runs it in every
/// document before the scripts of the document: the page finds the object when its first line runs.
/// </summary>
internal static class PageConnectionScript
{
    /// <summary>
    /// The name of the object on <c>window</c>. The user interface reads it by this name, whoever set it.
    /// </summary>
    public const string ObjectName = "bravoHost";

    /// <summary>
    /// Creates the script. It defines the object only in a document of the origin of the page, and the page
    /// can neither replace the object nor change it.
    /// </summary>
    /// <param name="apiAddress">Where the API listens.</param>
    /// <param name="token">The token of the API.</param>
    public static string Create(Uri apiAddress, string token)
    {
        ArgumentNullException.ThrowIfNull(apiAddress);
        ArgumentException.ThrowIfNullOrEmpty(token);

        // Each value is written as JSON, which is also a literal of the language of the script. The address
        // has no slash at its end: the page puts the path of a route after it.
        var origin = JsonSerializer.Serialize(PageAddress.Origin);
        var name = JsonSerializer.Serialize(ObjectName);
        var address = JsonSerializer.Serialize(apiAddress.GetLeftPart(UriPartial.Authority));
        var tokenValue = JsonSerializer.Serialize(token);

        return $$"""
            (() => {
                if (window.location.origin !== {{origin}}) {
                    return;
                }
                Object.defineProperty(window, {{name}}, {
                    value: Object.freeze({ address: {{address}}, token: {{tokenValue}} }),
                    writable: false,
                    configurable: false,
                    enumerable: false
                });
            })();
            """;
    }
}
