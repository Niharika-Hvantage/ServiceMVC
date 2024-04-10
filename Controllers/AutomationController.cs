using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using CRES.BusinessLogic;
using CRES.DataContract;
using System.Data;

namespace CRES.ServicesNew.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    //[Route("api/[controller]")]
    //[ApiController]
    public class AutomationController : ControllerBase
    {
        IConfigurationSection Sectionroot = null;
        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/automation/getDealListForAutomation")]
        public IActionResult GetDealListForAutomation()
        {

            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            DataTable dt = new DataTable();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            GenerateAutomationLogic automationlogic = new GenerateAutomationLogic();
            UserPermissionLogic upl = new UserPermissionLogic();
            //to get user 
            List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "GenerateAutomation");
            //if (permissionlist != null && permissionlist.Count > 0)
            //{
                dt = automationlogic.GetDealListForAutomation();
           // }
            try
            {
                if (dt != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Succeeded",
                        dt = dt
                       
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
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Automation.ToString(), "Error occurred in GetDealListForAutomation on Automation", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_authenticationResult);
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/automation/insertintoautomationRequests")]
        public IActionResult InsertIntoAutomationRequests([FromBody] DataTable data)
        {

            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            List<GenerateAutomationDataContract> list = new List<GenerateAutomationDataContract>();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            GenerateAutomationLogic automationlogic = new GenerateAutomationLogic();
            foreach (DataRow dr in data.Rows)
            {
                GenerateAutomationDataContract gad = new GenerateAutomationDataContract();
                gad.DealID = Convert.ToString(dr["DealID"]);
                gad.StatusText = "Processing";
                gad.AutomationType = 799;
                gad.BatchType = "All_AutoSpread_Deals";
                list.Add(gad);
            }
            automationlogic.QueueDealForAutomation(list, headerUserID.ToString());
            try
            {

                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded"
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Automation.ToString(), "Error occurred in GetDealListForAutomation on Automation", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_authenticationResult);
        }
        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/automation/CancelAutomation")]
        public IActionResult CancelAutomation()
        {

            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            List<GenerateAutomationDataContract> list = new List<GenerateAutomationDataContract>();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            GenerateAutomationLogic automationlogic = new GenerateAutomationLogic();
          
           var res= automationlogic.CancelAutomation();
            try
            {

                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded"
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Automation.ToString(), "Error occurred in CancelAutomation on Automation", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_authenticationResult);
        }


        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/automation/getAutomationAuditLog")]
        public IActionResult GetAutomationAuditLog(int? pageIndex, int? pageSize)
        {

            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            int? totalCount = 0;
            List<GenerateAutomationDataContract> list = new List<GenerateAutomationDataContract>();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            GenerateAutomationLogic automationlogic = new GenerateAutomationLogic();

          DataTable dt = automationlogic.GetAutomationAuditLog(pageIndex, pageSize, out totalCount);
           
            try
            {

                _authenticationResult = new GenericResult()
                {
                    dt=dt,
                    Succeeded = true,
                    Message = "Authentication succeeded"
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Automation.ToString(), "Error occurred in GetAutomationAuditLog on Automation", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_authenticationResult);
        }



        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/automation/getDealByBatchIDAutomation")]
        public IActionResult GetDealByBatchIDAutomation(int ID)
        {

            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            List<GenerateAutomationDataContract> list = new List<GenerateAutomationDataContract>();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            GenerateAutomationLogic automationlogic = new GenerateAutomationLogic();

            DataTable dt = automationlogic.GetDealByBatchIDAutomation(ID);
            try
            {

                _authenticationResult = new GenericResult()
                {
                    dt = dt,
                    Succeeded = true,
                    Message = "Authentication succeeded"
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Automation.ToString(), "Error occurred in GetAutomationAuditLog on Automation", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_authenticationResult);
        }




        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/automation/deleteAutomationlog")]
        public IActionResult DeleteAutomationlog(int ID)
        {

            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            List<GenerateAutomationDataContract> list = new List<GenerateAutomationDataContract>();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            GenerateAutomationLogic automationlogic = new GenerateAutomationLogic();

            var res = automationlogic.DeleteAutomationlog(ID);
            try
            {

                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded"
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Automation.ToString(), "Error occurred in GetAutomationAuditLog on Automation", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
