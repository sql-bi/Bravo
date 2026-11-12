using System;
using System.IO;
using Bravo.Desktop.WebView;

namespace Bravo.Desktop.Tests.WebView;

/// <summary>
/// The folders of a webview under test, each test with its own: the content folder with a page of the test,
/// and the data folder of the engine.
/// </summary>
internal sealed class WebViewTestSite : IDisposable
{
    /// <summary>
    /// What the title of the page begins with once the page has written its report, as JSON, after it.
    /// </summary>
    public const string ReportPrefix = "report:";

    // The page tells what it found when its first line ran, then what a document of another origin finds
    // and what the API answers. The title of the document is how a page speaks to its host without the
    // messages of the webview, which the application turns off.
    private const string Page = """
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>loading</title></head>
        <body>
        <script>
          const report = {};
          const connection = window.bravoHost;
          report.connection = connection ? { address: connection.address, token: connection.token } : null;
          try { window.bravoHost = { address: "replaced", token: "replaced" }; } catch (error) { }
          try { window.bravoHost.token = "changed"; } catch (error) { }
          report.unchanged = !!connection && window.bravoHost === connection && connection.token === report.connection.token;

          report.loads = Number(sessionStorage.getItem("loads") || "0") + 1;
          sessionStorage.setItem("loads", String(report.loads));

          const otherOrigin = new Promise(resolve => window.addEventListener("message", event => resolve(event.data), { once: true }));
          const frame = document.createElement("iframe");
          frame.sandbox = "allow-scripts";
          frame.srcdoc = "<script>parent.postMessage(typeof window.bravoHost + ' in ' + window.location.origin, '*')<\/script>";
          document.body.appendChild(frame);

          const options = connection
            ? fetch(connection.address + "/options", { headers: { Authorization: "Bearer " + connection.token } })
                .then(response => "status " + response.status, error => "failed: " + error)
            : Promise.resolve("no connection");

          Promise.all([otherOrigin, options]).then(([otherOriginReport, optionsReport]) => {
            report.otherOrigin = otherOriginReport;
            report.options = optionsReport;
            document.title = "report:" + JSON.stringify(report);
          });
        </script>
        </body>
        </html>
        """;

    private readonly string _root = Path.Combine(Path.GetTempPath(), "Bravo.Tests." + Guid.NewGuid().ToString("N"));

    /// <param name="withPage">Whether the content folder has the page.</param>
    public WebViewTestSite(bool withPage = true)
    {
        var contentFolder = Path.Combine(_root, "wwwroot");
        Directory.CreateDirectory(contentFolder);
        if (withPage)
        {
            File.WriteAllText(Path.Combine(contentFolder, PageAddress.IndexFileName), Page);
        }

        Options = new WebViewOptions
        {
            UserDataFolder = Path.Combine(_root, "WebView2"),
            ContentFolder = contentFolder,
            AreDeveloperToolsEnabled = false,
        };
    }

    public WebViewOptions Options { get; }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The engine may still hold files of its data folder for a moment after its control is closed.
        }
    }
}
