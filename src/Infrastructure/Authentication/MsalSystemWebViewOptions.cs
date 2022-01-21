using Microsoft.Identity.Client;
using System.IO;

namespace Sqlbi.Bravo.Infrastructure.Authentication
{
    public class MsalSystemWebViewOptions : SystemWebViewOptions
    {
        public MsalSystemWebViewOptions(string webrootPath)
        {
            var succeessHtml = File.ReadAllText(Path.Combine(webrootPath, "auth-msalsuccess.html"));
            var errorHtml = File.ReadAllText(Path.Combine(webrootPath, "auth-msalerror.html"));

            HtmlMessageSuccess = succeessHtml;
            HtmlMessageError = errorHtml;
        }
    }
}
