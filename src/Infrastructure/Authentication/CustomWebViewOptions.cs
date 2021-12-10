using Microsoft.Identity.Client;
using System.IO;

namespace Sqlbi.Bravo.Infrastructure.Authentication
{
    public class CustomWebViewOptions : SystemWebViewOptions
    {
        public CustomWebViewOptions(string webrootPath)
        {
            var succeessHtml = File.ReadAllText(Path.Combine(webrootPath, "auth-success.html"));
            var errorHtml = Path.Combine(webrootPath, "auth-error.html");

            HtmlMessageSuccess = succeessHtml;
            HtmlMessageError = errorHtml;
        }
    }
}
