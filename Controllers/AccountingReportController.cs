using CRES.BusinessLogic;
using CRES.DataContract;
using CRES.Utilities;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace CRES.Services.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class AccountingReportController : ControllerBase
    {
        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/accountingreport/getallreport")]
        public IActionResult GetAllReportFiles(int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            List<ReportFileDataContract> _lstReportFiles = new List<ReportFileDataContract>();

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            try
            {
            AccountingReportLogic accountingReportLogic = new AccountingReportLogic();
            int? totalCount = 0;

            //UserPermissionLogic upl = new UserPermissionLogic();
            //List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "AccountingReportlist");

            //if (permissionlist.Count > 0)
            //{
            //    _lstReportFiles = accountingReportLogic.GetAllReportFiles(headerUserID, pageSize, pageIndex, out totalCount);
            //}
            _lstReportFiles = accountingReportLogic.GetAllReportFiles(headerUserID, pageSize, pageIndex, out totalCount);

           
                if (_lstReportFiles != null)
                {
                    Logger.Write("Report list loaded successfully", MessageLevel.Info, headerUserID.ToString());
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        ReportFileList = _lstReportFiles,
                        
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
                Logger.Write(ModuleName.AccountingReport.ToString(), "Error in loading all account reports(method:GetAllReportFiles) : " + ExceptionHelper.GetFullMessage(ex), MessageLevel.Error, headerUserID.ToString(), "");
            }
            return Ok(_authenticationResult);
        }
        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/accountingreport/getdocumentsbyobjectid")]
        public IActionResult GetDocumentsByObjectID([FromBody] ReportFileLogDataContract _documentDC, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            List<ReportFileLogDataContract> lstDocuments = new List<ReportFileLogDataContract>();

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            try
            {
            int? totalCount = 0;
            AccountingReportLogic accountingReportLogic = new AccountingReportLogic();
            lstDocuments = accountingReportLogic.GetAllReportFileLogByObjectId(_documentDC, headerUserID, pageIndex, pageSize, out totalCount);

            
                if (lstDocuments != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        lstReportFileLog = lstDocuments
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
                string documentDC = _documentDC.ObjectGUID == null ? "" : _documentDC.ObjectGUID;
                Logger.Write(ModuleName.AccountingReportHistory.ToString(), "Error in getting list of account report history (method:GetDocumentsByObjectID) : " + ExceptionHelper.GetFullMessage(ex), MessageLevel.Error, headerUserID.ToString(), documentDC);
            }
            return Ok(_authenticationResult);
        }
        
        [HttpPost]
        [Route("api/accountingreport/updatereportlogstatus")]
        public IActionResult UpdateReportLogStatus([FromBody] List<ReportFileLogDataContract> _docDC)
        {

            GenericResult _authenticationResult = null;
            List<DocumentDataContract> lstDocuments = new List<DocumentDataContract>();
            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            try
            {
                if (_docDC != null && _docDC.Count > 0)
                {
                    AccountingReportLogic accountingReportLogic = new AccountingReportLogic();
                    accountingReportLogic.UpdateReportLogStatus(_docDC, headerUserID);
                }
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                };

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

        //==========Added getwarehouseStatus==========//
        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/accountingreport/getwarehouseStatus")]
        
        public IActionResult GetwarehouseStatus([FromBody] string btnname)
        {

            GenericResult _authenticationResult = null;
            List<DWStatusDataContract> lstStatus = new List<DWStatusDataContract>();
            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            try
            {

                AccountingReportLogic accountingReportLogic = new AccountingReportLogic();
                lstStatus = accountingReportLogic.GetwarehouseStatus(btnname);


                if (lstStatus != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstDWStatus = lstStatus,
                        Status2= lstStatus[0].Status2,
                        BatchEndTime=CommonHelper.ToDateTime(lstStatus[0].BatchEndTime),
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
//=====================================================================================================//
