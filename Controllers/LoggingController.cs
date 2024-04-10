using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CRES.BusinessLogic;
using CRES.DataContract;
using ExcelDataReader.Log;
using Microsoft.AspNetCore.Mvc;


namespace CRES.ServiceMVC.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class LoggingController : ControllerBase
    {

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/logging/writeLog")]
        public IActionResult WriteLog([FromBody] string logtext)
        {
            GenericResult _authenticationResult = null;
            List<DevDashBoardDataContract> lstCalculationStatus = new List<DevDashBoardDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            LoggerLogic log = new LoggerLogic();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }            
            
            String[] strlist= logtext.Split("||");
            if (strlist[1].ToLower() == "error")
            {
                log.WriteLogExceptionMessage(strlist[0], strlist[2], "", headerUserID, "UI Log", "");
            }
            else
            {
                log.WriteLogInfo(strlist[0], strlist[2], "", headerUserID);
            }           
           
            try
            {
                if (lstCalculationStatus != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Succeeded"
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Failed"
                    };
                }
            }
            catch (Exception ex)
            {               
                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }
    }
}
