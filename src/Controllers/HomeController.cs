using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using Dax.Vpax.Tools;
using Dax.ViewModel;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System;
using Microsoft.AspNetCore.Http.Features;

namespace Sqlbi.Bravo.Controllers
{

    [Route("[controller]/[action]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        public static PhotinoNET.PhotinoWindow HostWindow;

        /*
         * Returns a model view from a VPAX file stream.
         * Parameters: file stream
         */
        [HttpPost]
        public string GetModelFromVpax()
        {
            // Make it sync - required by ImportVpax
            var syncIOFeature = HttpContext.Features.Get<IHttpBodyControlFeature>();
            if (syncIOFeature != null){
                syncIOFeature.AllowSynchronousIO = true;
            }

            var vpaxContent = VpaxTools.ImportVpax(stream: Request.Body);

            var _vpaModel = new VpaModel(vpaxContent.DaxModel);

            var databaseSize = _vpaModel.Columns.Sum(c => c.TotalSize);
            var modelInfo = new
            {
                model = new
                {
                    tablesCount = _vpaModel.Tables.Count(),
                    columnsCount = _vpaModel.Columns.Count(),
                    maxRows = _vpaModel.Tables.Max(t => t.RowsCount),
                    size = databaseSize,
                    unreferencedCount = _vpaModel.Columns.Count(t => t.IsReferenced == false),
                    columns =
                        from c in _vpaModel.Columns
                        select new
                        {
                            columnName = c.ColumnName,
                            tableName = c.Table.TableName,
                            columnCardinality = c.ColumnCardinality,
                            size = c.TotalSize,
                            weight = (double)c.TotalSize / databaseSize,
                            isReferenced = c.IsReferenced,
                        },
                },

                measures = new { 
                    /* 
                     * It should contain an array oj objects like this:
                     * 
                     *  {
                     *      name: MEASURE_NAME,
                     *      tableName: TABLE_NAME,
                     *      measure: MEASURE
                     *  }
                     * 
                     */
                }
                

            };

            string result = JsonSerializer.Serialize(modelInfo);

            return result;

        }


        /*
         * Returns a model view (as GetModelFromVpax) from a local Power BI report 
         * Parameters: local window id?
         */
        [HttpGet]
        public string GetModelFromReport()
        {
            return null;
        }

        /*
         * Returns a model view (as GetModelFromVpax) from a remote Power BI dataset 
         * Parameters: remote dataset URL?
         */
        [HttpGet]
        public string GetModelFromDataset()
        {
            return null;
        }

        /*
         * Returns a VPAX file stream from a local Power BI report
         * Parameters: local window id?
         */
        [HttpGet]
        public Stream ExportVpaxFromReport()
        {
            return null;
        }

        /*
         * Returns a VPAX file stream from a remote Power BI dataset
         * Parameters: remote dataset URL?
         */
        [HttpGet]
        public Stream ExportVpaxFromDataset()
        {
            return null;
        }

        /*
         * Returns a list of ids of windows containing local Power BI reports
         */
        [HttpGet]
        public string ListReports()
        {
            return null;
        }

        /*
         * Returns a list of remote Power BI datasets URLs
         */
        [HttpGet]
        public string ListDatasets()
        {
            return null;
        }

        /*
         * Update a local Power BI report (for example by passing formatted measures)
         * Parameters: local window id?, a list of pair of properties/values to change?
         */
        [HttpPost]
        public string UpdateReport()
        {
            return null;
        }

        /*
         * Update a remote Power BI dataset (for example by passing formatted measures)
         * Parameters: remote dataset URL?, a list of pair of properties/values to change?
         */
        [HttpPost]
        public string UpdateDataset()
        {
            return null;
        }

        /*
         * Format passed DAX measures
         * Parameters: a list of measures, a list of options
         */
        [HttpGet]
        public string FormatDax()
        {
            return null;
        }


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
        [HttpGet]
        public string GetOptions()
        {
            return null;
        }
            
        /*
         * Update the options.
         */
        [HttpGet]
        public string UpdateOptions()
        {
            return null;
        }

        /*
         * Sign in by receiving an auth token from the UI.
         */
        [HttpGet]
        public string SignIn()
        {
            return null;
        }

        /*
         * Returns the user info such as the name, email and picture (if any)
         */ 
        [HttpGet]
        public string GetUser()
        {
            return null;
        }
    }
}
