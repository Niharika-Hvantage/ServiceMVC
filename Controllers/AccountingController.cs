using CRES.BusinessLogic;
using CRES.DataContract;
using CRES.NoteCalculator;
using CRES.Utilities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

namespace CRES.Services.Controllers
{
     
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class AccountingController : ControllerBase
    {
        private PeriodicLogic _periodicLogic = new PeriodicLogic();

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/accounting/getallaccountingclose")]
        public IActionResult GetAllAccountingClose([FromBody] string PortfolioMasterGuid)
        {
            GenericResult _authenticationResult = null;
            var headerUserID = new Guid();
            DataTable dtPeriodicClose = new DataTable();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            if (PortfolioMasterGuid != null)
            {
                dtPeriodicClose = _periodicLogic.GetAlPeriodicClose(new Guid(PortfolioMasterGuid));
            }
            else
            {
                dtPeriodicClose = _periodicLogic.GetAlPeriodicClose(null);
            }
            try
            {
                if (dtPeriodicClose != null)
                {
                    Logger.Write(dtPeriodicClose + " periodic close for user Id " + headerUserID + " loaded successfully", MessageLevel.Info);
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(dtPeriodicClose.Rows.Count),
                        dt = dtPeriodicClose
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed"
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

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/accounting/saveaccountingbyclosedate")]
        public IActionResult SaveAccountingByCloseDate([FromBody] DataTable dt)
        {
            GenericResult _authenticationResult = null;
            List<PeriodicDataContract> lstPeriodicClose = new List<PeriodicDataContract>();
            var result = 0;
            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            result = _periodicLogic.Closeaccounting(dt, headerUserID.ToString());

            try
            {
                if (result != 0)
                {
                    Logger.Write(lstPeriodicClose.Count + "Accounting close Save Accounting By Close Date for user Id " + headerUserID + " is successfully done.", MessageLevel.Info);
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",

                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed"
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

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/accounting/saveaccountingbyopendate")]
        public IActionResult SaveAccountingByOpenDate([FromBody] DataTable dt)
        {
            GenericResult _authenticationResult = null;
            List<PeriodicDataContract> lstPeriodicClose = new List<PeriodicDataContract>();
            var result = 0;
            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            result = _periodicLogic.Openaccounting(dt, headerUserID.ToString());

            try
            {
                if (result != 0)
                {
                    Logger.Write(dt.Rows.Count + "Accounting close Save Accounting By Open Date for user Id " + headerUserID + " is suucessfully done.", MessageLevel.Info);
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",

                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed"
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

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/accounting/getaccountingclosebydealId")]
        public IActionResult GetAccountingCloseByDealId([FromBody] string DeailId, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            int? totalCount = 0;

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            DataTable dtPeriodicClose = _periodicLogic.GetAccountingCloseByDealId(DeailId, headerUserID.ToString(), pageIndex, pageSize, out totalCount);

            try
            {
                if (dtPeriodicClose != null)
                {
                    Logger.Write(dtPeriodicClose.Rows.Count + "Accounting close get by Deal id by userid " + headerUserID + " .", MessageLevel.Info);
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(dtPeriodicClose.Rows.Count),
                        dt = dtPeriodicClose
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed"
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

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/accounting/savedealaccountingbyclosedate")]
        public IActionResult SaveDealAccountingByCloseDate([FromBody] string str)
        {
            GenericResult _authenticationResult = null;
            var result = 0;
            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            result = _periodicLogic.SaveDealAccountingByCloseDate(str, headerUserID.ToString());

            try
            {
                if (result != 0)
                {
                    Logger.Write("Accounting close Save Accounting By Close Date for user Id " + headerUserID + " is successfully done.", MessageLevel.Info);
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",

                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed"
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

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/accounting/savedealaccountingbyopendate")]
        public IActionResult SaveDealAccountingByOpenDate([FromBody] string str)
        {
            GenericResult _authenticationResult = null;
            var result = 0;
            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            result = _periodicLogic.SaveDealAccountingByOpenDate(str, headerUserID.ToString());

            try
            {
                if (result != 0)
                {
                    Logger.Write("Accounting Open Save Accounting By Open Date for user Id " + headerUserID + " is successfully done.", MessageLevel.Info);
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",

                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed"
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