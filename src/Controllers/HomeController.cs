using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace Sqlbi.Bravo.Controllers
{
    [Route("api/[action]")]
    [ApiController]
    [NonController]
    public class HomeController : ControllerBase
    {
        /*
         * Returns a model view from a VPAX file stream.
         * Parameters: file stream
         */
        //[HttpPost]
        //public IActionResult GetModelFromVpax()

        /*
         * Returns a model view (as GetModelFromVpax) from a local Power BI report 
         * Parameters: local window id?
         */
        //[HttpGet]
        //public string GetModelFromReport()
        //{
        //    return null;
        //}

        /*
         * Returns a model view (as GetModelFromVpax) from a remote Power BI dataset 
         * Parameters: remote dataset URL?
         */
        //[HttpGet]
        //public string GetModelFromDataset()
        //{
        //    return null;
        //}

        /*
         * Returns a VPAX file stream from a local Power BI report
         * Parameters: local window id?
         */
        //[HttpGet]
        //public Stream ExportVpaxFromReport()
        //{
        //    return null;
        //}

        /*
         * Returns a VPAX file stream from a remote Power BI dataset
         * Parameters: remote dataset URL?
         */
        //[HttpGet]
        //public Stream ExportVpaxFromDataset()
        //{
        //    return null;
        //}

        /*
         * Returns a list of ids of windows containing local Power BI reports
         */
        //[HttpGet]
        //public string ListReports()
        //{
        //    return null;
        //}

        /*
         * Returns a list of remote Power BI datasets URLs
         */
        //[HttpGet]
        //public string ListDatasets()
        //{
        //    return null;
        //}

        /*
         * Update a local Power BI report (for example by passing formatted measures)
         * Parameters: local window id?, a list of pair of properties/values to change?
         */
        //[HttpPost]
        //public string UpdateReport()
        //{
        //    return null;
        //}

        /*
         * Update a remote Power BI dataset (for example by passing formatted measures)
         * Parameters: remote dataset URL?, a list of pair of properties/values to change?
         */
        //[HttpPost]
        //public string UpdateDataset()
        //{
        //    return null;
        //}

        /*
         * Format passed DAX measures
         * Parameters: a list of measures, a list of options
         */
        //[HttpGet]
        //public string FormatDax()
        //{
        //    return null;
        //}


        /*
         * Change the window theme
         * Parameters: a string that could be "light" or "dark"
         */
        [HttpGet]
        public string ChangeTheme()
        {
            return null;
        }


        /*
         * Dec 5, 2021: These are OPTIONAL, depending on how we decide to implement things - see the email
         */

        /*
         * Get the options.
         */
        //[HttpGet]
        //public string GetOptions()
        //{
        //    return null;
        //}
            
        /*
         * Update the options.
         */
        //[HttpGet]
        //public string UpdateOptions()
        //{
        //    return null;
        //}

        /*
         * See => ** AuthenticationController.Redirect() **
         * Respond to the MSAL authentication
         * http://localhost:5000/home/auth
         * We intercept the code and use the MSAL library here or we can load auth.html 
         */
        //[HttpGet]
        //public string Auth()

        /*
         * Sign in by receiving an auth token from the UI.
         */
        //[HttpGet]
        //public string SignIn()
        //{
        //    return null;
        //}

        /*
         * Returns the user info such as the name, email and picture (if any)
         */ 
        //[HttpGet]
        //public string GetUser()
        //{
        //    return null;
        //}
    }
}
