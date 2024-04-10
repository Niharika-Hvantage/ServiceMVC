using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using CRES.DataContract;
using CRES.BusinessLogic;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace CRES.Services.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class ImportServicingLogController : ControllerBase
    {
        [HttpPost]
        [Services.Controllers.DeflateCompression]
        [Services.Controllers.IsAuthenticate]
        [Route("api/ImportServicing/importIntoServicing")]
        public IActionResult ImportIntoINServicingTransaction([FromBody] List<ServicingLogDataContract> lstServicingLog)
        {
            GenericResult _actionResult = null;
            var result = 0;
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            var createdBy = string.Empty;
            var UpdatedBy = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }


            try
            {
                if (result != 0)
                {

                    _actionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Data imported successfully.",
                    };
                }
                else
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Updation failed",
                    };
                }
            }
            catch (Exception ex)
            {
                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            //var javaScriptSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            //string jsonString = javaScriptSerializer.Serialize(_actionResult);
            //var json = new JavaScriptSerializer().Serialize(_actionResult);
            //Request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            return Ok(_actionResult);
        }
    }
}