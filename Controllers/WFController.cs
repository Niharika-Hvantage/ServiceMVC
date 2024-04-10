using CRES.BusinessLogic;
using CRES.DataContract;
using CRES.DataContract.WorkFlow;
using CRES.NoteCalculator;
using CRES.Services.Infrastructure;
using CRES.Utilities;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Net.Security;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CRES.Services.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class WFController : ControllerBase
    {

        private readonly IEmailNotification _iEmailNotification;
        public string DynamicsAuthToken = "";
        public WFController(IEmailNotification iemailNotification)
        {
            _iEmailNotification = iemailNotification;
        }


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/wfcontroller/getworkflowdetailbytaskId")]
        public IActionResult GetWorkflowDetailByTaskId([FromBody] WFDetailDataContract _wfDetailDataContract)
        {
            GenericResult _authenticationResult = null;
            //WFDetailDataContract _wfDetailDataContract = new WFDetailDataContract();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            WFLogic _wfLogic = new WFLogic();

            UserPermissionLogic upl = new UserPermissionLogic();
            List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "Draw_approval_List");

            //if (permissionlist.Count > 0)
            //{
            _wfDetailDataContract = _wfLogic.GetWorkflowDetailByTaskId(_wfDetailDataContract, headerUserID.ToString());
            _wfDetailDataContract.WFAdditionalList = _wfLogic.GetWorkflowAdditionalDetailByTaskId(_wfDetailDataContract, headerUserID.ToString());
            //_wfDetailDataContract.User = _wfLogic.GetDealPrimaryAMByDealOrTaskType("", Convert.ToInt32(_wfDetailDataContract.TaskTypeID), _wfDetailDataContract.TaskID, "");
            if (_wfDetailDataContract.PurposeTypeId == 630)
            {
                if (_wfDetailDataContract.WFAdditionalList! != null)
                {
                    _wfDetailDataContract.ExitFee = _wfDetailDataContract.WFAdditionalList.ExitFee;
                    _wfDetailDataContract.ExitFeePercentage = _wfDetailDataContract.WFAdditionalList.ExitFeePercentage;
                    _wfDetailDataContract.PrepayPremium = _wfDetailDataContract.WFAdditionalList.PrepayPremium;
                }
                //GetDiscrepancyForCommitmentDataByDealID
                if (_wfDetailDataContract.WFStatusMasterID > 1)
                {
                    DealLogic dl = new DealLogic();
                    DataTable dt = dl.GetDiscrepancyForCommitmentDataByDealID(_wfDetailDataContract.DealID);
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        _wfDetailDataContract.IsDiscrepancyForCommitment = true;
                    }
                }
            }
            if (_wfDetailDataContract.WFAdditionalList! != null)
            {
                _wfDetailDataContract.AdditionalEmail = _wfDetailDataContract.WFAdditionalList.AdditionalEmail;
            }

            _wfDetailDataContract.WFCheckList = _wfLogic.GetCheckListByTaskId(_wfDetailDataContract, headerUserID.ToString());
            if (_wfDetailDataContract.IsDisableFundingTeamApproval == 1)
            {
                //_wfDetailDataContract.WFCheckList.ForEach(i => i.IsDisable = (_wfDetailDataContract.IsDisableFundingTeamApproval == 1 && i.CheckListMasterId == 6));

                if (_wfDetailDataContract.TaskTypeID == 502)
                    _wfDetailDataContract.WFCheckList.Where(i => i.CheckListMasterId == 6).ToList().ForEach(i => { i.CheckListStatus = 499; i.CheckListStatusText = "Yes"; i.IsDisable = true; });
                else if (_wfDetailDataContract.TaskTypeID == 719)
                {
                    _wfDetailDataContract.WFCheckList.Where(i => i.CheckListMasterId == 15).ToList().ForEach(i => { i.CheckListStatus = 499; i.CheckListStatusText = "Yes"; i.IsDisable = true; });
                }
            }
            
            //commented as per jason's email asking for backtrack the change- M61 - Make Draw Fee Applicable Checklist Item "No" on Force Funding Purpose Type
            //if (_wfDetailDataContract.PurposeTypeId == 520)
            //{
            //    if (_wfDetailDataContract.TaskTypeID == 502)
            //        _wfDetailDataContract.WFCheckList.Where(i => i.CheckListMasterId == 9).ToList().ForEach(i => { i.CheckListStatus = 616; i.CheckListStatusText = "No"; i.IsDisable = true;i.Comment = String.IsNullOrEmpty(i.Comment) ? "Force Funding" : i.Comment; });

            //}



            _wfDetailDataContract.WFStatusList = _wfLogic.GetStatusMasterByTaskId(_wfDetailDataContract, headerUserID.ToString());
            //_wfDetailDataContract.WFClientList = _wfLogic.GetClientByDealFundingID(new Guid(_wfDetailDataContract.TaskID), headerUserID.ToString());
            _wfDetailDataContract.WFNotificationMasterEmail = _wfLogic.GetWFNotificationMasterEmail(_wfDetailDataContract, headerUserID.ToString());
            var CurrWFStatusPurposeMapping = _wfDetailDataContract.WFStatusList.Where(i => i.StatusName.ToLower() == _wfDetailDataContract.StatusName.ToLower());

            //}

            List<WFRejectListDataContract> lstRejectList = new List<WFRejectListDataContract>();
            //var q = _wfDetailDataContract.WFStatusList.FindAll(x => x.WFStatusPurposeMappingID < _wfDetailDataContract.WFStatusPurposeMappingID).ToList();
            //var q = _wfDetailDataContract.WFStatusList.FindAll(x => x.WFStatusPurposeMappingID < CurrWFStatusPurposeMapping.FirstOrDefault().WFStatusPurposeMappingID).ToList();

            lstRejectList = _wfLogic.GetWFRejectStatusByTaskId(_wfDetailDataContract, headerUserID.ToString());
            lstRejectList = lstRejectList.FindAll(x => x.WFStatusPurposeMappingID < CurrWFStatusPurposeMapping.FirstOrDefault().WFStatusPurposeMappingID).ToList();

            //foreach (var item in q)
            //{
            //    WFRejectListDataContract RejectList = new WFRejectListDataContract();
            //    RejectList.WFStatusPurposeMappingID = item.WFStatusPurposeMappingID;
            //    RejectList.StatusName = item.StatusName;
            //    RejectList.StatusDisplayName = item.StatusDisplayName;
            //    lstRejectList.Add(RejectList);
            //}
            try
            {
                if (_wfDetailDataContract != null)
                {
                    Logger.Write("Deal list loaded successfully", MessageLevel.Info, headerUserID.ToString());
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        WFDetailDataContract = _wfDetailDataContract,
                        lstRejectList = lstRejectList,
                        UserPermissionList = permissionlist

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
                Logger.Write("Error in loading all deal detail", ex);

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
        [Route("api/wfcontroller/getworkflowadditionaldetailbytaskId")]
        public IActionResult GetWorkflowAdditionalDetailByTaskId([FromBody] WFDetailDataContract _wfDetailDataContract)
        {

            GenericResult _authenticationResult = null;
            //WFDetailDataContract _wfDetailDataContract = new WFDetailDataContract();

            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            WFLogic _wfLogic = new WFLogic();
            _wfDetailDataContract.WFAdditionalList = _wfLogic.GetWorkflowAdditionalDetailByTaskId(_wfDetailDataContract, headerUserID.ToString());

            try
            {
                if (_wfDetailDataContract != null)
                {
                    Logger.Write("Deal list loaded successfully", MessageLevel.Info, headerUserID.ToString());
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        WFDetailDataContract = _wfDetailDataContract
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
                Logger.Write("Error in loading workflow additional detail", ex);

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
        [Route("api/wfcontroller/manageworkflowdetailfortaskId")]
        public IActionResult ManageWorkflowDetailForTaskId([FromBody] WFDetailDataContract _wfDetailDataContract)
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            List<WFNotificationDataContract> lstWFDetail = new List<WFNotificationDataContract>();
            string PreHeaderText = "";
            var headerUserID = string.Empty;
            var delegateuserid = string.Empty;
            int status = 0;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            if (!string.IsNullOrEmpty(Request.Headers["DelegatedUser"]))
            {
                delegateuserid = Convert.ToString(Request.Headers["DelegatedUser"]);
            }

            try
            {



                _wfDetailDataContract.CreatedBy = headerUserID;
                _wfDetailDataContract.DelegatedUserID = delegateuserid;

                WFLogic _wfLogic = new WFLogic();

                WFConcurrencyParams conparams = new WFConcurrencyParams();
                conparams.TaskID = _wfDetailDataContract.TaskID;
                conparams.TaskTypeID = _wfDetailDataContract.TaskTypeID;
                conparams.WFStatusPurposeMappingID = _wfDetailDataContract.OriginalWFStatusPurposeMappingID;

                status = _wfLogic.CheckWFConcurrency(headerUserID, conparams);
                if (status == 1)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        StatusCode = status
                    };
                    return Ok(_authenticationResult);
                }



                string res = _wfLogic.InsertWorkflowDetailForTaskId(_wfDetailDataContract);
                //add/update full pay of fields

                //if (_wfDetailDataContract.PurposeTypeId == 630 || !string.IsNullOrEmpty(_wfDetailDataContract.AdditionalEmail))
                //if (_wfDetailDataContract.PurposeTypeId == 630)

                // {
                WFTaskAdditionalDetailDataContract dc = new WFTaskAdditionalDetailDataContract();
                dc.TaskID = _wfDetailDataContract.TaskID;
                dc.TaskTypeID = _wfDetailDataContract.TaskTypeID;
                dc.ExitFee = _wfDetailDataContract.ExitFee;
                dc.ExitFeePercentage = _wfDetailDataContract.ExitFeePercentage;
                dc.PrepayPremium = _wfDetailDataContract.PrepayPremium;
                dc.AdditionalEmail = _wfDetailDataContract.AdditionalEmail;
                _wfLogic.InsertUpdateWFTaskAdditionalDetail(dc, headerUserID);
                //}

                if (_wfDetailDataContract.TaskTypeID == 719)
                {
                    if (_wfDetailDataContract.WFCheckList.Where(x => x.CheckListMasterId == 17) != null && _wfDetailDataContract.WFCheckList.Where(x => x.CheckListMasterId == 17).Count() > 0)
                    {
                        var CheckListStatus = _wfDetailDataContract.WFCheckList.Where(x => x.CheckListMasterId == 17).FirstOrDefault().CheckListStatus;
                        if (CheckListStatus == 499)
                            _wfLogic.UpdatePropertyManagerEmail(_wfDetailDataContract);
                    }


                }

                string message = "Changes were saved successfully.";

                if (headerUserID != null)
                {
                    if (res == "success")
                    {
                        //send email except Save,Save & Draft
                        if (_wfDetailDataContract.SubmitType != 551 && _wfDetailDataContract.SubmitType != 497)
                        {
                            //notifiy user by sending email
                            lstWFDetail = _wfLogic.GetWorkflowNotificationDetailByTaskId(_wfDetailDataContract.TaskID, Convert.ToInt32(_wfDetailDataContract.TaskTypeID), headerUserID);
                            if (lstWFDetail.Count > 0)
                            {
                                string DwarApprovalList = "";
                                foreach (WFCheckListDataContract checklist in _wfDetailDataContract.WFCheckList)
                                {
                                    var CheckListStatusText = _wfDetailDataContract.WFCheckListStatus.Where(x => x.LookupID == checklist.CheckListStatus).FirstOrDefault().Name;
                                    DwarApprovalList += "<tr><td style=" + "text-align:left;padding-left:5px!important; padding-right:5px!important;" + ">" + checklist.CheckListName + "</td><td style=" + "text-align:left;padding-left:5px!important; padding-right:5px!important;" + ">" + CheckListStatusText + "</td><td style=" + "text-align:left;padding-left:5px!important; padding-right:5px!important;" + ">" + checklist.Comment + "</td></tr>";
                                }

                                //get activity log
                                string commentHistory = "";
                                List<WFDetailDataContract> lstWFComments = new List<WFDetailDataContract>();
                                //userid= "00000000-0000-0000-0000-000000000000" means timezone will be Easten Standard Time
                                lstWFComments = _wfLogic.GetWFCommentsByTaskId(_wfDetailDataContract, "00000000-0000-0000-0000-000000000000");
                                lstWFComments = lstWFComments.Where(
                                    x => (((x.WFStatusMasterID != 1 && x.WFStatusMasterID != 2) || x.SubmitType == 496) && x.Comment != "Checklist updated"
                                    && x.Comment.Contains("Changed the funding date") == false && x.Comment.Contains("Changed the funding amount") == false)
                                    ).ToList();
                                //CreatedDate
                                foreach (WFDetailDataContract wfd in lstWFComments)
                                {
                                    if (!String.IsNullOrEmpty(wfd.DelegatedUserName))
                                    {
                                        wfd.Login = wfd.DelegatedUserName + "(on behalf of " + wfd.Login + " )";
                                    }
                                    //Wednesday, November 27th, 2019 12:9:44 PM
                                    commentHistory += "<i>" + wfd.Login + "  " + ((wfd.SubmitType == 496 && string.IsNullOrEmpty(wfd.Comment)) ? "rejected transaction back to " : "") + wfd.StatusName.FormatWFStatusName() + "  " + Convert.ToDateTime(wfd.CreatedDate).ToString("dddd, MMMM dd, yyyy hh:mm:tt") + " " + wfd.Abbreviation + "</i>" + "<br/>";
                                    if (!string.IsNullOrEmpty(wfd.Comment))
                                    {
                                        commentHistory += wfd.Comment + "<br/>";
                                    }
                                }

                                if (!string.IsNullOrEmpty(commentHistory))
                                {
                                    commentHistory = "<b>Activity log are below:</b><br/>" + commentHistory;
                                }
                                //Add “Funding Team’s Approval Required” to beginning of the subject line if value of checklist 'Funding Team’s Approval Required'  is 'YES'
                                var FundingApprovalRequired = _wfDetailDataContract.WFCheckList.Where(x => x.CheckListMasterId == 6 && x.CheckListStatus == 499);
                                if (FundingApprovalRequired != null && FundingApprovalRequired.Count() > 0 && _wfDetailDataContract.TaskTypeID == 502)
                                {
                                    PreHeaderText = FundingApprovalRequired.FirstOrDefault().CheckListName;
                                }

                                var ReserveFundingApprovalRequired = _wfDetailDataContract.WFCheckList.Where(x => x.CheckListMasterId == 15 && x.CheckListStatus == 499);
                                if (ReserveFundingApprovalRequired != null && ReserveFundingApprovalRequired.Count() > 0 && _wfDetailDataContract.TaskTypeID == 719)
                                {
                                    PreHeaderText = ReserveFundingApprovalRequired.FirstOrDefault().CheckListName;
                                }

                                lstWFDetail.ForEach(x =>
                                {
                                    x.DealName = _wfDetailDataContract.DealName;
                                    x.Comment = _wfDetailDataContract.DrawComment;
                                    x.ActivityLog = commentHistory;
                                    x.FooterText = _wfDetailDataContract.FooterText;
                                    x.SenderName = _wfDetailDataContract.SenderName;
                                    x.DwarApprovalList = DwarApprovalList;
                                    x.SpecialInstructions = _wfDetailDataContract.SpecialInstructions;
                                    x.AdditionalComments = _wfDetailDataContract.AdditionalComments;
                                    x.NoteswithAmount = x.FundingAmount > 0 ? _wfDetailDataContract.NoteswithAmount : "";
                                    x.ReserveScheduleBreakDown = x.FundingAmount > 0 ? _wfDetailDataContract.ReserveScheduleBreakDown : "";
                                    x.PreHeaderText = PreHeaderText;
                                    x.TaskTypeID = _wfDetailDataContract.TaskTypeID;
                                });

                                if (_wfDetailDataContract.TaskTypeID == 502)
                                {
                                    _iEmailNotification.SendWorkFlowNotification(lstWFDetail);
                                }
                                else if (_wfDetailDataContract.TaskTypeID == 719)
                                {
                                    _iEmailNotification.SendReserveWorkFlowInternalNotification(lstWFDetail);
                                }
                            }
                        }

                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = message,
                            WFStatusPurposeMappingID = _wfDetailDataContract.WFStatusPurposeMappingID

                        };
                    }
                    else
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "Some Error Occured."
                        };
                    }
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
        [Route("api/wfcontroller/getallworkflow")]
        public IActionResult GetAllWorkflow(string filterType, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            List<WorkflowListDataContract> _lstWorkflow = new List<WorkflowListDataContract>();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            WFLogic wfLogic = new WFLogic();
            int? totalCount = 0;

            UserPermissionLogic upl = new UserPermissionLogic();
            //List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "Deallist");
            List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "WorkFlow");

            //if (permissionlist.Count > 0)
            //{
            //    _lstDeals = dealLogic.GetAllDealUSP(headerUserID, pageSize, pageIndex, out totalCount);
            //}

            //_lstWorkflow = wfLogic.GetAllWorkflow(headerUserID, pageSize, pageIndex, out totalCount);
            _lstWorkflow = wfLogic.GetAllWorkflowByFiltertype(headerUserID, filterType, pageSize, pageIndex, out totalCount);

            try
            {
                if (_lstWorkflow != null)
                {
                    Logger.Write("Workflow list loaded successfully", MessageLevel.Info, headerUserID.ToString());
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        lstWorkflow = _lstWorkflow,
                        UserPermissionList = permissionlist
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
                Logger.Write("Error in loading all deal detail", ex);

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
        [Route("api/wfcontroller/getallworkflowbyfiltertype")]
        public IActionResult GetAllWorkflowByFiltertype([FromBody] WFDetailDataContract _wfDetailDataContract, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            List<WorkflowListDataContract> _lstWorkflow = new List<WorkflowListDataContract>();
            List<WFDashboardDataContract> _lstWFDashboard = new List<WFDashboardDataContract>();


            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            WFLogic wfLogic = new WFLogic();
            int? totalCount = 0;

            UserPermissionLogic upl = new UserPermissionLogic();
            //List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "Deallist");
            List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "WorkFlow");

            //if (permissionlist.Count > 0)
            //{
            //    _lstDeals = dealLogic.GetAllDealUSP(headerUserID, pageSize, pageIndex, out totalCount);
            //}

            //_lstWorkflow = wfLogic.GetAllWorkflow(headerUserID, pageSize, pageIndex, out totalCount);
            _lstWorkflow = wfLogic.GetAllWorkflowByFiltertype(headerUserID, _wfDetailDataContract.FilterType, pageSize, pageIndex, out totalCount);
            //to get user 
            var userdata = upl.GetFCApprover(headerUserID);

            try
            {
                if (_lstWorkflow != null)
                {

                    Logger.Write("Workflow list loaded successfully", MessageLevel.Info, headerUserID.ToString());
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        lstWorkflow = _lstWorkflow,
                        UserPermissionList = permissionlist,
                        UserList = userdata
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
                Logger.Write("Error in loading all deal detail", ex);

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
        [Route("api/wfcontroller/GetWFCommentsByTaskId")]
        public IActionResult GetWFCommentsByTaskId([FromBody] WFDetailDataContract _wfDetailDataContract)
        {

            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            WFLogic _wfLogic = new WFLogic();

            UserPermissionLogic upl = new UserPermissionLogic();
            List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "Draw_approval_List");

            List<WFDetailDataContract> lstWFComments = new List<WFDetailDataContract>();
            //if (permissionlist.Count > 0)
            //{
            if (!string.IsNullOrEmpty(_wfDetailDataContract.TimeZone))
            {
                headerUserID = new Guid("00000000-0000-0000-0000-000000000000");
            }
            lstWFComments = _wfLogic.GetWFCommentsByTaskId(_wfDetailDataContract, headerUserID.ToString());

            //}

            try
            {
                if (_wfDetailDataContract != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstWFComments = lstWFComments,

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
                Logger.Write("Error in loading all deal detail", ex);

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
        [Route("api/wfcontroller/getWFNotificationConfigByNotificationType")]
        public IActionResult GetWFNotificationConfigByNotificationType([FromBody] WFNotificationMasterDataContract _wfNotificationMasterDataContract)
        {
            GenericResult _authenticationResult = null;
            List<WFNotificationConfigDataContract> WFNotificationConfig = new List<WFNotificationConfigDataContract>();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            WFLogic wfLogic = new WFLogic();
            _wfNotificationMasterDataContract.CreatedBy = headerUserID.ToString();
            WFNotificationConfig = wfLogic.GetWFNotificationConfigByNotificationType(_wfNotificationMasterDataContract);

            try
            {
                if (WFNotificationConfig != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        wfNotificationConfigDataContract = WFNotificationConfig,
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
                Logger.Write("Error in getting notification config", ex);

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
        [Route("api/wfcontroller/InsertUpdateWFNotification")]
        public IActionResult InsertUpdateWFNotification([FromBody] WFNotificationDetailDataContract _wfDetailDataContract)
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            var delegatedUserID = string.Empty;
            string htmlContent = "";
            int status = 0;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            if (!string.IsNullOrEmpty(Request.Headers["DelegatedUser"]))
            {
                delegatedUserID = Convert.ToString(Request.Headers["DelegatedUser"]);
            }

            try
            {
                _wfDetailDataContract.CreatedBy = headerUserID;
                _wfDetailDataContract.DelegatedUserID = delegatedUserID;
                WFLogic _wfLogic = new WFLogic();

                //
                WFConcurrencyParams conparams = new WFConcurrencyParams();
                conparams.TaskID = _wfDetailDataContract.TaskID.ToString();
                conparams.TaskTypeID = _wfDetailDataContract.TaskTypeID;
                conparams.WFStatusPurposeMappingID = _wfDetailDataContract.OriginalWFStatusPurposeMappingID;

                status = _wfLogic.CheckWFConcurrency(headerUserID, conparams);
                if (status == 1)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        StatusCode = status
                    };
                    return Ok(_authenticationResult);
                }
                //

                try
                {
                    ////send email except Save,Save & Draft
                    //  EmailNotification notification = new EmailNotification();
                    _wfDetailDataContract.MessageHTML = Regex.Replace(_wfDetailDataContract.MessageHTML, @"\r\n?|\n", "<br />");
                    if (!string.IsNullOrEmpty(_wfDetailDataContract.EmailToIds))
                    {
                        _wfDetailDataContract.EmailToIds = _wfDetailDataContract.EmailToIds.Replace(";", ",");
                    }
                    if (!string.IsNullOrEmpty(_wfDetailDataContract.EmailCCIds))
                    {
                        _wfDetailDataContract.EmailCCIds = _wfDetailDataContract.EmailCCIds.Replace(";", ",");
                        //if (!string.IsNullOrEmpty(_wfDetailDataContract.AdditionalEmail))
                        //{
                        //    _wfDetailDataContract.EmailCCIds = _wfDetailDataContract.EmailCCIds + "," + _wfDetailDataContract.AdditionalEmail.Replace(";", ",");
                        //}
                    }
                    //else if(!string.IsNullOrEmpty(_wfDetailDataContract.AdditionalEmail))
                    //{
                    //    _wfDetailDataContract.EmailCCIds = _wfDetailDataContract.AdditionalEmail.Replace(";", ",");
                    //}



                    //LogDB("sending email");
                    _iEmailNotification.SendWFNotification(_wfDetailDataContract, out htmlContent);
                }
                catch (Exception ex)
                {

                    //LogDB(ex.Message);
                }

                _wfDetailDataContract.MessageHTML = htmlContent;
                if (!string.IsNullOrEmpty(_wfDetailDataContract.EnvironmentName))
                {
                    _wfDetailDataContract.Subject = _wfDetailDataContract.Subject.Replace(_wfDetailDataContract.EnvironmentName, "");
                }

                string res = _wfLogic.InsertUpdateWFNotification(_wfDetailDataContract, headerUserID);

                //update exit fee and additional email info
                WFTaskAdditionalDetailDataContract dc = new WFTaskAdditionalDetailDataContract();
                dc.TaskID = _wfDetailDataContract.TaskID.ToString();
                dc.TaskTypeID = _wfDetailDataContract.TaskTypeID;
                dc.ExitFee = _wfDetailDataContract.ExitFee;
                dc.ExitFeePercentage = _wfDetailDataContract.ExitFeePercentage;
                dc.PrepayPremium = _wfDetailDataContract.PrepayPremium;
                dc.AdditionalEmail = _wfDetailDataContract.AdditionalEmail;
                _wfLogic.InsertUpdateWFTaskAdditionalDetail(dc, headerUserID);
                //
                string message = "Changes were saved successfully.";

                if (headerUserID != null)
                {
                    if (res == "success")
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = message
                        };
                    }
                    else
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "Some Error Occured."
                        };
                    }
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
        [Route("api/wfcontroller/getTemplateRecipientEmailIDs")]
        public IActionResult GetTemplateRecipientEmailIDs([FromBody] WFNotificationMasterDataContract _wfNotificationMasterDataContract)
        {

            GenericResult _authenticationResult = null;
            List<WFTemplateRecipientDataContract> wfTemplateRecipient = new List<WFTemplateRecipientDataContract>();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            WFLogic wfLogic = new WFLogic();
            _wfNotificationMasterDataContract.CreatedBy = headerUserID.ToString();
            wfTemplateRecipient = wfLogic.GetTemplateRecipientEmailIDs(_wfNotificationMasterDataContract);

            try
            {
                if (wfTemplateRecipient != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        wfTemplateRecipient = wfTemplateRecipient,
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
                Logger.Write("Error in getting notification config", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }

        [HttpGet]
        [Route("api/wfcontroller/sendConsolidatWFEmailNotification-new")]
        public IActionResult GetConsolidatWFEmailNotification()
        {
            GenericResult _authenticationResult = null;
            List<WFTemplateRecipientDataContract> wfTemplateRecipient = new List<WFTemplateRecipientDataContract>();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            WFLogic wfLogic = new WFLogic();
            DataTable dt = wfLogic.GetConsolidatWFEmailNotification();
            try
            {
                if (dt.Rows.Count > 0)
                {
                    _iEmailNotification.SendConsolidatWFEmailNotification();

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Mail Send Successfully",

                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Mail Send failed"
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Error in getting notification config", ex);

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
        [Route("api/deal/checkwfconcurrentupdate")]
        public IActionResult CheckWFConcurrentUpdate([FromBody] WFDetailDataContract _wfDetailDataContract)
        {
            GenericResult _authenticationResult = null;
            WFDetailDataContract wfDate = new WFDetailDataContract();
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            WFLogic wfLogic = new WFLogic();

            //DateTime dt = DateTime.ParseExact(DealDC.LastUpdatedFF_String, "yyyy-MM-dd HH:mm:ss tt", CultureInfo.InvariantCulture);
            DateTime dtWFUpdateDate = DateTime.ParseExact(_wfDetailDataContract.UpdatedDate.ToString(), "MM/dd/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
            DateTime dtWFAdditionalUpdateDate = DateTime.ParseExact(_wfDetailDataContract.UpdatedDate.ToString(), "MM/dd/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);

            wfDate = wfLogic.CheckWFConcurrentUpdate(new Guid(_wfDetailDataContract.TaskID), dtWFUpdateDate, dtWFAdditionalUpdateDate);

            try
            {
                if (wfDate.LastUpdated == null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = ""
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = wfDate.LastUpdatedBy + " has modified the schedule on " + Convert.ToDateTime(wfDate.LastUpdated).ToString("M/dd/yyyy h:mm:ss tt") + " please refresh and then save your changes."

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
        [Route("api/wfcontroller/validatewireconfirmbytaskId")]
        public IActionResult ValidateWireConfirmByTaskId([FromBody] WFDetailDataContract _wfDetailDataContract)
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            try
            {
                _wfDetailDataContract.CreatedBy = headerUserID;

                WFLogic _wfLogic = new WFLogic();

                int res = _wfLogic.ValidateWireConfirmByTaskId(_wfDetailDataContract, headerUserID);

                if (headerUserID != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Success",
                        StatusCode = res
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

        [HttpGet]
        [Route("api/wfcontroller/testcheckapppath")]
        public string TestCheckAppPath()
        {
            //string nm =CommonHelper.FormatCustomerForQuickBook("205 E Riverside Drive & Crescent Apartments", "19-1571");
            string htmlContentMain = "";
            try
            {
                using (StreamReader reader = new StreamReader(_iEmailNotification.TestCheckAppPath() + "//" + "wwwroot/EmailTemplate/" + "WorkFlowRequestNotification.html"))
                {
                    htmlContentMain = reader.ReadToEnd();
                }
                //htmlContentMain = _iEmailNotification.TestCheckAppPath();
                return htmlContentMain;

            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }

        [HttpGet]
        [Route("api/wfcontroller/ForceFundingnotification")]
        public string ForceFundingnotification()
        {
            string htmlContentMain = "";
            try
            {

                WFLogic _wfLogic = new WFLogic();
                DataTable dt = new DataTable();
                NoteLogic _NoteLogic = new NoteLogic();
                List<HolidayListDataContract> _HolidayList = new List<HolidayListDataContract>();
                WFNotificationDataContract _notificationData = null;
                WFDetailDataContract _wfDetailDataContract = null;
                _HolidayList = _NoteLogic.GetHolidayList();
                dt = _wfLogic.GetAllForceFunding();

                foreach (DataRow dr in dt.Rows)
                {

                    _wfDetailDataContract = new WFDetailDataContract();
                    _wfDetailDataContract.TaskID = Convert.ToString(dr["Taskid"]);
                    _wfDetailDataContract.TaskTypeID = 502;
                    DateTime fdat = Convert.ToDateTime(dr["Date"]);
                    int datediff = 0;

                    DateTime Cdat = DateTime.Now.Date;

                    DateTime dtnxtdate = DateExtensions.CreateNewDate(Cdat.Year, Cdat.Month, Cdat.Day);
                    //DateTime next10Workingdate = DateExtensions.GetnextWorkingDays(dtnxtdate.AddDays(1), Convert.ToInt16(10), "US", _HolidayList).Date;
                    //DateTime next5Workingdate = DateExtensions.GetnextWorkingDays(dtnxtdate.AddDays(1), Convert.ToInt16(5), "US", _HolidayList).Date;
                    //DateTime next3Workingdate = DateExtensions.GetnextWorkingDays(dtnxtdate.AddDays(1), Convert.ToInt16(3), "US", _HolidayList).Date;

                    DateTime next10Workingdate = DateExtensions.GetnextWorkingDays(dtnxtdate, Convert.ToInt16(10), "US", _HolidayList).Date;
                    DateTime next5Workingdate = DateExtensions.GetnextWorkingDays(dtnxtdate, Convert.ToInt16(5), "US", _HolidayList).Date;
                    DateTime next2Workingdate = DateExtensions.GetnextWorkingDays(dtnxtdate, Convert.ToInt16(2), "US", _HolidayList).Date;

                    //   var datediff = (nextWorkingdate - DateTime.Now.Date).TotalDays;
                    //  if (Convert.ToInt32(datediff) > 10)
                    //if (Convert.ToInt32(datediff) == 10 || Convert.ToInt32(datediff) == 5 || Convert.ToInt32(datediff) == 3)

                    if (next10Workingdate == fdat || next5Workingdate == fdat || next2Workingdate == fdat)
                    {
                        if (next10Workingdate == fdat && dr["NotificationStatus"].ToString() == "PreliminaryNotSent")
                        {
                            datediff = 10;
                            var lstWFDetail = _wfLogic.GetForceFundingNotificationByTaskID(Convert.ToString(dr["Taskid"]));
                            DateTime fundingDate = Convert.ToDateTime(dr["Date"]);
                            if (lstWFDetail.Count > 0)
                            {
                                _notificationData = new WFNotificationDataContract();
                                _notificationData = GetInternalNotificionDetail(_wfDetailDataContract);

                                if (_notificationData != null && !string.IsNullOrEmpty(_notificationData.DealName) &&
                                    !string.IsNullOrEmpty(_notificationData.DwarApprovalList))
                                {
                                    lstWFDetail.ForEach(x =>
                                    {
                                        x.DealName = _notificationData.DealName;
                                        x.Comment = _notificationData.Comment;
                                        x.ActivityLog = _notificationData.ActivityLog;
                                        x.FooterText = _notificationData.FooterText;
                                        x.SenderName = _notificationData.SenderName;
                                        x.DwarApprovalList = _notificationData.DwarApprovalList;
                                        x.SpecialInstructions = _notificationData.SpecialInstructions;
                                        x.AdditionalComments = _notificationData.AdditionalComments;
                                        x.NoteswithAmount = _notificationData.NoteswithAmount;
                                        x.ReserveScheduleBreakDown = _notificationData.ReserveScheduleBreakDown;
                                        x.PreHeaderText = _notificationData.PreHeaderText;
                                        x.TaskTypeID = _notificationData.TaskTypeID;
                                    });


                                    _iEmailNotification.SendEmailForceFundingNotification(lstWFDetail, Convert.ToString(dr["DealName"]), fundingDate, datediff);
                                }
                            }
                        }
                        if (next5Workingdate == fdat && dr["NotificationStatus"].ToString() == "PreliminaryNotSent")
                        {
                            datediff = 5;
                            var lstWFDetail = _wfLogic.GetForceFundingNotificationByTaskID(Convert.ToString(dr["Taskid"]));
                            DateTime fundingDate = Convert.ToDateTime(dr["Date"]);
                            if (lstWFDetail.Count > 0)
                            {
                                _notificationData = new WFNotificationDataContract();
                                _notificationData = GetInternalNotificionDetail(_wfDetailDataContract);

                                if (_notificationData != null && !string.IsNullOrEmpty(_notificationData.DealName) &&
                                    !string.IsNullOrEmpty(_notificationData.DwarApprovalList))
                                {
                                    lstWFDetail.ForEach(x =>
                                    {
                                        x.DealName = _notificationData.DealName;
                                        x.Comment = _notificationData.Comment;
                                        x.ActivityLog = _notificationData.ActivityLog;
                                        x.FooterText = _notificationData.FooterText;
                                        x.SenderName = _notificationData.SenderName;
                                        x.DwarApprovalList = _notificationData.DwarApprovalList;
                                        x.SpecialInstructions = _notificationData.SpecialInstructions;
                                        x.AdditionalComments = _notificationData.AdditionalComments;
                                        x.NoteswithAmount = _notificationData.NoteswithAmount;
                                        x.ReserveScheduleBreakDown = _notificationData.ReserveScheduleBreakDown;
                                        x.PreHeaderText = _notificationData.PreHeaderText;
                                        x.TaskTypeID = _notificationData.TaskTypeID;
                                    });
                                    _iEmailNotification.SendEmailForceFundingNotification(lstWFDetail, Convert.ToString(dr["DealName"]), fundingDate, datediff);
                                }
                            }
                        }
                        if (next2Workingdate == fdat && dr["NotificationStatus"].ToString() == "FinalNotSent")
                        {
                            datediff = 2;
                            var lstWFDetail = _wfLogic.GetForceFundingNotificationByTaskID(Convert.ToString(dr["Taskid"]));
                            DateTime fundingDate = Convert.ToDateTime(dr["Date"]);
                            if (lstWFDetail.Count > 0)
                            {
                                _notificationData = new WFNotificationDataContract();
                                _notificationData = GetInternalNotificionDetail(_wfDetailDataContract);

                                if (_notificationData != null && !string.IsNullOrEmpty(_notificationData.DealName) &&
                                    !string.IsNullOrEmpty(_notificationData.DwarApprovalList))
                                {
                                    lstWFDetail.ForEach(x =>
                                    {
                                        x.DealName = _notificationData.DealName;
                                        x.Comment = _notificationData.Comment;
                                        x.ActivityLog = _notificationData.ActivityLog;
                                        x.FooterText = _notificationData.FooterText;
                                        x.SenderName = _notificationData.SenderName;
                                        x.DwarApprovalList = _notificationData.DwarApprovalList;
                                        x.SpecialInstructions = _notificationData.SpecialInstructions;
                                        x.AdditionalComments = _notificationData.AdditionalComments;
                                        x.NoteswithAmount = _notificationData.NoteswithAmount;
                                        x.ReserveScheduleBreakDown = _notificationData.ReserveScheduleBreakDown;
                                        x.PreHeaderText = _notificationData.PreHeaderText;
                                        x.TaskTypeID = _notificationData.TaskTypeID;
                                        x.DealLine = _notificationData.DealLine;

                                    });
                                    _iEmailNotification.SendEmailForceFundingNotification(lstWFDetail, Convert.ToString(dr["DealName"]), fundingDate, datediff);
                                }
                            }
                        }
                    }
                }

                return htmlContentMain;

            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }

        //logdb for testing and debuging
        public void LogDB(string message)
        {
            Microsoft.Extensions.Configuration.IConfigurationBuilder builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
            builder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
            var root = builder.Build();
            System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection();
            connection.ConnectionString = root.GetSection("Application").GetSection("ConnectionStrings").Value;
            System.Data.SqlClient.SqlCommand dbCmd = new System.Data.SqlClient.SqlCommand("dbo.TestLog", connection);
            dbCmd.CommandType = CommandType.StoredProcedure;
            dbCmd.Parameters.AddWithValue("LogMessage", message);
            connection.Open();
            dbCmd.ExecuteNonQuery();
            connection.Close();
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/wfcontroller/getdrawFeeinvoicedetailbytaskId")]
        public IActionResult GetDrawFeeInvoiceDetailByTaskID([FromBody] string taskid)
        {
            GenericResult _authenticationResult = null;
            DrawFeeInvoiceDataContract drawDC = new DrawFeeInvoiceDataContract();

            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            WFLogic _wfLogic = new WFLogic();


            drawDC = _wfLogic.GetDrawFeeInvoiceDetailByTaskID(taskid, headerUserID.ToString());

            try
            {
                if (drawDC != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        DrawFeeInvoice = drawDC,
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
                Logger.Write("Error in method GetDrawFeeInvoiceDetailByTaskID", ex);

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
        [Route("api/wfcontroller/insertupdatedrawfeeinvoice")]
        public IActionResult InsertUpdateDrawFeeInvoiceDetail([FromBody] DrawFeeInvoiceDataContract DrawFeeDC)
        {
            GenericResult _authenticationResult = null;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            try
            {
                WFLogic _wfLogic = new WFLogic();
                string res = _wfLogic.InsertUpdateDrawFeeInvoiceDetail(headerUserID, DrawFeeDC);

                if (headerUserID != null)
                {
                    if (res == "success")
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = res
                        };
                    }
                    else
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = res
                        };
                    }
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
        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/wfcontroller/getWorkFlowStatus")]
        public IActionResult GetWorkFlowStatus()
        {
            GenericResult _authenticationResult = null;

            WFLogic _wfLogic = new WFLogic();
            DataTable dt = _wfLogic.GetWorkFlowStatus();

            try
            {
                if (dt != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        dt = dt
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
        [Route("api/wfcontroller/createinvoice")]
        public IActionResult CreateInvoice([FromBody] DrawFeeInvoiceDataContract DrawFeeDC)
        {
            GenericResult _authenticationResult = null;
            GetConfigSetting();
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            try
            {
                if (string.IsNullOrEmpty(DrawFeeDC.InvoiceNo))
                {
                    WFLogic _wfLogic = new WFLogic();
                    //if draw date is greater than wire confirmed date than change invoice status to “Invoice Queued” 
                    //and the Draw Fee invoice will be sent at 5 PM EST on the Funding Date.   
                    if (DrawFeeDC.FundingDate.Date >= DrawFeeDC.UpdatedDate.Date)
                    {
                        DrawFeeDC.DrawFeeStatus = 696;
                        if (DrawFeeDC.DrawFeeInvoiceDetailID == 0 || string.IsNullOrEmpty(DrawFeeDC.FirstName))
                        {
                            DrawFeeInvoiceDataContract drawDC = new DrawFeeInvoiceDataContract();
                            drawDC = _wfLogic.GetDrawFeeInvoiceDetailByTaskID(DrawFeeDC.TaskID.ToString(), headerUserID.ToString());
                            DrawFeeDC.DrawFeeInvoiceDetailID = drawDC.DrawFeeInvoiceDetailID;
                        }
                        DrawFeeDC.IsLogActivity = true;
                        _wfLogic.UpdateDrawFeeInvoiceDetailStatus(headerUserID, DrawFeeDC);
                    }
                    else
                    {
                        //if draw date is less than wire confirmed date than 
                        //Draw Fee will be sent right away, and the Draw Fee Invoice status will changed to “Invoiced”
                        DrawFeeDC = CreateInvoiceInQBD(DrawFeeDC);
                        if (DrawFeeDC.IsSuccess)
                        {
                            DrawFeeDC.DrawFeeStatus = 693;
                            DrawFeeDC.IsLogActivity = true;
                            _wfLogic = new WFLogic();
                            _wfLogic.UpdateDrawFeeInvoiceDetailStatus(headerUserID, DrawFeeDC);
                            //DrawFeeDC.InvoiceNo = jsonOutputResult.Contents[0].Object.Number;
                            //DrawFeeDC.InvoiceNoUI = DrawFeeDC.CreDealID + "-DR-" + DrawFeeDC.DrawNo;
                            string pdfFileName = CreateJsonToPDF(DrawFeeDC);
                            DrawFeeDC.FileName = pdfFileName;
                            DrawFeeDC.IsLogActivity = false;
                            _wfLogic.UpdateDrawFeeInvoiceDetailStatus(headerUserID, DrawFeeDC);
                            //send draw fee invoice to email
                            AttachInvoiceSendEmail(DrawFeeDC);
                        }
                        else//if creating invoice is not successfull then put it in the queue(update invoice status as invoice queued)
                        {
                            DrawFeeDC.DrawFeeStatus = 696;
                            if (DrawFeeDC.DrawFeeInvoiceDetailID == 0 || string.IsNullOrEmpty(DrawFeeDC.FirstName))
                            {
                                DrawFeeInvoiceDataContract drawDC = new DrawFeeInvoiceDataContract();
                                drawDC = _wfLogic.GetDrawFeeInvoiceDetailByTaskID(DrawFeeDC.TaskID.ToString(), headerUserID.ToString());
                                DrawFeeDC.DrawFeeInvoiceDetailID = drawDC.DrawFeeInvoiceDetailID;
                            }
                            DrawFeeDC.IsLogActivity = true;
                            _wfLogic.UpdateDrawFeeInvoiceDetailStatus(headerUserID, DrawFeeDC);
                        }
                    }
                }
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Success"
                };
            }
            catch (Exception ex)
            {

                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = ex.Message
                };


                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), "", ex.TargetSite.Name.ToString(), "", ex);

                EmailDataContract emaildc = new EmailDataContract();
                emaildc.ModuleId = 705;
                emaildc.Subject = "Quickbook Invoice Error Notification";
                emaildc.Body = "Error occurred while creating invoice in QBD for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString() + " - " + ex.Message;
                _iEmailNotification.SendErrorNotificationEmail(emaildc);
            }

            return Ok(_authenticationResult);
        }

        IConfigurationSection Sectionroot = null;
        public void GetConfigSetting()
        {
            if (Sectionroot == null)
            {
                IConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
                var root = builder.Build();
                Sectionroot = root.GetSection("Application");
            }
        }

        IConfigurationRoot configroot = null;
        public void GetConfigRoot()
        {
            if (configroot == null)
            {
                IConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
                configroot = builder.Build();
            }
        }

        //create json to pdf file using html template
        public string CreateJsonToPDF(DrawFeeInvoiceDataContract DrawFeeDC)
        {
            GetConfigSetting();
            //FundingDate(MMDDYYYY)_DealName_ItemCode_ItemDescription_Amount.pdf
            string Location = Sectionroot.GetSection("storage:invoiceStorageLocation").Value; ;
            string strPaymentDate = Convert.ToString(DrawFeeDC.FundingDate.ToString("MMddyyyy"));
            //string fileName = strPaymentDate + "_" + DrawFeeDC.DealName.Replace(" ","") + "_" + "DrawFee" + "_" + DrawFeeDC.DrawNo.Replace(" ", "").Replace("#","") + "_" + Convert.ToDecimal(DrawFeeDC.Amount).ToString("#.##") + ".pdf";
            //string fileName = "ACORE Fee Invoice – " + DrawFeeDC.DealName + " - "  + DrawFeeDC.DrawNo.Replace("#","") + ".pdf";

            Regex illegalInFileName = new Regex(@"[\\/:*?""<>|]");
            string DealNameWithoutSpecialChar = illegalInFileName.Replace(DrawFeeDC.DealName, "-");
            DealNameWithoutSpecialChar = Regex.Replace(DealNameWithoutSpecialChar, @"[^0-9a-zA-Z -]+", "-");

            string fileName = "ACORE Draw Fee Invoice – " + DealNameWithoutSpecialChar + " - " + DrawFeeDC.InvoiceNoUI + ".pdf";
            //392-AzureBlob, 459-Box, 641-LocalServer, 642-FTPServer

            var invoiceStorageTypeId = Sectionroot.GetSection("storage:invoiceStorageTypeId").Value;

            // string fileName = "DrawFeeInvoice_" + DateTime.Now.ToString("MMddyyyy_HHmmss")+ ".pdf";
            string filepath = Directory.GetCurrentDirectory() + @"\wwwroot\InvoiceTemplate\DrawFeePDF.html";
            if (invoiceStorageTypeId == "392")
            {
                using (MemoryStream stream = new System.IO.MemoryStream())
                {
                    //Create document
                    Document doc = new Document();
                    //string FirstName = "pushp singh";
                    string sr = string.Empty;
                    using (StreamReader reader = new StreamReader(filepath))
                    {
                        sr = reader.ReadToEnd();
                    }

                    if (sr.Length > 0)
                    {
                        sr = sr.Replace("{fullname}", (DrawFeeDC.FirstName + " " + DrawFeeDC.LastName).Trim());
                        sr = sr.Replace("{feeamount}", String.Format("{0:C}", DrawFeeDC.Amount));
                        sr = sr.Replace("{fundingdate}", DrawFeeDC.FundingDate.ToString());
                        sr = sr.Replace("{invoiceno}", DrawFeeDC.InvoiceNoUI);
                        sr = sr.Replace("{qbdinvoiceno}", DrawFeeDC.InvoiceNo.ToString());
                        sr = sr.Replace("{wirereference}", DrawFeeDC.DealName + "-" + DrawFeeDC.DrawNo + " (Invoice#:" + DrawFeeDC.InvoiceNoUI + ")");
                        string add = "";
                        if (!string.IsNullOrEmpty(DrawFeeDC.Designation))
                            add = add + DrawFeeDC.Designation + "<br />";
                        if (!string.IsNullOrEmpty(DrawFeeDC.CompanyName))
                            add = add + DrawFeeDC.CompanyName + "<br />";
                        if (!string.IsNullOrEmpty(DrawFeeDC.Email1))
                            add = add + DrawFeeDC.Email1 + "<br />";
                        //if (!string.IsNullOrEmpty(DrawFeeDC.Email2))
                        //    add = add + DrawFeeDC.Email2 + "<br />";

                        add = add + DrawFeeDC.Address + "< br />" + DrawFeeDC.City + ", " + DrawFeeDC.State + " " + DrawFeeDC.Zip + "<br />";
                        if (!string.IsNullOrEmpty(DrawFeeDC.PhoneNo))
                            add = add + DrawFeeDC.PhoneNo + "<br />";
                        if (!string.IsNullOrEmpty(DrawFeeDC.AlternatePhone))
                            add = add + DrawFeeDC.AlternatePhone + "<br />";

                        sr = sr.Replace("{address}", add);
                        sr = sr.Replace("{firstname}", DrawFeeDC.FirstName);
                        sr = sr.Replace("{dealandcomment}", DrawFeeDC.DealName + " - " + DrawFeeDC.DrawNo);
                        sr = sr.Replace("{memo}", DrawFeeDC.InvoiceNoUI);
                        sr = sr.Replace("{currdate}", DateTime.Now.ToString("MMMM d, yyyy"));


                    }

                    StringReader htmlContent = new StringReader(sr);
                    //Without saving local pdf

                    var Container = Sectionroot.GetSection("storage:container:name").Value;
                    CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);
                    CloudBlobDirectory blobDirectory = container.GetDirectoryReference(Location);
                    CloudBlockBlob blockBlob = blobDirectory.GetBlockBlobReference(fileName);


                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (var docm = new iTextSharp.text.Document())
                        {
                            PdfWriter writer = PdfWriter.GetInstance(docm, ms);
                            docm.Open();
                            XMLWorkerHelper.GetInstance().ParseXHtml(writer, docm, htmlContent);
                        }
                        var byteArray = ms.ToArray();

                        blockBlob.Properties.ContentType = "application/pdf";
                        blockBlob.UploadFromByteArray(byteArray, 0, byteArray.Length);
                    }
                }


                return fileName;
            }
            else
            {


                using (MemoryStream stream = new System.IO.MemoryStream())
                {
                    //Create document
                    Document doc = new Document();
                    // string FirstName = "pushp singh";
                    string sr = string.Empty;
                    using (StreamReader reader = new StreamReader(filepath))
                    {
                        sr = reader.ReadToEnd();
                    }

                    if (sr.Length > 0)
                    {
                        sr = sr.Replace("{fullname}", (DrawFeeDC.FirstName + " " + DrawFeeDC.LastName).Trim());
                        sr = sr.Replace("{feeamount}", String.Format("{0:C}", DrawFeeDC.Amount));
                        sr = sr.Replace("{fundingdate}", DrawFeeDC.FundingDate.ToString());
                        sr = sr.Replace("{invoiceno}", DrawFeeDC.InvoiceNoUI);
                        sr = sr.Replace("{qbdinvoiceno}", DrawFeeDC.InvoiceNo.ToString());
                        sr = sr.Replace("{wirereference}", DrawFeeDC.DealName + "-" + DrawFeeDC.DrawNo + " (Invoice#:" + DrawFeeDC.InvoiceNoUI + ")");
                        string add = "";
                        if (!string.IsNullOrEmpty(DrawFeeDC.Designation))
                            add = add + DrawFeeDC.Designation + "<br />";
                        if (!string.IsNullOrEmpty(DrawFeeDC.CompanyName))
                            add = add + DrawFeeDC.CompanyName + "<br />";
                        if (!string.IsNullOrEmpty(DrawFeeDC.Email1))
                            add = add + DrawFeeDC.Email1 + "<br />";
                        //if (!string.IsNullOrEmpty(DrawFeeDC.Email2))
                        //    add = add + DrawFeeDC.Email2 + "<br />";
                        add = add + DrawFeeDC.Address + "< br />" + DrawFeeDC.City + ", " + DrawFeeDC.State + " " + DrawFeeDC.Zip + "<br />";
                        if (!string.IsNullOrEmpty(DrawFeeDC.PhoneNo))
                            add = add + DrawFeeDC.PhoneNo + "<br />";
                        if (!string.IsNullOrEmpty(DrawFeeDC.AlternatePhone))
                            add = add + DrawFeeDC.AlternatePhone + "<br />";

                        sr = sr.Replace("{address}", add);
                        sr = sr.Replace("{firstname}", DrawFeeDC.FirstName);
                        sr = sr.Replace("{dealandcomment}", DrawFeeDC.DealName + " - " + DrawFeeDC.DrawNo);
                        sr = sr.Replace("{memo}", DrawFeeDC.InvoiceNoUI);
                        sr = sr.Replace("{currdate}", DateTime.Now.ToString("MMMM d, yyyy"));


                    }

                    StringReader htmlContent = new StringReader(sr);

                    DocumentDataContract _docDC = new DocumentDataContract();
                    MemoryStream ms = new MemoryStream();
                    //using (MemoryStream ms = new MemoryStream())
                    //{
                    using (var docm = new iTextSharp.text.Document())
                    {
                        PdfWriter writer = PdfWriter.GetInstance(docm, ms);
                        writer.CloseStream = false;
                        docm.Open();
                        XMLWorkerHelper.GetInstance().ParseXHtml(writer, docm, htmlContent);
                    }

                    _docDC.FileName = fileName;
                    // _docDC.Storagetype = "Box";
                    _docDC.FolderName = Location;

                    //    var DocumentStorageID =  new BoxHelper().UploadFileToFolder("", _docDC, ms);
                    var DocumentStorageID = new BoxHelper().UploadFileToFolder("", _docDC, ms).GetAwaiter().GetResult();
                    fileName = DocumentStorageID.ToString();


                    //fileName = DocumentStorageID.ToString();
                    //  }


                }
                return fileName;
            }
        }
        //create json to pdf file using html template
        public string CreateJsonToPDFForBatchUpload(DrawFeeInvoiceDataContract DrawFeeDC)
        {
            GetConfigSetting();
            //FundingDate(MMDDYYYY)_DealName_ItemCode_ItemDescription_Amount.pdf
            string Location = Sectionroot.GetSection("storage:invoiceStorageLocation").Value; ;
            string strPaymentDate = Convert.ToString(DrawFeeDC.FundingDate.ToString("MMddyyyy"));
            //string fileName = strPaymentDate + "_" + DrawFeeDC.DealName.Replace(" ","") + "_" + "DrawFee" + "_" + DrawFeeDC.DrawNo.Replace(" ", "").Replace("#","") + "_" + Convert.ToDecimal(DrawFeeDC.Amount).ToString("#.##") + ".pdf";
            //string fileName = "ACORE Fee Invoice – " + DrawFeeDC.DealName + " - "  + DrawFeeDC.DrawNo.Replace("#","") + ".pdf";

            Regex illegalInFileName = new Regex(@"[\\/:*?""<>|]");
            string DealNameWithoutSpecialChar = illegalInFileName.Replace(DrawFeeDC.DealName, "-");
            string InvoiceTypeNameWithoutSpecialChar = illegalInFileName.Replace(DrawFeeDC.InvoiceTypeName, "-");
            string fileName = "ACORE " + InvoiceTypeNameWithoutSpecialChar + " Invoice – " + DealNameWithoutSpecialChar + " - " + DrawFeeDC.InvoiceNoUI + ".pdf";
            //392-AzureBlob, 459-Box, 641-LocalServer, 642-FTPServer

            var invoiceStorageTypeId = Sectionroot.GetSection("storage:invoiceStorageTypeId").Value;

            // string fileName = "DrawFeeInvoice_" + DateTime.Now.ToString("MMddyyyy_HHmmss")+ ".pdf";
            string filepath = Directory.GetCurrentDirectory() + @"\wwwroot\InvoiceTemplate\InvoicePDF.html";
            if (invoiceStorageTypeId == "392")
            {
                using (MemoryStream stream = new System.IO.MemoryStream())
                {
                    //Create document
                    Document doc = new Document();
                    //string FirstName = "pushp singh";
                    string sr = string.Empty;
                    using (StreamReader reader = new StreamReader(filepath))
                    {
                        sr = reader.ReadToEnd();
                    }

                    if (sr.Length > 0)
                    {
                        sr = sr.Replace("{feename}", InvoiceTypeNameWithoutSpecialChar.Trim());
                        sr = sr.Replace("{fullname}", (DrawFeeDC.FirstName + " " + DrawFeeDC.LastName).Trim());
                        sr = sr.Replace("{feeamount}", String.Format("{0:C}", DrawFeeDC.Amount));
                        sr = sr.Replace("{fundingdate}", DrawFeeDC.FundingDate.ToString());
                        sr = sr.Replace("{invoiceno}", DrawFeeDC.InvoiceNoUI);
                        sr = sr.Replace("{qbdinvoiceno}", DrawFeeDC.InvoiceNo.ToString());
                        sr = sr.Replace("{wirereference}", DrawFeeDC.DealName + " (Invoice#:" + DrawFeeDC.InvoiceNoUI + ")");
                        string add = "";
                        if (!string.IsNullOrEmpty(DrawFeeDC.Designation))
                            add = add + DrawFeeDC.Designation + "<br />";
                        if (!string.IsNullOrEmpty(DrawFeeDC.CompanyName))
                            add = add + DrawFeeDC.CompanyName + "<br />";
                        if (!string.IsNullOrEmpty(DrawFeeDC.Email1))
                            add = add + DrawFeeDC.Email1 + "<br />";
                        //if (!string.IsNullOrEmpty(DrawFeeDC.Email2))
                        //    add = add + DrawFeeDC.Email2 + "<br />";

                        add = add + DrawFeeDC.Address + "< br />" + DrawFeeDC.City + ", " + DrawFeeDC.State + " " + DrawFeeDC.Zip + "<br />";
                        if (!string.IsNullOrEmpty(DrawFeeDC.PhoneNo))
                            add = add + DrawFeeDC.PhoneNo + "<br />";
                        if (!string.IsNullOrEmpty(DrawFeeDC.AlternatePhone))
                            add = add + DrawFeeDC.AlternatePhone + "<br />";

                        sr = sr.Replace("{address}", add);
                        sr = sr.Replace("{firstname}", DrawFeeDC.FirstName);
                        sr = sr.Replace("{dealandcomment}", DrawFeeDC.DealName);
                        sr = sr.Replace("{memo}", DrawFeeDC.InvoiceNoUI);
                        sr = sr.Replace("{currdate}", DateTime.Now.ToString("MMMM d, yyyy"));
                        if (DrawFeeDC.InvoiceTypeID != 558)
                        {

                            sr = sr.Replace("{invoicecomment}", string.IsNullOrEmpty(DrawFeeDC.InvoiceComment) ? "" : " - " + DrawFeeDC.InvoiceComment);

                        }
                        else
                        {
                            sr = sr.Replace("{invoicecomment}", " - In accordance with Loan Agreement");

                        }

                    }

                    StringReader htmlContent = new StringReader(sr);
                    //Without saving local pdf

                    var Container = Sectionroot.GetSection("storage:container:name").Value;
                    CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);
                    CloudBlobDirectory blobDirectory = container.GetDirectoryReference(Location);
                    CloudBlockBlob blockBlob = blobDirectory.GetBlockBlobReference(fileName);


                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (var docm = new iTextSharp.text.Document())
                        {
                            PdfWriter writer = PdfWriter.GetInstance(docm, ms);
                            docm.Open();
                            XMLWorkerHelper.GetInstance().ParseXHtml(writer, docm, htmlContent);
                        }
                        var byteArray = ms.ToArray();

                        blockBlob.Properties.ContentType = "application/pdf";
                        blockBlob.UploadFromByteArray(byteArray, 0, byteArray.Length);
                    }
                }


                return fileName;
            }
            else
            {


                using (MemoryStream stream = new System.IO.MemoryStream())
                {
                    //Create document
                    Document doc = new Document();
                    // string FirstName = "pushp singh";
                    string sr = string.Empty;
                    using (StreamReader reader = new StreamReader(filepath))
                    {
                        sr = reader.ReadToEnd();
                    }

                    if (sr.Length > 0)
                    {
                        sr = sr.Replace("{feename}", InvoiceTypeNameWithoutSpecialChar.Trim());
                        sr = sr.Replace("{fullname}", (DrawFeeDC.FirstName + " " + DrawFeeDC.LastName).Trim());
                        sr = sr.Replace("{feeamount}", String.Format("{0:C}", DrawFeeDC.Amount));
                        sr = sr.Replace("{fundingdate}", DrawFeeDC.FundingDate.ToString());
                        sr = sr.Replace("{invoiceno}", DrawFeeDC.InvoiceNoUI);
                        sr = sr.Replace("{qbdinvoiceno}", DrawFeeDC.InvoiceNo.ToString());
                        sr = sr.Replace("{wirereference}", DrawFeeDC.DealName + " (Invoice#:" + DrawFeeDC.InvoiceNoUI + ")");
                        string add = "";
                        if (!string.IsNullOrEmpty(DrawFeeDC.Designation))
                            add = add + DrawFeeDC.Designation + "<br />";
                        if (!string.IsNullOrEmpty(DrawFeeDC.CompanyName))
                            add = add + DrawFeeDC.CompanyName + "<br />";
                        if (!string.IsNullOrEmpty(DrawFeeDC.Email1))
                            add = add + DrawFeeDC.Email1 + "<br />";
                        //if (!string.IsNullOrEmpty(DrawFeeDC.Email2))
                        //    add = add + DrawFeeDC.Email2 + "<br />";
                        add = add + DrawFeeDC.Address + "< br />" + DrawFeeDC.City + ", " + DrawFeeDC.State + " " + DrawFeeDC.Zip + "<br />";
                        if (!string.IsNullOrEmpty(DrawFeeDC.PhoneNo))
                            add = add + DrawFeeDC.PhoneNo + "<br />";
                        if (!string.IsNullOrEmpty(DrawFeeDC.AlternatePhone))
                            add = add + DrawFeeDC.AlternatePhone + "<br />";

                        sr = sr.Replace("{address}", add);
                        sr = sr.Replace("{firstname}", DrawFeeDC.FirstName);
                        sr = sr.Replace("{dealandcomment}", DrawFeeDC.DealName);
                        sr = sr.Replace("{memo}", DrawFeeDC.InvoiceNoUI);
                        sr = sr.Replace("{currdate}", DateTime.Now.ToString("MMMM d, yyyy"));


                    }

                    StringReader htmlContent = new StringReader(sr);

                    DocumentDataContract _docDC = new DocumentDataContract();
                    MemoryStream ms = new MemoryStream();
                    //using (MemoryStream ms = new MemoryStream())
                    //{
                    using (var docm = new iTextSharp.text.Document())
                    {
                        PdfWriter writer = PdfWriter.GetInstance(docm, ms);
                        writer.CloseStream = false;
                        docm.Open();
                        XMLWorkerHelper.GetInstance().ParseXHtml(writer, docm, htmlContent);
                    }

                    _docDC.FileName = fileName;
                    // _docDC.Storagetype = "Box";
                    _docDC.FolderName = Location;


                    //    var DocumentStorageID =  new BoxHelper().UploadFileToFolder("", _docDC, ms);
                    var DocumentStorageID = new BoxHelper().UploadFileToFolder("", _docDC, ms).GetAwaiter().GetResult();

                    fileName = DocumentStorageID.ToString();
                    //  }


                }
                return fileName;
            }
        }
        public void AttachInvoiceSendEmail(DrawFeeInvoiceDataContract DrawFeeDC)
        {
            GetConfigSetting();
            string Location = Sectionroot.GetSection("storage:invoiceStorageLocation").Value;
            var invoiceStorageTypeId = Sectionroot.GetSection("storage:invoiceStorageTypeId").Value;
            MemoryStream memStreamDownloaded = new MemoryStream();
            if (invoiceStorageTypeId == "392")
            {

                var Container = Sectionroot.GetSection("storage:container:name").Value;
                var accountName = Sectionroot.GetSection("storage:account:name").Value;
                var accountKey = Sectionroot.GetSection("storage:account:key").Value;
                var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer excelContainer = blobClient.GetContainerReference(Container);
                CloudBlobDirectory dr = excelContainer.GetDirectoryReference(Location);
                CloudBlockBlob cloudBlockBlob = dr.GetBlockBlobReference(DrawFeeDC.FileName);
                cloudBlockBlob.DownloadToStream(memStreamDownloaded);
                // MemoryStream memStream = new MemoryStream(memStreamDownloaded.ToArray());
            }
            else
            {
                DocumentDataContract _docDC = new DocumentDataContract();
                _docDC.FolderName = Location;
                _docDC.FileName = DrawFeeDC.FileName;


                var memStreamDown = new BoxHelper().DownloadFile(_docDC);
                // MemoryStream memStream = new MemoryStream(memStreamDown.Result.ToArray());
                memStreamDownloaded = new MemoryStream(memStreamDown.Result.ToArray());
            }
            DrawFeeDC.filestream = memStreamDownloaded;
            _iEmailNotification.SendInvoiceNotification(DrawFeeDC);

            //var objMailMessgae = new MailMessage();

            //objMailMessgae.From = new MailAddress("no-reply@m61systems.com", "M61 Support");

            //if (!string.IsNullOrEmpty(DrawFeeDC.Email1))
            //{
            //    objMailMessgae.To.Add(DrawFeeDC.Email1);
            //}
            //if (!string.IsNullOrEmpty(DrawFeeDC.Email2))
            //{
            //    //objMailMessgae.To.Add(DrawFeeDC.Email2);
            //  foreach (var emailcc in DrawFeeDC.Email2.Replace(" ","").Split(','))
            //    {
            //        if (!string.IsNullOrEmpty(emailcc))
            //            objMailMessgae.CC.Add(emailcc);
            //    }
            //}

            //objMailMessgae.Body = "Hello,<br /> Please find attached invoice file.<br />" +
            //    "<p style=font-size:12px><strong>Note:</strong> This is a system generated email.</p>"+
            //          "<hr style = font-size:12px/>"+
            //           "<p style = font-size:12px>"+
            //                "ACORE Capital Email Notice: This e-mail communication is intended only for the addressee(s) named above and any others who have been specifically authorized to receive it and may contain information that is privileged, confidential or otherwise protected from disclosure.If you are not the intended recipient of this e-mail communication, please do not copy, use or disclose to others the contents of this communication.Please notify the sender that you have received this e-mail in error by replying to this e-mail.Please then delete the e-mail from your system and any copies of it.No confidentiality or privilege is waived or lost by any transmission errors.This communication does not constitute an offer to sell or a solicitation of an offer to purchase any interest in any investment vehicles managed or advised by ACORE Capital.ACORE Capital does not provide tax, accounting, or legal advice to our clients, and all investors are advised to consult with their tax, accounting, or legal advisers regarding any potential investment."+
            //            "</p>";

            //    objMailMessgae.Subject = "Invoice Detail - "+ DrawFeeDC.InvoiceNoUI;
            //objMailMessgae.IsBodyHtml = true;
            //memStreamDownloaded.Position = 0;
            //objMailMessgae.Attachments.Add(new System.Net.Mail.Attachment(memStreamDownloaded, DrawFeeDC.FileName, "pdf/application"));
            //EmailSettings emailsetting = new EmailSettings();
            //emailsetting.Host = "smtp.gmail.com";
            //emailsetting.UserName = "no-reply@m61systems.com";
            //emailsetting.Password = "F1ght0n#";
            //emailsetting.Port = "587";
            //var smtp = new SmtpClient();
            //smtp.Host = "smtp.gmail.com";
            //smtp.EnableSsl = true;
            //NetworkCredential NetworkCred = new NetworkCredential(emailsetting.UserName, emailsetting.Password);
            //smtp.UseDefaultCredentials = true;
            //smtp.Credentials = NetworkCred;
            //smtp.Port = Convert.ToInt32(emailsetting.Port);

            //ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            //{ return true; };
            //smtp.Send(objMailMessgae);

        }


        [HttpGet]
        [Route("api/wfcontroller/updatedrawfeeinvoicestatus")]
        public IActionResult UpdateDrawFeeInvoiceStatus()
        {

            GenericResult _authenticationResult = null;
            WFLogic _wfLogic = new WFLogic();
            List<DrawFeeInvoiceDataContract> lstDraw = new List<DrawFeeInvoiceDataContract>();
            DrawFeeInvoiceDataContract DrawFeeDC = null;

            try
            {
                GetConfigSetting();

                lstDraw = _wfLogic.GetAllPendingInvoice("");
                if (!IsUseDynamicForInvoice())
                {
                    //call invoice api and update status
                    HttpClient _httpClient = new HttpClient();
                    string Autofykey = Sectionroot.GetSection("Autofykey").Value;
                    string SchemaVersion = Sectionroot.GetSection("SchemaVersion").Value;
                    string AutofyBaseURL = Sectionroot.GetSection("AutofyBaseURL").Value;
                    string AutofyCompanyEndPoint = Sectionroot.GetSection("AutofyCompanyEndPoint").Value;
                    _httpClient.DefaultRequestHeaders.Add("x-api-key", Autofykey);
                    _httpClient.DefaultRequestHeaders.Add("SchemaVersion", "20.04");
                    foreach (DrawFeeInvoiceDataContract df in lstDraw)
                    {
                        try
                        {
                            string AutoFURL = AutofyBaseURL + "data/" + AutofyCompanyEndPoint + "/Invoice?number=" + df.InvoiceNo;
                            HttpResponseMessage result = _httpClient.GetAsync(AutoFURL).Result;
                            if (result.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                string resultString = result.Content.ReadAsStringAsync().Result;
                                var jsonOutputResult = JsonConvert.DeserializeObject<AutofyInvoiceOutputDataContract>(resultString);
                                if (string.IsNullOrEmpty(jsonOutputResult.HasErrors) || jsonOutputResult.HasErrors == "false")
                                {
                                    if (jsonOutputResult.Contents.Count > 0)
                                    {

                                        DrawFeeDC = new DrawFeeInvoiceDataContract();
                                        DrawFeeDC.DrawFeeInvoiceDetailID = df.DrawFeeInvoiceDetailID;
                                        DrawFeeDC.DrawFeeStatus = df.DrawFeeStatus;
                                        if (jsonOutputResult.Contents[0].Object.IsPaid == true)
                                        {
                                            DrawFeeDC.DrawFeeStatus = 694;
                                            DrawFeeDC.IsLogActivity = true;
                                        }
                                        //DrawFeeDC.PaymentDate = jsonOutputResult.Contents[0].Object.LastModifiedDate;
                                        if (jsonOutputResult.Contents[0].Object.LinkedTransactions != null && jsonOutputResult.Contents[0].Object.LinkedTransactions.Count > 0)
                                        {
                                            DrawFeeDC.AmountPaid = -(jsonOutputResult.Contents[0].Object.LinkedTransactions.Sum(x => x.Amount));
                                            DrawFeeDC.PaymentDate = jsonOutputResult.Contents[0].Object.LinkedTransactions.OrderByDescending(x => x.Date).FirstOrDefault().Date;
                                        }
                                        _wfLogic.UpdateDrawFeeInvoiceDetailStatus("", DrawFeeDC);
                                    }
                                    //update status
                                }
                            }
                        }
                        catch (Exception ex) { }

                    }
                }
                else
                {
                    GetConfigRoot();
                    string authToken = GetBusinessCentralAccessToken();
                    DynamicsSalesInvoiceOutput invoiceoutput = null;
                    foreach (DrawFeeInvoiceDataContract df in lstDraw)
                    {
                        try
                        {
                            invoiceoutput = GetInvoiceDetailDynamicByGuid(df.InvoiceGuid, authToken);
                            if (!invoiceoutput.IsError)
                            {
                                if (invoiceoutput.status == "Paid")
                                {
                                    DrawFeeDC = new DrawFeeInvoiceDataContract();
                                    DrawFeeDC.DrawFeeInvoiceDetailID = df.DrawFeeInvoiceDetailID;
                                    DrawFeeDC.DrawFeeStatus = 694;
                                    DrawFeeDC.IsLogActivity = true;
                                    DrawFeeDC.PaymentDate = invoiceoutput.lastModifiedDateTime;
                                    DrawFeeDC.InvoiceNo = invoiceoutput.number;
                                    DrawFeeDC.AmountPaid = (invoiceoutput.totalAmountIncludingTax - invoiceoutput.remainingAmount);
                                    _wfLogic.UpdateDrawFeeInvoiceDetailStatus("", DrawFeeDC);
                                }
                            }
                        }
                        catch (Exception ex) { }
                    }


                }



            }
            catch (Exception ex)
            {

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while updating invoice status in QBD from scheduler for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), "", ex.TargetSite.Name.ToString(), "", ex);

            }
            return Ok(_authenticationResult);

        }

        [HttpGet]
        [Route("api/wfcontroller/updateinvoicequeuedtoinvoiced")]
        public IActionResult UpdateInvoiceQueuedToInvoiced()
        {

            GenericResult _authenticationResult = null;
            WFLogic _wfLogic = new WFLogic();
            List<DrawFeeInvoiceDataContract> lstDraw = new List<DrawFeeInvoiceDataContract>();
            DrawFeeInvoiceDataContract DrawFeeDC = null;

            try
            {

                GetConfigSetting();
                CreateMissingQBDCustomer();
                lstDraw = _wfLogic.UpdateAndGetAllInvoiceQueued("");

                //call invoice api and update status of invoice
                foreach (DrawFeeInvoiceDataContract df in lstDraw)
                {
                    try
                    {
                        DrawFeeDC = df;
                        // AttachInvoiceSendEmail(df);
                        DrawFeeDC = CreateInvoiceInQBD(df);
                        if (DrawFeeDC.IsSuccess)
                        {
                            DrawFeeDC.DrawFeeStatus = 693;
                            DrawFeeDC.IsLogActivity = true;
                            _wfLogic.UpdateDrawFeeInvoiceDetailStatus("", DrawFeeDC);
                            //DrawFeeDC.InvoiceNo = jsonOutputResult.Contents[0].Object.Number;
                            //DrawFeeDC.InvoiceNoUI = DrawFeeDC.CreDealID + "-DR-" + DrawFeeDC.DrawNo;
                            string pdfFileName = CreateJsonToPDF(DrawFeeDC);
                            DrawFeeDC.FileName = pdfFileName;
                            DrawFeeDC.IsLogActivity = false;
                            _wfLogic.UpdateDrawFeeInvoiceDetailStatus("", DrawFeeDC);
                            //send draw fee invoice to email
                            AttachInvoiceSendEmail(DrawFeeDC);
                        }
                    }
                    catch (Exception ex)
                    {

                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in QBD from scheduler for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), "", ex.TargetSite.Name.ToString(), "", ex);

                        EmailDataContract emaildc = new EmailDataContract();
                        emaildc.ModuleId = 705;
                        emaildc.Subject = "Quickbook Invoice Error Notification";
                        emaildc.Body = "Error occurred while creating invoice in QBD from scheduler for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString() + " - " + ex.Message;
                        _iEmailNotification.SendErrorNotificationEmail(emaildc);
                    }

                }
            }
            catch (Exception ex)
            {

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in QBD from scheduler", "", "", ex.TargetSite.Name.ToString(), "", ex);

            }
            ProcessInvoiceUploadedByBatch();
            return Ok(_authenticationResult);

        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/wfcontroller/getAllFeeInvoice")]
        public IActionResult GetAllFeeInvoice([FromBody] string DealID, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;


            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            WFLogic wfLogic = new WFLogic();
            int? totalCount = 0;


            DataTable dt = wfLogic.GetAllFeeInvoice(headerUserID, DealID, pageSize, pageIndex, out totalCount);

            try
            {
                if (dt != null)
                {
                    Logger.Write("Workflow list loaded successfully", MessageLevel.Info, headerUserID.ToString());
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        dt = dt,

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
                Logger.Write("Error in loading all deal detail", ex);

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
        [Route("api/wfcontroller/checkqbdcompanycustomer")]
        public IActionResult CheckQBDCompanyCustomer([FromBody] DrawFeeInvoiceDataContract drawFeeDC)
        {
            GenericResult _authenticationResult = null;
            DrawFeeInvoiceDataContract drawDC = new DrawFeeInvoiceDataContract();
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            WFLogic _wfLogic = new WFLogic();
            drawFeeDC.DealName = CommonHelper.FormatCustomerForQuickBook(drawFeeDC.DealName, drawFeeDC.CreDealID);
            drawDC = _wfLogic.CheckQBDCompanyCustomer(drawFeeDC);
            try
            {
                if (drawDC != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        DrawFeeInvoice = drawDC,
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
        [Route("api/wfcontroller/createqbdcustomer")]
        public IActionResult CreateQBDCustomer([FromBody] DrawFeeInvoiceDataContract drawFeeDC)
        {
            GenericResult _authenticationResult = null;
            DrawFeeInvoiceDataContract drawDC = new DrawFeeInvoiceDataContract();
            WFLogic wfLogic = new WFLogic();
            var headerUserID = new Guid();
            try
            {
                if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                {
                    headerUserID = new Guid(Request.Headers["TokenUId"]);
                }
            }
            catch { }
            if (!IsUseDynamicForInvoice())
            {
                GetConfigSetting();
                try
                {
                    QBDCustomerInputDataContract qbdcustomer = new QBDCustomerInputDataContract();
                    qbdcustomer.FirstName = drawFeeDC.FirstName;
                    qbdcustomer.LastName = drawFeeDC.LastName;
                    qbdcustomer.CompanyName = drawFeeDC.CompanyName;
                    //format customer name
                    qbdcustomer.Name = CommonHelper.FormatCustomerForQuickBook(drawFeeDC.DealName, drawFeeDC.CreDealID);
                    qbdcustomer.FullName = qbdcustomer.Name;
                    qbdcustomer.CCEmail = drawFeeDC.EmailCC;
                    qbdcustomer.BillingAddress = new QBDCustomerBillingAddress();
                    //qbdcustomer.BillingAddress.Line1 = drawFeeDC.Address;
                    qbdcustomer.BillingAddress = FormateQBDCustomerAddress(drawFeeDC.Address);
                    qbdcustomer.BillingAddress.City = drawFeeDC.City;
                    qbdcustomer.BillingAddress.State = drawFeeDC.State;
                    qbdcustomer.BillingAddress.PostalCode = drawFeeDC.Zip;

                    //call customer api

                    string inputjsonstring = JsonConvert.SerializeObject(qbdcustomer);


                    // var jsonInputResult = JsonConvert.DeserializeObject<AutofyInvoiceInputDataContract>(jsonInput);
                    HttpClient _httpClient = new HttpClient();
                    //using (var content = new StringContent(JsonConvert.SerializeObject(jsonInput), System.Text.Encoding.UTF8, "application/json"))
                    using (var content = new StringContent(JsonConvert.SerializeObject(inputjsonstring), System.Text.Encoding.UTF8, "application/json"))
                    {
                        string Autofykey = Sectionroot.GetSection("Autofykey").Value;
                        string SchemaVersion = Sectionroot.GetSection("SchemaVersion").Value;
                        string AutofyBaseURL = Sectionroot.GetSection("AutofyBaseURL").Value;
                        string AutofyCompanyEndPoint = Sectionroot.GetSection("AutofyCompanyEndPoint").Value;
                        content.Headers.Add("x-api-key", Autofykey);
                        content.Headers.Add("SchemaVersion", "20.04");
                        string AutoFURL = AutofyBaseURL + "data/" + AutofyCompanyEndPoint + "/customer";
                        HttpResponseMessage result = _httpClient.PostAsync(AutoFURL, content).Result;
                        if (result.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string resultString = result.Content.ReadAsStringAsync().Result;
                            var jsonOutputResult = JsonConvert.DeserializeObject<QBDCustomerOutputDataContract>(resultString);
                            if (string.IsNullOrEmpty(jsonOutputResult.HasErrors) || jsonOutputResult.HasErrors == "false")
                            {
                                qbdcustomer.ID = jsonOutputResult.Contents[0].Object.ID;
                                wfLogic.AddUpdateQBDCustomer(headerUserID.ToString(), qbdcustomer);
                            }
                            //if customer already there in the quickbook but not in our db then add in our db
                            else if (jsonOutputResult.Contents[0].Errors != null && jsonOutputResult.Contents[0].Errors.Count > 0
                                && jsonOutputResult.Contents[0].Errors[0].Message.Contains("the list element is already in use")
                                )
                            {

                                AutoFURL = AutofyBaseURL + "data/" + AutofyCompanyEndPoint + "/customer?name=" + WebUtility.UrlEncode(qbdcustomer.Name);
                                _httpClient.DefaultRequestHeaders.Add("x-api-key", Autofykey);
                                _httpClient.DefaultRequestHeaders.Add("SchemaVersion", "20.04");
                                HttpResponseMessage resultcust = _httpClient.GetAsync(AutoFURL).Result;
                                if (resultcust.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    string resultStringcust = resultcust.Content.ReadAsStringAsync().Result;
                                    var jsonOutputResultcust = JsonConvert.DeserializeObject<QBDCustomerOutputDataContract>(resultStringcust);

                                    if (string.IsNullOrEmpty(jsonOutputResultcust.HasErrors) || jsonOutputResultcust.HasErrors == "false")
                                    {
                                        qbdcustomer.ID = jsonOutputResultcust.Contents[0].Object.ID;
                                        wfLogic.AddUpdateQBDCustomer(headerUserID.ToString(), qbdcustomer);
                                    }
                                }
                            }
                            else
                            {
                                string errormsg = "";
                                if (jsonOutputResult.Errors != null && jsonOutputResult.Errors.Count > 0)
                                    errormsg = jsonOutputResult.Errors[0].Message;
                                else if (jsonOutputResult.Contents != null && jsonOutputResult.Contents.Count > 0 && jsonOutputResult.Contents[0].Errors != null
                                    && jsonOutputResult.Contents[0].Errors.Count > 0)
                                    errormsg = jsonOutputResult.Contents[0].Errors[0].Message;

                                if (!string.IsNullOrEmpty(errormsg))
                                {
                                    LoggerLogic Log = new LoggerLogic();
                                    Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating customer " + drawFeeDC.DealName + " in QBD from scheduler", "", "", "CreateMissingQBDCustomer", errormsg);
                                    EmailDataContract emaildc = new EmailDataContract();
                                    emaildc.ModuleId = 705;
                                    emaildc.Subject = "Quickbook Customer Error Notification";
                                    emaildc.Body = "Error occurred while creating customer " + drawFeeDC.DealName + " in QBD - " + errormsg;
                                    _iEmailNotification.SendErrorNotificationEmail(emaildc);
                                }
                            }
                            _authenticationResult = new GenericResult()
                            {
                                Succeeded = true,
                                Message = "Authentication succeeded"
                            };
                        }
                        else
                        {

                            LoggerLogic Log = new LoggerLogic();
                            Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating customer " + drawFeeDC.DealName + " in QBD", "", headerUserID.ToString(), "CreateQBDCustomer", result.ReasonPhrase);

                            _authenticationResult = new GenericResult()
                            {
                                Succeeded = false,
                                Message = "Authentication failed"
                            };
                            EmailDataContract emaildc = new EmailDataContract();
                            emaildc.ModuleId = 705;
                            emaildc.Subject = "Quickbook Customer Error Notification";
                            emaildc.Body = "Error occurred while creating customer " + drawFeeDC.DealName + " in QBD - " + result.ReasonPhrase;
                            _iEmailNotification.SendErrorNotificationEmail(emaildc);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerLogic Log = new LoggerLogic();
                    Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating customer " + drawFeeDC.DealName + " in QBD", "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = ex.Message
                    };
                }
            }
            else
            {
                string CreatedFrom = "";
                try
                {
                    SetDynamicsAppConfig();
                    if (!string.IsNullOrEmpty(drawFeeDC.CreatedFrom))
                    {
                        CreatedFrom = " From " + drawFeeDC.CreatedFrom;
                    }
                    //use dynamic 365 business central
                    //GetConfigRoot();
                    //IConfiguration config = configroot.GetSection("Dynamics365BusinessCentralDetail");
                    //string BusinessCentralAPIURL = config.GetSection("BusinessCentralBaseURL").Value
                    //    + "companies(9e4c94af-c141-ec11-bb7b-000d3a256200)/customers";
                    GetConfigRoot();
                    IConfiguration config = configroot.GetSection("Dynamics365BusinessCentralDetail");
                    string BusinessCentralAPIURL = config.GetSection("BusinessCentralOdataBaseURL").Value;
                    string BusinessCentralCompanyName = config.GetSection("CompanyName").Value;
                    BusinessCentralAPIURL = BusinessCentralAPIURL + "Company('" + BusinessCentralCompanyName + "')/"
                    + "CustomerListAPI";
                    string authToken = GetBusinessCentralAccessToken();
                    if (string.IsNullOrEmpty(authToken))
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = "Authentication failed"
                        };
                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating customer " + drawFeeDC.DealName + " in Dynamics" + CreatedFrom, "", headerUserID.ToString(), "CreateQBDCustomer", "Unauthentication token");
                        return Ok(_authenticationResult);
                    }

                    //start - customer by dynamic api
                    /*
                    DynamicsCustomerInput cust = new DynamicsCustomerInput();
                    cust.displayName = CommonHelper.FormatCustomerForQuickBook(drawFeeDC.DealName, drawFeeDC.CreDealID); ;
                    cust.type = "Company";
                    string add = drawFeeDC.Address;
                    cust.addressLine1 = add.Length > 100 ? add.Substring(0, 100) : add;
                    cust.addressLine2 = add.Length > 100 ? add.Substring(100, add.Remove(0, 100).Length) : "";
                    //cust.addressLine2 = "Company";
                    cust.city = drawFeeDC.City;
                    cust.state = drawFeeDC.State;
                    //cust.country = "Company";
                    cust.postalCode = drawFeeDC.Zip;
                    cust.phoneNumber = drawFeeDC.PhoneNo;
                    cust.email = drawFeeDC.Email1;
                    cust.taxLiable = true;
                    cust.currencyId = "00000000-0000-0000-0000-000000000000";
                    cust.currencyCode = "USD";
                    cust.paymentTermsId = "5335f8d6-c141-ec11-bb7b-000d3a256200";
                    cust.blocked = "_x0020_";
                    cust.id = "00000000-0000-0000-0000-000000000000";
                    //cust.id = "";

                    string cust_id = CheckCustomerinDynamics(cust, authToken);
                    HttpResponseMessage result = null;

                    // call business central api
                    DynamicsCustomerUpdate updt = new DynamicsCustomerUpdate();
                    updt.displayName = cust.displayName;
                    updt.addressLine1 = cust.addressLine1;
                    updt.addressLine2 = cust.addressLine2;
                    updt.city = cust.city;
                    updt.state = cust.state;
                    updt.postalCode = cust.postalCode;
                    updt.phoneNumber = cust.phoneNumber;
                    string inputjsonstring = JsonConvert.SerializeObject(cust);
                    HttpClient _httpClient = new HttpClient();
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //Set the Authorization header with the Access Token received specifying the Credentials

                    //using (var content = new StringContent(JsonConvert.SerializeObject(jsonInput), System.Text.Encoding.UTF8, "application/json"))
                    using (var content = new StringContent(inputjsonstring, System.Text.Encoding.UTF8, "application/json"))
                    {

                        if (string.IsNullOrEmpty(cust_id))
                        {
                            result = _httpClient.PostAsync(BusinessCentralAPIURL, content).Result;

                        }
                        else //Update customer
                        {
                            BusinessCentralAPIURL = BusinessCentralAPIURL + "(" + cust_id + ")";
                            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "*");
                            result = _httpClient.PatchAsync(BusinessCentralAPIURL, content).Result;
                        }
                        if (result.StatusCode == System.Net.HttpStatusCode.Created)
                        {
                            string resultString = result.Content.ReadAsStringAsync().Result;
                            var jsonOutputResult = JsonConvert.DeserializeObject<DynamicsCustomerOutput>(resultString);
                            QBDCustomerInputDataContract qbdcustomer = new QBDCustomerInputDataContract();
                            qbdcustomer.ID = jsonOutputResult.id;
                            qbdcustomer.FullName = jsonOutputResult.displayName;
                            wfLogic.AddUpdateQBDCustomer(headerUserID.ToString(), qbdcustomer);

                            _authenticationResult = new GenericResult()
                            {
                                Succeeded = true,
                                Message = "Authentication succeeded"
                            };
                        }
                        else
                        {
                            string resultString = result.Content.ReadAsStringAsync().Result;
                            var jsonResultError = JsonConvert.DeserializeObject<error>(resultString);
                            LoggerLogic Log = new LoggerLogic();
                            Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating customer " + drawFeeDC.DealName + " in Dynamics"+ CreatedFrom, "", headerUserID.ToString(), "CreateQBDCustomer", jsonResultError.message);

                            //_authenticationResult = new GenericResult()
                            //{
                            //    Succeeded = false,
                            //    Message = "Authentication failed"
                            //};
                            //EmailDataContract emaildc = new EmailDataContract();
                            //emaildc.ModuleId = 705;
                            //emaildc.Subject = "Quickbook Customer Error Notification";
                            //emaildc.Body = "Error occurred while creating customer " + drawFeeDC.DealName + " in QBD - " + result.ReasonPhrase;
                            //_iEmailNotification.SendErrorNotificationEmail(emaildc);
                        }
                    */
                    //  end - customer by dynamic api
                    //start-customer by Odata api
                    DynamicsODataCustomerInput cust = new DynamicsODataCustomerInput();
                    cust.Name = CommonHelper.FormatCustomerForQuickBook(drawFeeDC.DealName, drawFeeDC.CreDealID); ;
                    cust.No = drawFeeDC.CreDealID;
                    //cust.type = "Company";
                    cust.Contact = drawFeeDC.FirstName + " " + drawFeeDC.LastName;
                    string add = drawFeeDC.Address;
                    cust.Address = add.Length > 100 ? add.Substring(0, 100) : add;
                    cust.Address_2 = add.Length > 100 ? add.Substring(100, add.Remove(0, 100).Length) : "";
                    //cust.addressLine2 = "Company";
                    cust.City = drawFeeDC.City;
                    cust.State = drawFeeDC.State;
                    //cust.country = "Company";
                    cust.Post_Code = drawFeeDC.Zip;
                    cust.Phone_No = drawFeeDC.PhoneNo;
                    cust.E_Mail = drawFeeDC.Email1;
                    //cust.taxLiable = true;
                    //cust.currencyId = "00000000-0000-0000-0000-000000000000";
                    //cust.Currency_Code = "USD";
                    //cust.paymentTermsId = "5335f8d6-c141-ec11-bb7b-000d3a256200";
                    //cust.Payment_Method_Code = "BANK";
                    cust.Payment_Method_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Payment_Method_Code").FirstOrDefault().Value;
                    //cust.Payment_Terms_Code = "1M(8D)";
                    //cust.Payment_Terms_Code = "30 DAYS";
                    cust.Payment_Terms_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Payment_Terms_Code").FirstOrDefault().Value;
                    cust.Blocked = "";
                    //cust.Customer_Posting_Group = "ACCOUNTS REC";
                    cust.Customer_Posting_Group = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Customer_Posting_Group").FirstOrDefault().Value;
                    //cust.Gen_Bus_Posting_Group = "DOMESTIC";
                    cust.Gen_Bus_Posting_Group = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Gen_Bus_Posting_Group").FirstOrDefault().Value;
                    cust.Country_Region_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Country_Region_Code").FirstOrDefault().Value;

                    //cust.id = "00000000-0000-0000-0000-000000000000";
                    //cust.id = "";

                    string CustomerNo = CheckCustomerinDynamicsByOdata(cust, authToken);
                    HttpResponseMessage result = null;

                    // call business central api
                    DynamicsODataCustomerUpdate updt = new DynamicsODataCustomerUpdate();
                    updt.Contact = drawFeeDC.FirstName + " " + drawFeeDC.LastName;
                    updt.Address = cust.Address;
                    updt.Address_2 = cust.Address_2;
                    updt.City = cust.City;
                    updt.State = cust.State;
                    updt.Post_Code = cust.Post_Code;
                    updt.Phone_No = cust.Phone_No;
                    updt.E_Mail = cust.E_Mail;
                    //updt.Country_Region_Code = "US";
                    updt.Country_Region_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Country_Region_Code").FirstOrDefault().Value;
                    updt.Payment_Method_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Payment_Method_Code").FirstOrDefault().Value;
                    updt.Payment_Terms_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Payment_Terms_Code").FirstOrDefault().Value;
                    updt.Customer_Posting_Group = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Customer_Posting_Group").FirstOrDefault().Value;
                    updt.Gen_Bus_Posting_Group = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Gen_Bus_Posting_Group").FirstOrDefault().Value;
                    updt.Country_Region_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Country_Region_Code").FirstOrDefault().Value;
                    string inputjsonstring = "";
                    HttpClient _httpClient = new HttpClient();
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //Set the Authorization header with the Access Token received specifying the Credentials
                    if (string.IsNullOrEmpty(CustomerNo))
                    {
                        inputjsonstring = JsonConvert.SerializeObject(cust);
                    }
                    else
                    {
                        inputjsonstring = JsonConvert.SerializeObject(updt);
                    }


                    //using (var content = new StringContent(JsonConvert.SerializeObject(jsonInput), System.Text.Encoding.UTF8, "application/json"))
                    using (var content = new StringContent(inputjsonstring, System.Text.Encoding.UTF8, "application/json"))
                    {

                        if (string.IsNullOrEmpty(CustomerNo))
                        {
                            result = _httpClient.PostAsync(BusinessCentralAPIURL, content).Result;

                        }
                        else //Update customer
                        {
                            BusinessCentralAPIURL = BusinessCentralAPIURL + "('" + CustomerNo + "')";
                            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "*");
                            result = _httpClient.PatchAsync(BusinessCentralAPIURL, content).Result;
                        }
                        if (result.StatusCode == System.Net.HttpStatusCode.Created || result.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string resultString = result.Content.ReadAsStringAsync().Result;
                            var jsonOutputResult = JsonConvert.DeserializeObject<DynamicsODataCustomerOutput>(resultString);
                            QBDCustomerInputDataContract qbdcustomer = new QBDCustomerInputDataContract();
                            //qbdcustomer.ID = jsonOutputResult.id;
                            qbdcustomer.FullName = jsonOutputResult.Name;
                            qbdcustomer.CustomerNo = jsonOutputResult.No;
                            qbdcustomer.ID = jsonOutputResult.SystemId;
                            qbdcustomer.ContactID = jsonOutputResult.Primary_Contact_No;
                            wfLogic.AddUpdateQBDCustomer(headerUserID.ToString(), qbdcustomer);
                            _authenticationResult = new GenericResult()
                            {
                                Succeeded = true,
                                Message = "Authentication succeeded"
                            };
                        }
                        else
                        {
                            string resultString = result.Content.ReadAsStringAsync().Result;
                            var jsonResultError = JsonConvert.DeserializeObject<error>(resultString);
                            string errmessage = string.IsNullOrEmpty(jsonResultError.message) ? resultString : jsonResultError.message;
                            errmessage = errmessage + "-Url-" + BusinessCentralAPIURL + "-status code-" + result.StatusCode;
                            LoggerLogic Log = new LoggerLogic();
                            Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating customer " + drawFeeDC.DealName + " in Dynamics" + CreatedFrom, "", headerUserID.ToString(), "CreateQBDCustomer", errmessage);

                            _authenticationResult = new GenericResult()
                            {
                                Succeeded = false,
                                Message = "Authentication failed"
                            };
                            EmailDataContract emaildc = new EmailDataContract();
                            emaildc.ModuleId = 705;
                            emaildc.Subject = "Dynamics 365 Customer Error Notification";
                            emaildc.Body = "Error occurred while creating customer " + drawFeeDC.DealName + " in Dynamics - " + errmessage;
                            _iEmailNotification.SendErrorNotificationEmail(emaildc);
                        }

                        //End-customer by Odata api

                    }
                }

                catch (Exception ex)
                {
                    LoggerLogic Log = new LoggerLogic();
                    Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating customer " + drawFeeDC.DealName + " in dynamics" + CreatedFrom, "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = ex.Message
                    };
                }
            }
            return Ok(_authenticationResult);
        }


        [HttpPatch]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/wfcontroller/updateqbdcustomer")]
        public IActionResult UpdateQBDCustomer([FromBody] DrawFeeInvoiceDataContract drawFeeDC)
        {
            GenericResult _authenticationResult = null;
            DrawFeeInvoiceDataContract drawDC = new DrawFeeInvoiceDataContract();
            WFLogic wfLogic = new WFLogic();
            GetConfigSetting();
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            if (!IsUseDynamicForInvoice())
            {
                try
                {

                    QBDCustomerInputDataContract qbdcustomer = new QBDCustomerInputDataContract();
                    qbdcustomer.ID = drawFeeDC.ID;
                    qbdcustomer.FirstName = drawFeeDC.FirstName;
                    qbdcustomer.LastName = drawFeeDC.LastName;
                    qbdcustomer.CompanyName = drawFeeDC.CompanyName;
                    qbdcustomer.Name = CommonHelper.FormatCustomerForQuickBook(drawFeeDC.DealName, drawFeeDC.CreDealID);
                    qbdcustomer.FullName = qbdcustomer.Name;
                    qbdcustomer.BillingAddress = FormateQBDCustomerAddress(drawFeeDC.Address);
                    qbdcustomer.BillingAddress.City = drawFeeDC.City;
                    qbdcustomer.BillingAddress.State = drawFeeDC.State;
                    qbdcustomer.BillingAddress.PostalCode = drawFeeDC.Zip;

                    //call customer api

                    string inputjsonstring = JsonConvert.SerializeObject(qbdcustomer);


                    // var jsonInputResult = JsonConvert.DeserializeObject<AutofyInvoiceInputDataContract>(jsonInput);
                    HttpClient _httpClient = new HttpClient();
                    //using (var content = new StringContent(JsonConvert.SerializeObject(jsonInput), System.Text.Encoding.UTF8, "application/json"))
                    using (var content = new StringContent(JsonConvert.SerializeObject(inputjsonstring), System.Text.Encoding.UTF8, "application/json"))
                    {
                        string Autofykey = Sectionroot.GetSection("Autofykey").Value;
                        string SchemaVersion = Sectionroot.GetSection("SchemaVersion").Value;
                        string AutofyBaseURL = Sectionroot.GetSection("AutofyBaseURL").Value;
                        string AutofyCompanyEndPoint = Sectionroot.GetSection("AutofyCompanyEndPoint").Value;
                        content.Headers.Add("x-api-key", Autofykey);
                        content.Headers.Add("SchemaVersion", "20.04");
                        string AutoFURL = AutofyBaseURL + "data/" + AutofyCompanyEndPoint + "/customer";
                        HttpResponseMessage result = _httpClient.PatchAsync(AutoFURL, content).Result;
                        if (result.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string resultString = result.Content.ReadAsStringAsync().Result;
                            var jsonOutputResult = JsonConvert.DeserializeObject<QBDCustomerOutputDataContract>(resultString);
                            if (string.IsNullOrEmpty(jsonOutputResult.HasErrors) || jsonOutputResult.HasErrors == "false")
                            {
                                qbdcustomer.ID = jsonOutputResult.Contents[0].Object.ID;
                                wfLogic.AddUpdateQBDCustomer(headerUserID.ToString(), qbdcustomer);
                            }
                            _authenticationResult = new GenericResult()
                            {
                                Succeeded = true,
                                Message = "Authentication succeeded"
                            };
                        }
                        else
                        {
                            LoggerLogic Log = new LoggerLogic();
                            Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while updating customer " + drawFeeDC.DealName + " in QBD", "", headerUserID.ToString(), "UpdateQBDCustomer", result.ReasonPhrase);

                            _authenticationResult = new GenericResult()
                            {
                                Succeeded = false,
                                Message = "Authentication failed"
                            };
                            EmailDataContract emaildc = new EmailDataContract();
                            emaildc.ModuleId = 705;
                            emaildc.Subject = "Quickbook Customer Error Notification";
                            emaildc.Body = "Error occurred while updating customer " + drawFeeDC.DealName + " in QBD - " + result.ReasonPhrase;
                            _iEmailNotification.SendErrorNotificationEmail(emaildc);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerLogic Log = new LoggerLogic();
                    Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while updating customer " + drawFeeDC.DealName + " in QBD", "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = ex.Message
                    };
                }
            }
            else
            {
                //use dynamic 365 business central
                string CreatedFrom = "";

                if (!string.IsNullOrEmpty(drawFeeDC.CreatedFrom))
                {
                    CreatedFrom = " From " + drawFeeDC.CreatedFrom;
                }

                try
                {
                    GetConfigRoot();
                    SetDynamicsAppConfig();
                    IConfiguration config = configroot.GetSection("Dynamics365BusinessCentralDetail");
                    string BusinessCentralAPIURL = config.GetSection("BusinessCentralOdataBaseURL").Value;
                    string BusinessCentralCompanyName = config.GetSection("CompanyName").Value;
                    string CustomerNo = "";
                    BusinessCentralAPIURL = BusinessCentralAPIURL + "Company('" + BusinessCentralCompanyName + "')/"
                    + "CustomerListAPI";
                    string authToken = GetBusinessCentralAccessToken();
                    if (string.IsNullOrEmpty(authToken))
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = "Authentication failed"
                        };
                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while updating customer " + drawFeeDC.DealName + " in Dynamics" + CreatedFrom, "", headerUserID.ToString(), "CreateQBDCustomer", "Unauthentication token");
                        return Ok(_authenticationResult);
                    }

                    HttpResponseMessage result = null;
                    // call business central api
                    /*
                    DynamicsCustomerUpdate updt = new DynamicsCustomerUpdate();
                    updt.displayName = CommonHelper.FormatCustomerForQuickBook(drawFeeDC.DealName, drawFeeDC.CreDealID);
                    string add = drawFeeDC.Address;
                    updt.addressLine1 = add.Length>100? add.Substring(0, 100): add;
                    updt.addressLine2 = add.Length>100? add.Substring(100, add.Remove(0, 100).Length) : "";
                    updt.city = drawFeeDC.City;
                    updt.state = drawFeeDC.State;
                    updt.postalCode = drawFeeDC.Zip;
                    updt.phoneNumber = drawFeeDC.PhoneNo;
                    DynamicsCustomer dcust = wfLogic.GetCustomerByAccountName(updt.displayName);
                    string inputjsonstring = JsonConvert.SerializeObject(updt);
                    HttpClient _httpClient = new HttpClient();
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //Set the Authorization header with the Access Token received specifying the Credentials

                    //using (var content = new StringContent(JsonConvert.SerializeObject(jsonInput), System.Text.Encoding.UTF8, "application/json"))
                    using (var content = new StringContent(inputjsonstring, System.Text.Encoding.UTF8, "application/json"))
                    {

                       //Update customer
                        BusinessCentralAPIURL = BusinessCentralAPIURL + "(" + dcust.ID + ")";
                        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "*");
                        result = _httpClient.PatchAsync(BusinessCentralAPIURL, content).Result;
                        if (result.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string resultString = result.Content.ReadAsStringAsync().Result;
                            var jsonOutputResult = JsonConvert.DeserializeObject<DynamicsCustomerOutput>(resultString);
                            QBDCustomerInputDataContract qbdcustomer = new QBDCustomerInputDataContract();
                            qbdcustomer.ID = jsonOutputResult.id;
                            qbdcustomer.FullName = jsonOutputResult.displayName;
                            wfLogic.AddUpdateQBDCustomer(headerUserID.ToString(), qbdcustomer);

                            _authenticationResult = new GenericResult()
                            {
                                Succeeded = true,
                                Message = "Authentication succeeded"
                            };
                        }
                        else
                        {

                            LoggerLogic Log = new LoggerLogic();
                            //Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating customer " + drawFeeDC.DealName + " in QBD", "", headerUserID.ToString(), "CreateQBDCustomer", result.ReasonPhrase);

                            //_authenticationResult = new GenericResult()
                            //{
                            //    Succeeded = false,
                            //    Message = "Authentication failed"
                            //};
                            //EmailDataContract emaildc = new EmailDataContract();
                            //emaildc.ModuleId = 705;
                            //emaildc.Subject = "Quickbook Customer Error Notification";
                            //emaildc.Body = "Error occurred while creating customer " + drawFeeDC.DealName + " in QBD - " + result.ReasonPhrase;
                            //_iEmailNotification.SendErrorNotificationEmail(emaildc);
                        }
                        */
                    //start-customer by Odata api
                    DynamicsODataCustomerInput cust = new DynamicsODataCustomerInput();
                    cust.Name = CommonHelper.FormatCustomerForQuickBook(drawFeeDC.DealName, drawFeeDC.CreDealID);
                    cust.Contact = drawFeeDC.FirstName + " " + drawFeeDC.LastName;
                    cust.Search_Name = cust.Contact;
                    //cust.type = "Company";
                    string add = drawFeeDC.Address;
                    cust.Address = add.Length > 100 ? add.Substring(0, 100) : add;
                    cust.Address_2 = add.Length > 100 ? add.Substring(100, add.Remove(0, 100).Length) : "";
                    //cust.addressLine2 = "Company";
                    cust.City = drawFeeDC.City;
                    cust.State = drawFeeDC.State;
                    //cust.country = "Company";
                    cust.Post_Code = drawFeeDC.Zip;
                    cust.Phone_No = drawFeeDC.PhoneNo;
                    cust.E_Mail = drawFeeDC.Email1;
                    //cust.taxLiable = true;
                    //cust.currencyId = "00000000-0000-0000-0000-000000000000";
                    //cust.Currency_Code = "USD";
                    //cust.paymentTermsId = "5335f8d6-c141-ec11-bb7b-000d3a256200";
                    cust.Payment_Method_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Payment_Method_Code").FirstOrDefault().Value;
                    cust.Payment_Terms_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Payment_Terms_Code").FirstOrDefault().Value;
                    cust.Customer_Posting_Group = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Customer_Posting_Group").FirstOrDefault().Value;
                    cust.Gen_Bus_Posting_Group = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Gen_Bus_Posting_Group").FirstOrDefault().Value;
                    cust.Country_Region_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Country_Region_Code").FirstOrDefault().Value;
                    cust.Blocked = "";
                    //cust.id = "00000000-0000-0000-0000-000000000000";
                    //cust.id = "";

                    CustomerNo = CheckCustomerinDynamicsByOdata(cust, authToken);
                    // call business central api
                    DynamicsODataCustomerUpdate updt = new DynamicsODataCustomerUpdate();
                    DynamicsCustomer dcust = wfLogic.GetCustomerByAccountName(cust.Name);
                    updt.Contact = "";
                    updt.Contact = drawFeeDC.FirstName + " " + drawFeeDC.LastName;
                    updt.Primary_Contact_No = dcust.ContactID;
                    updt.Search_Name = updt.Contact;
                    updt.Address = cust.Address;
                    updt.Address_2 = cust.Address_2;
                    updt.City = cust.City;
                    updt.State = cust.State;
                    updt.Post_Code = cust.Post_Code;
                    updt.Phone_No = cust.Phone_No;
                    updt.E_Mail = cust.E_Mail;
                    //updt.Country_Region_Code = "US";
                    updt.Country_Region_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Country_Region_Code").FirstOrDefault().Value;
                    updt.Payment_Method_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Payment_Method_Code").FirstOrDefault().Value;
                    updt.Payment_Terms_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Payment_Terms_Code").FirstOrDefault().Value;
                    updt.Customer_Posting_Group = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Customer_Posting_Group").FirstOrDefault().Value;
                    updt.Gen_Bus_Posting_Group = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Gen_Bus_Posting_Group").FirstOrDefault().Value;

                    string inputjsonstring = JsonConvert.SerializeObject(updt);
                    HttpClient _httpClient = new HttpClient();
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    //using (var content = new StringContent(JsonConvert.SerializeObject(jsonInput), System.Text.Encoding.UTF8, "application/json"))
                    BusinessCentralAPIURL = BusinessCentralAPIURL + "('" + CustomerNo + "')";


                    using (var content = new StringContent(inputjsonstring, System.Text.Encoding.UTF8, "application/json"))
                    {
                        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "*");
                        result = _httpClient.PatchAsync(BusinessCentralAPIURL, content).Result;
                        if (result.StatusCode == System.Net.HttpStatusCode.OK || result.StatusCode == System.Net.HttpStatusCode.Created)
                        {
                            string resultString = result.Content.ReadAsStringAsync().Result;
                            var jsonOutputResult = JsonConvert.DeserializeObject<DynamicsODataCustomerOutput>(resultString);
                            QBDCustomerInputDataContract qbdcustomer = new QBDCustomerInputDataContract();
                            //qbdcustomer.ID = jsonOutputResult.id;
                            qbdcustomer.FullName = jsonOutputResult.Name;
                            qbdcustomer.CustomerNo = jsonOutputResult.No;
                            qbdcustomer.ContactID = jsonOutputResult.Primary_Contact_No;
                            wfLogic.AddUpdateQBDCustomer(headerUserID.ToString(), qbdcustomer);
                            _authenticationResult = new GenericResult()
                            {
                                Succeeded = true,
                                Message = "Authentication succeeded"
                            };
                        }
                        else
                        {
                            string resultString = result.Content.ReadAsStringAsync().Result;
                            var jsonResultError = JsonConvert.DeserializeObject<error>(resultString);
                            string errmessage = string.IsNullOrEmpty(jsonResultError.message) ? resultString : jsonResultError.message;
                            errmessage = errmessage + "-Url-" + BusinessCentralAPIURL + "-status code-" + result.StatusCode;
                            LoggerLogic Log = new LoggerLogic();
                            Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while updating customer " + drawFeeDC.DealName + " in Dynamics" + CreatedFrom, "", headerUserID.ToString(), "UpdateQBDCustomer", errmessage);

                            _authenticationResult = new GenericResult()
                            {
                                Succeeded = false,
                                Message = "Authentication failed"
                            };
                            EmailDataContract emaildc = new EmailDataContract();
                            emaildc.ModuleId = 705;


                            emaildc.Subject = "Dynamics Customer Error Notification";
                            emaildc.Body = "Error occurred while updating customer " + drawFeeDC.DealName + " in Dynamics - " + errmessage;
                            _iEmailNotification.SendErrorNotificationEmail(emaildc);
                        }

                        //End-customer by Odata api
                    }
                }
                catch (Exception ex)
                {
                    LoggerLogic Log = new LoggerLogic();
                    Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while updating customer " + drawFeeDC.DealName + " in Dynamics", "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = ex.Message
                    };
                }
            }
            return Ok(_authenticationResult);
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/wfcontroller/getquickbookcompany")]
        public IActionResult GetQuickBookCompany([FromBody] QBDCompanyDataContract qbdDC)
        {
            GenericResult _authenticationResult = null;
            QBDCompanyDataContract qbdOutDC = new QBDCompanyDataContract();
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            WFLogic _wfLogic = new WFLogic();
            qbdOutDC = _wfLogic.GetQuickBookCompany(headerUserID.ToString(), qbdDC);
            try
            {
                if (qbdOutDC != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        QBDCompany = qbdOutDC,
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

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/wfcontroller/getallStatesMaster")]
        public IActionResult GetAllStatesMaster()
        {
            GenericResult _authenticationResult = null;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            WFLogic _wfLogic = new WFLogic();
            DataTable dt = _wfLogic.GetAllStatesMaster(headerUserID.ToString());

            try
            {
                if (dt != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        dt = dt
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

        public DrawFeeInvoiceDataContract CreateInvoiceInQBD([FromBody] DrawFeeInvoiceDataContract DrawFeeDC)
        {
            if (!IsUseDynamicForInvoice())
            {

                // generate invoice in below cases
                //if autosend invoice from workflow
                //if manual from dealdetail
                if ((DrawFeeDC.AutoSendInvoice == 571 || DrawFeeDC.IsManualInvoice == true || DrawFeeDC.InvoiceSource == "System")
                && string.IsNullOrEmpty(DrawFeeDC.InvoiceNo) == true)
                {
                    GetConfigSetting();
                    var headerUserID = string.Empty;
                    //if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                    //{
                    //    headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
                    //}

                    try
                    {
                        WFLogic _wfLogicInvoiceConfig = new WFLogic();
                        InvoiceConfigDataContract invoiceDC = new InvoiceConfigDataContract();
                        invoiceDC = _wfLogicInvoiceConfig.GetInvoiceConfigByInvoiceType(DrawFeeDC.InvoiceTypeID, "");
                        if (invoiceDC != null && !string.IsNullOrEmpty(invoiceDC.InvoiceCode))
                        {
                            DrawFeeDC.InvoiceCode = invoiceDC.InvoiceCode;
                            DrawFeeDC.TemplateName = invoiceDC.Template;
                        }

                        WFLogic _wfLogic = new WFLogic();
                        if (DrawFeeDC.DrawFeeInvoiceDetailID == 0 || string.IsNullOrEmpty(DrawFeeDC.FirstName))
                        {
                            DrawFeeInvoiceDataContract drawDC = new DrawFeeInvoiceDataContract();
                            drawDC = _wfLogic.GetDrawFeeInvoiceDetailByTaskID(DrawFeeDC.TaskID.ToString(), headerUserID.ToString());
                            DrawFeeDC.DrawFeeInvoiceDetailID = drawDC.DrawFeeInvoiceDetailID;
                            DrawFeeDC.FirstName = drawDC.FirstName;
                            DrawFeeDC.LastName = drawDC.LastName;
                            DrawFeeDC.Amount = drawDC.Amount;
                            DrawFeeDC.City = drawDC.City;
                            DrawFeeDC.State = drawDC.State;
                            DrawFeeDC.Zip = drawDC.Zip;
                            DrawFeeDC.Address = drawDC.Address;
                            DrawFeeDC.Email1 = drawDC.Email1;
                            DrawFeeDC.Email2 = drawDC.Email2;
                            DrawFeeDC.PhoneNo = drawDC.PhoneNo;
                            DrawFeeDC.AlternatePhone = drawDC.AlternatePhone;
                            DrawFeeDC.CompanyName = drawDC.CompanyName;
                            DrawFeeDC.InvoiceNoUI = drawDC.InvoiceNoUI;
                            DrawFeeDC.InvoiceNo = drawDC.InvoiceNo;
                            DrawFeeDC.AMEmails = drawDC.AMEmails;
                            DrawFeeDC.SenderFirstName = drawDC.SenderFirstName;
                            DrawFeeDC.SenderLastName = drawDC.SenderLastName;
                            DrawFeeDC.SenderEmail = drawDC.SenderEmail;
                        }

                        if (string.IsNullOrEmpty(DrawFeeDC.InvoiceNo) == true)
                        {
                            AutofyInvoiceInputDataContract _autofyInput = new AutofyInvoiceInputDataContract();
                            _autofyInput.Customer = new Customer();
                            _autofyInput.Customer.FullName = CommonHelper.FormatCustomerForQuickBook(DrawFeeDC.DealName, DrawFeeDC.CreDealID);
                            _autofyInput.Date = DrawFeeDC.FundingDate;
                            _autofyInput.DueDate = DrawFeeDC.FundingDate;
                            _autofyInput.ToBePrinted = true;
                            _autofyInput.ToBeEmailed = false;
                            _autofyInput.ItemLines = new List<ItemLines>();
                            //call invoice split
                            //get invoice split by financial source and adjustedcommitment
                            List<InvoiceSplitOutputDataContract> lstSplit = new List<InvoiceSplitOutputDataContract>();
                            if (invoiceDC != null && invoiceDC.IsApplySplit == true)
                            {
                                InvoiceSplitParamDataContract param = new InvoiceSplitParamDataContract();
                                param.DealID = DrawFeeDC.CreDealID;
                                param.InvoiceTypeID = DrawFeeDC.InvoiceTypeID;
                                param.FeeAmount = DrawFeeDC.Amount;
                                lstSplit = _wfLogic.GetInvoiceSplit(param, headerUserID);
                                if (lstSplit.Count > 0)
                                {
                                    foreach (InvoiceSplitOutputDataContract itm in lstSplit)
                                    {
                                        _autofyInput.ItemLines.Add(
                                        new ItemLines { Description = DrawFeeDC.DrawNo, Amount = Convert.ToDecimal(itm.SplitAmount), Rate = Convert.ToDecimal(itm.SplitAmount), Item = new Item() { FullName = itm.QBItemName } }
                                        );
                                    }
                                }
                            }
                            else
                            {
                                _autofyInput.ItemLines.Add(
                                    new ItemLines { Description = DrawFeeDC.DrawNo, Amount = Convert.ToDecimal(DrawFeeDC.Amount), Rate = Convert.ToDecimal(DrawFeeDC.Amount), Item = new Item() { FullName = DrawFeeDC.InvoiceCode } }
                                    );
                            }
                            _autofyInput.BillingAddress = new BillingAddress();
                            _autofyInput.BillingAddress.City = DrawFeeDC.City;
                            _autofyInput.BillingAddress.State = DrawFeeDC.State;
                            _autofyInput.IsPaid = false;
                            _autofyInput.Template = new Template();
                            _autofyInput.Template.FullName = DrawFeeDC.TemplateName;
                            _autofyInput.Memo = DrawFeeDC.InvoiceNoUI;
                            _autofyInput.DateShipped = DateTime.Now;
                            string inputjsonstring = JsonConvert.SerializeObject(_autofyInput);

                            // var jsonInputResult = JsonConvert.DeserializeObject<AutofyInvoiceInputDataContract>(jsonInput);
                            HttpClient _httpClient = new HttpClient();
                            //using (var content = new StringContent(JsonConvert.SerializeObject(jsonInput), System.Text.Encoding.UTF8, "application/json"))
                            using (var content = new StringContent(JsonConvert.SerializeObject(inputjsonstring), System.Text.Encoding.UTF8, "application/json"))
                            {
                                string Autofykey = Sectionroot.GetSection("Autofykey").Value;
                                string SchemaVersion = Sectionroot.GetSection("SchemaVersion").Value;
                                string AutofyBaseURL = Sectionroot.GetSection("AutofyBaseURL").Value;
                                string AutofyCompanyEndPoint = Sectionroot.GetSection("AutofyCompanyEndPoint").Value;
                                content.Headers.Add("x-api-key", Autofykey);
                                content.Headers.Add("SchemaVersion", "20.04");
                                string AutoFURL = AutofyBaseURL + "data/" + AutofyCompanyEndPoint + "/Invoice";
                                HttpResponseMessage result = _httpClient.PostAsync(AutoFURL, content).Result;
                                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    string resultString = result.Content.ReadAsStringAsync().Result;
                                    var jsonOutputResult = JsonConvert.DeserializeObject<AutofyInvoiceOutputDataContract>(resultString);
                                    if (string.IsNullOrEmpty(jsonOutputResult.HasErrors) || jsonOutputResult.HasErrors == "false")
                                    {
                                        DrawFeeDC.IsSuccess = true;
                                        DrawFeeDC.InvoiceNo = jsonOutputResult.Contents[0].Object.Number;
                                    }
                                    else
                                    {
                                        string errorMsg = "";
                                        if (jsonOutputResult.Contents[0].Errors != null && jsonOutputResult.Contents[0].Errors.Count > 0)
                                        {
                                            errorMsg = jsonOutputResult.Contents[0].Errors[0].Message;
                                        }
                                        else if (jsonOutputResult.Errors != null && jsonOutputResult.Errors.Count > 0)
                                        {
                                            errorMsg = jsonOutputResult.Errors[0].Message;
                                        }

                                        LoggerLogic Log = new LoggerLogic();
                                        Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in QBD for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), "", headerUserID.ToString(), "CreateInvoiceInQBD", errorMsg);

                                        EmailDataContract emaildc = new EmailDataContract();
                                        emaildc.ModuleId = 705;
                                        emaildc.Subject = "Quickbook Invoice Error Notification";
                                        emaildc.Body = "Error occurred while creating invoice in QBD for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString() + " - " + errorMsg;
                                        _iEmailNotification.SendErrorNotificationEmail(emaildc);

                                    }

                                }
                                else
                                {
                                    LoggerLogic Log = new LoggerLogic();
                                    Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in QBD for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), "", headerUserID.ToString(), "CreateInvoiceInQBD", result.ReasonPhrase);
                                    EmailDataContract emaildc = new EmailDataContract();
                                    emaildc.ModuleId = 705;
                                    emaildc.Subject = "Quickbook Invoice Error Notification";
                                    emaildc.Body = "Error occurred while creating invoice in QBD for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString() + " - " + result.ReasonPhrase;
                                    _iEmailNotification.SendErrorNotificationEmail(emaildc);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in QBD for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);
                        DrawFeeDC.IsSuccess = false;
                        EmailDataContract emaildc = new EmailDataContract();
                        emaildc.ModuleId = 705;
                        emaildc.Subject = "Quickbook Invoice Error Notification";
                        emaildc.Body = "Error occurred while creating invoice in QBD for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString() + " - " + ex.Message;
                        _iEmailNotification.SendErrorNotificationEmail(emaildc);
                    }
                }
            }
            else
            {
                // generate invoice in below cases
                //if autosend invoice from workflow
                //if manual from dealdetail
                if ((DrawFeeDC.AutoSendInvoice == 571 || DrawFeeDC.IsManualInvoice == true || DrawFeeDC.InvoiceSource == "System")
                && string.IsNullOrEmpty(DrawFeeDC.InvoiceNo) == true)
                {
                    GetConfigSetting();
                    SetDynamicsAppConfig();
                    var headerUserID = string.Empty;
                    //if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                    //{
                    //    headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
                    //}

                    try
                    {
                        WFLogic _wfLogicInvoiceConfig = new WFLogic();
                        InvoiceConfigDataContract invoiceDC = new InvoiceConfigDataContract();
                        invoiceDC = _wfLogicInvoiceConfig.GetInvoiceConfigByInvoiceType(DrawFeeDC.InvoiceTypeID, "");
                        if (invoiceDC != null && !string.IsNullOrEmpty(invoiceDC.InvoiceCode))
                        {
                            DrawFeeDC.InvoiceCode = invoiceDC.InvoiceCode;
                            DrawFeeDC.TemplateName = invoiceDC.Template;
                        }

                        WFLogic _wfLogic = new WFLogic();
                        if (DrawFeeDC.DrawFeeInvoiceDetailID == 0 || string.IsNullOrEmpty(DrawFeeDC.FirstName))
                        {
                            DrawFeeInvoiceDataContract drawDC = new DrawFeeInvoiceDataContract();
                            drawDC = _wfLogic.GetDrawFeeInvoiceDetailByTaskID(DrawFeeDC.TaskID.ToString(), headerUserID.ToString());
                            DrawFeeDC.DrawFeeInvoiceDetailID = drawDC.DrawFeeInvoiceDetailID;
                            DrawFeeDC.FirstName = drawDC.FirstName;
                            DrawFeeDC.LastName = drawDC.LastName;
                            DrawFeeDC.Amount = drawDC.Amount;
                            DrawFeeDC.City = drawDC.City;
                            DrawFeeDC.State = drawDC.State;
                            DrawFeeDC.Zip = drawDC.Zip;
                            DrawFeeDC.Address = drawDC.Address;
                            DrawFeeDC.Email1 = drawDC.Email1;
                            DrawFeeDC.Email2 = drawDC.Email2;
                            DrawFeeDC.PhoneNo = drawDC.PhoneNo;
                            DrawFeeDC.AlternatePhone = drawDC.AlternatePhone;
                            DrawFeeDC.CompanyName = drawDC.CompanyName;
                            DrawFeeDC.InvoiceNoUI = drawDC.InvoiceNoUI;
                            DrawFeeDC.InvoiceNo = drawDC.InvoiceNo;
                            DrawFeeDC.AMEmails = drawDC.AMEmails;
                            DrawFeeDC.SenderFirstName = drawDC.SenderFirstName;
                            DrawFeeDC.SenderLastName = drawDC.SenderLastName;
                            DrawFeeDC.SenderEmail = drawDC.SenderEmail;
                        }

                        if (string.IsNullOrEmpty(DrawFeeDC.InvoiceNo) == true)
                        {
                            //create invoice in dynamic 365
                            GetConfigRoot();
                            IConfiguration config = configroot.GetSection("Dynamics365BusinessCentralDetail");
                            string CompanyID = config.GetSection("CompanyID").Value;
                            string BusinessCentralAPIURL = config.GetSection("BusinessCentralBaseURL").Value
                                + "companies(" + CompanyID + ")/salesInvoices";
                            string authToken = GetBusinessCentralAccessToken();

                            if (string.IsNullOrEmpty(authToken))
                            {
                                LoggerLogic Log = new LoggerLogic();
                                Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in Dynamics for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), headerUserID.ToString(), "CreateInvoiceInQBD", "Unauthentication token");
                                DrawFeeDC.IsSuccess = false;
                                return DrawFeeDC;
                            }

                            string accountName = CommonHelper.FormatCustomerForQuickBook(DrawFeeDC.DealName, DrawFeeDC.CreDealID);
                            DynamicsCustomer dcust = _wfLogic.GetCustomerByAccountName(accountName);
                            DynamicsSalesInvoiceInput dinput = new DynamicsSalesInvoiceInput();
                            dinput.invoiceDate = DrawFeeDC.FundingDate.ToString("yyyy-MM-dd");
                            dinput.dueDate = DrawFeeDC.FundingDate.ToString("yyyy-MM-dd");
                            dinput.postingDate = DrawFeeDC.FundingDate.ToString("yyyy-MM-dd");
                            dinput.customerNumber = dcust.CustomerNo;
                            //dinput.paymentTermsId = "5335f8d6-c141-ec11-bb7b-000d3a256200";
                            //dinput.paymentTermsId = "7e5ed7ef-a1b1-ec11-8aa5-0022482b5b4b";
                            //dinput.currencyCode = "USD";
                            //dinput.currencyId = "00000000-0000-0000-0000-000000000000";

                            //dinput.paymentTermsId = DynamicsAppConfig.Where(i => i.Key == "Dynamics_paymentTermsId").FirstOrDefault().Value;
                            //dinput.currencyCode = DynamicsAppConfig.Where(i => i.Key == "Dynamics_currencyCode").FirstOrDefault().Value;
                            //dinput.currencyId = DynamicsAppConfig.Where(i => i.Key == "Dynamics_currencyId").FirstOrDefault().Value;
                            dinput.externalDocumentNumber = DrawFeeDC.InvoiceNoUI;
                            dinput.customerPurchaseOrderReference = string.IsNullOrEmpty(DrawFeeDC.Comment) ? DrawFeeDC.DrawNo : DrawFeeDC.Comment;
                            if (dinput.customerPurchaseOrderReference.Length > 35)
                            {
                                dinput.customerPurchaseOrderReference = dinput.customerPurchaseOrderReference.Substring(0, 35);
                            }

                            dinput.salesInvoiceLines = new List<DynamicsSalesInvoiceLineInput>();
                            //call invoice split
                            //get invoice split by financial source and adjustedcommitment
                            List<InvoiceSplitOutputDataContract> lstSplit = new List<InvoiceSplitOutputDataContract>();
                            if (invoiceDC != null && invoiceDC.IsApplySplit == true)
                            {
                                InvoiceSplitParamDataContract param = new InvoiceSplitParamDataContract();
                                param.DealID = DrawFeeDC.CreDealID;
                                param.InvoiceTypeID = DrawFeeDC.InvoiceTypeID;
                                param.FeeAmount = DrawFeeDC.Amount;
                                lstSplit = _wfLogic.GetInvoiceSplit(param, headerUserID);
                                if (lstSplit.Count > 0)
                                {
                                    foreach (InvoiceSplitOutputDataContract itm in lstSplit)
                                    {
                                        dinput.salesInvoiceLines.Add(
                                        new DynamicsSalesInvoiceLineInput
                                        {
                                            lineType = "Account",
                                            description = itm.QBItemName,
                                            lineObjectNumber = itm.QBAccountNo,
                                            quantity = 1,
                                            unitPrice = itm.SplitAmount
                                        }
                                        );
                                    }
                                }
                            }
                            else
                            {
                                dinput.salesInvoiceLines.Add(
                                        new DynamicsSalesInvoiceLineInput
                                        {
                                            lineType = "Account",
                                            description = invoiceDC.InvoiceCode,
                                            lineObjectNumber = invoiceDC.InvoiceAccountNo,
                                            quantity = 1,
                                            unitPrice = DrawFeeDC.Amount
                                        }
                                        );
                            }

                            string inputjsonstring = JsonConvert.SerializeObject(dinput);
                            HttpResponseMessage result = null;
                            HttpClient _httpClient = new HttpClient();
                            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            //Set the Authorization header with the Access Token received specifying the Credentials
                            using (var content = new StringContent(inputjsonstring, System.Text.Encoding.UTF8, "application/json"))
                            {
                                result = _httpClient.PostAsync(BusinessCentralAPIURL, content).Result;
                                if (result.StatusCode == System.Net.HttpStatusCode.Created || result.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    string resultString = result.Content.ReadAsStringAsync().Result;
                                    var jsonOutputResult = JsonConvert.DeserializeObject<DynamicsSalesInvoiceOutput>(resultString);
                                    DrawFeeDC.IsSuccess = true;
                                    DrawFeeDC.InvoiceNo = jsonOutputResult.number;
                                    DrawFeeDC.PreAssignedInvoiceNo = jsonOutputResult.number;
                                    DrawFeeDC.InvoiceGuid = jsonOutputResult.id;
                                }

                                else
                                {
                                    string resultString = result.Content.ReadAsStringAsync().Result;
                                    var jsonResultError = JsonConvert.DeserializeObject<error>(resultString);
                                    string errmessage = string.IsNullOrEmpty(jsonResultError.message) ? resultString : jsonResultError.message;
                                    errmessage = errmessage + "-Url-" + BusinessCentralAPIURL + "-status code-" + result.StatusCode;
                                    LoggerLogic Log = new LoggerLogic();
                                    Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in dynamics for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), "", headerUserID.ToString(), "CreateInvoiceInQBD", errmessage);
                                    EmailDataContract emaildc = new EmailDataContract();
                                    emaildc.ModuleId = 705;
                                    emaildc.Subject = "Dynamics 365 Invoice Error Notification";
                                    emaildc.Body = "Error occurred while creating invoice in Dynamics for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString() + " - " + errmessage;
                                    _iEmailNotification.SendErrorNotificationEmail(emaildc);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in Dynamics for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);
                        DrawFeeDC.IsSuccess = false;
                        EmailDataContract emaildc = new EmailDataContract();
                        emaildc.ModuleId = 705;
                        emaildc.Subject = "Dynamics 365 Invoice Error Notification";
                        emaildc.Body = "Error occurred while creating invoice in QBD for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString() + " - " + ex.Message;
                        _iEmailNotification.SendErrorNotificationEmail(emaildc);
                    }
                }

            }
            return DrawFeeDC;
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/wfcontroller/saveFeeInvoices")]
        public IActionResult SaveFeeInvoices([FromBody] DataTable dtFeeInvoice)
        {
            GenericResult _authenticationResult = null;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            WFLogic _wfLogic = new WFLogic();
            _wfLogic.SaveFeeInvoices(dtFeeInvoice, headerUserID.ToString());

            try
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Saved fee invoices successfully."

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

        //send notification on payoff completed
        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/wfcontroller/sendwfnotificationfornegativeamt")]
        public IActionResult SendWFNotificationForNegativeAmt([FromBody] WFNotificationDetailDataContract _wfDetailDataContract)
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            var delegatedUserID = string.Empty;
            string htmlContent = "";

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            _wfDetailDataContract.CreatedBy = headerUserID;
            WFLogic _wfLogic = new WFLogic();
            try
            {
                _wfDetailDataContract.MessageHTML = Regex.Replace(_wfDetailDataContract.MessageHTML, @"\r\n?|\n", "<br />");
                if (!string.IsNullOrEmpty(_wfDetailDataContract.EmailToIds))
                {
                    _wfDetailDataContract.EmailToIds = _wfDetailDataContract.EmailToIds.Replace(";", ",");
                }
                if (!string.IsNullOrEmpty(_wfDetailDataContract.EmailCCIds))
                {
                    _wfDetailDataContract.EmailCCIds = _wfDetailDataContract.EmailCCIds.Replace(";", ",");
                }
                //LogDB("sending email");
                _iEmailNotification.SendWFNotificationForNegativeAmt(_wfDetailDataContract, out htmlContent);
            }
            catch (Exception ex)
            {
                //LogDB(ex.Message);
            }
            _authenticationResult = new GenericResult()
            {
                Succeeded = true
            };
            return Ok(_authenticationResult);
        }

        [HttpGet]
        [Route("api/wfcontroller/syncQuickbook")]
        public IActionResult SyncQuickbook()
        {
            GenericResult _authenticationResult = null;
            if (IsUseDynamicForInvoice())
            {
                GetConfigRoot();
                DynamicsAuthToken = GetBusinessCentralAccessToken();

                if (string.IsNullOrEmpty(DynamicsAuthToken))
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Dynamics Authentication failed."
                    };
                    return Ok(_authenticationResult);
                }
            }

            Thread FirstThread = new Thread(() => UpdateInvoiceQueuedToInvoicedAndInVoiceStatus());
            FirstThread.Start();
            _authenticationResult = new GenericResult()
            {
                Succeeded = true,
                Message = "Quickbook synced successfully."
            };

            /*
            
            try {
                var updateinvoicequeued = UpdateInvoiceQueuedToInvoiced();
                
                if (updateinvoicequeued != null)
                {
                    var updateinvoice = UpdateDrawFeeInvoiceStatus();
                    if (updateinvoice != null)
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "Quickbook synced successfully."
                        };
                    }
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
            */
            return Ok(_authenticationResult);

        }

        public void UpdateInvoiceQueuedToInvoicedAndInVoiceStatus()
        {
            UpdateInvoiceQueuedToInvoiced();
            UpdateDrawFeeInvoiceStatus();
            //process invoice uploaded by batch
            ProcessInvoiceUploadedByBatch();
            //create invoice in sandbox for testing-need remove later
            //InvoiceQueuedToInvoicedForSandBox();
            //
            SendInvoiceSyncSuccessNotification();
        }

        public void CreateMissingQBDCustomer()
        {
            GetConfigSetting();
            List<DrawFeeInvoiceDataContract> lstdrawDC = new List<DrawFeeInvoiceDataContract>();
            WFLogic wfLogic = new WFLogic();
            lstdrawDC = wfLogic.GetMissingQBDCustomer();
            IActionResult ActionResult = null;

            if (!IsUseDynamicForInvoice())
            {
                foreach (DrawFeeInvoiceDataContract drawFeeDC in lstdrawDC)
                {
                    try
                    {
                        QBDCustomerInputDataContract qbdcustomer = new QBDCustomerInputDataContract();
                        qbdcustomer.FirstName = drawFeeDC.FirstName;
                        qbdcustomer.LastName = drawFeeDC.LastName;
                        qbdcustomer.CompanyName = drawFeeDC.CompanyName;
                        qbdcustomer.Name = CommonHelper.FormatCustomerForQuickBook(drawFeeDC.DealName, drawFeeDC.CreDealID);
                        qbdcustomer.FullName = qbdcustomer.Name;
                        qbdcustomer.BillingAddress = FormateQBDCustomerAddress(drawFeeDC.Address);
                        qbdcustomer.BillingAddress.City = drawFeeDC.City;
                        qbdcustomer.BillingAddress.State = drawFeeDC.State;
                        qbdcustomer.BillingAddress.PostalCode = drawFeeDC.Zip;

                        //call customer api

                        string inputjsonstring = JsonConvert.SerializeObject(qbdcustomer);


                        // var jsonInputResult = JsonConvert.DeserializeObject<AutofyInvoiceInputDataContract>(jsonInput);
                        HttpClient _httpClient = new HttpClient();
                        //using (var content = new StringContent(JsonConvert.SerializeObject(jsonInput), System.Text.Encoding.UTF8, "application/json"))
                        using (var content = new StringContent(JsonConvert.SerializeObject(inputjsonstring), System.Text.Encoding.UTF8, "application/json"))
                        {
                            string Autofykey = Sectionroot.GetSection("Autofykey").Value;
                            string SchemaVersion = Sectionroot.GetSection("SchemaVersion").Value;
                            string AutofyBaseURL = Sectionroot.GetSection("AutofyBaseURL").Value;
                            string AutofyCompanyEndPoint = Sectionroot.GetSection("AutofyCompanyEndPoint").Value;
                            content.Headers.Add("x-api-key", Autofykey);
                            content.Headers.Add("SchemaVersion", "20.04");
                            string AutoFURL = AutofyBaseURL + "data/" + AutofyCompanyEndPoint + "/customer";
                            HttpResponseMessage result = _httpClient.PostAsync(AutoFURL, content).Result;
                            if (result.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                string resultString = result.Content.ReadAsStringAsync().Result;
                                var jsonOutputResult = JsonConvert.DeserializeObject<QBDCustomerOutputDataContract>(resultString);
                                if (string.IsNullOrEmpty(jsonOutputResult.HasErrors) || jsonOutputResult.HasErrors == "false")
                                {
                                    qbdcustomer.ID = jsonOutputResult.Contents[0].Object.ID;
                                    wfLogic.AddUpdateQBDCustomer("", qbdcustomer);
                                }
                                //if customer already there in the quickbook but not in our db then add in our db
                                else if (jsonOutputResult.Contents[0].Errors != null && jsonOutputResult.Contents[0].Errors.Count > 0
                                    && jsonOutputResult.Contents[0].Errors[0].Message.Contains("the list element is already in use")
                                    )
                                {

                                    AutoFURL = AutofyBaseURL + "data/" + AutofyCompanyEndPoint + "/customer?name=" + WebUtility.UrlEncode(qbdcustomer.Name);
                                    _httpClient.DefaultRequestHeaders.Add("x-api-key", Autofykey);
                                    _httpClient.DefaultRequestHeaders.Add("SchemaVersion", "20.04");
                                    HttpResponseMessage resultcust = _httpClient.GetAsync(AutoFURL).Result;
                                    if (resultcust.StatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        string resultStringcust = resultcust.Content.ReadAsStringAsync().Result;
                                        var jsonOutputResultcust = JsonConvert.DeserializeObject<QBDCustomerOutputDataContract>(resultStringcust);

                                        if (string.IsNullOrEmpty(jsonOutputResultcust.HasErrors) || jsonOutputResultcust.HasErrors == "false")
                                        {
                                            qbdcustomer.ID = jsonOutputResultcust.Contents[0].Object.ID;
                                            wfLogic.AddUpdateQBDCustomer("", qbdcustomer);
                                        }
                                    }
                                }
                                else
                                {
                                    string errormsg = "";
                                    if (jsonOutputResult.Errors != null && jsonOutputResult.Errors.Count > 0)
                                        errormsg = jsonOutputResult.Errors[0].Message;
                                    else if (jsonOutputResult.Contents != null && jsonOutputResult.Contents.Count > 0 && jsonOutputResult.Contents[0].Errors != null
                                        && jsonOutputResult.Contents[0].Errors.Count > 0)
                                        errormsg = jsonOutputResult.Contents[0].Errors[0].Message;

                                    if (!string.IsNullOrEmpty(errormsg))
                                    {
                                        LoggerLogic Log = new LoggerLogic();
                                        Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating customer " + drawFeeDC.DealName + " in QBD from scheduler", "", "", "CreateMissingQBDCustomer", errormsg);
                                    }
                                }
                            }
                            else
                            {
                                LoggerLogic Log = new LoggerLogic();
                                Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating customer " + drawFeeDC.DealName + " in QBD from scheduler", "", "", "CreateMissingQBDCustomer", result.ReasonPhrase);

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating customer " + drawFeeDC.DealName + " in QBD from scheduler", "", "", ex.TargetSite.Name.ToString(), "", ex);

                    }
                }
            }
            else
            {
                foreach (DrawFeeInvoiceDataContract drawFeeDC in lstdrawDC)
                {
                    drawFeeDC.CreatedFrom = "Scheduler";
                    ActionResult = CreateQBDCustomer(drawFeeDC);
                }
            }
        }


        [HttpGet]
        [Route("api/wfcontroller/generatemissingpdfforinvoiced")]
        public IActionResult GenerateMissingPDFForInvoiced()
        {

            GenericResult _authenticationResult = null;
            WFLogic _wfLogic = new WFLogic();
            List<DrawFeeInvoiceDataContract> lstDraw = new List<DrawFeeInvoiceDataContract>();
            DrawFeeInvoiceDataContract DrawFeeDC = null;

            try
            {

                GetConfigSetting();
                lstDraw = _wfLogic.GeAllInvoicedMissingPDF("");
                //call invoice api and update status of invoice
                foreach (DrawFeeInvoiceDataContract df in lstDraw)
                {
                    try
                    {
                        DrawFeeDC = df;
                        string pdfFileName = CreateJsonToPDF(DrawFeeDC);
                        DrawFeeDC.FileName = pdfFileName;
                        DrawFeeDC.IsLogActivity = false;
                        _wfLogic.UpdateDrawFeeInvoiceDetailStatus("", DrawFeeDC);
                        //send draw fee invoice to email
                        AttachInvoiceSendEmail(DrawFeeDC);
                    }
                    catch (Exception ex)
                    {
                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while generating missing pdf from scheduler for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), "", ex.TargetSite.Name.ToString(), "", ex);
                    }

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

        [HttpGet]
        [Route("api/wfcontroller/generatemissingpdfforinvoicedwithoutemail")]
        public IActionResult GenerateMissingPDFForInvoicedWithoutEmail()
        {

            GenericResult _authenticationResult = null;
            WFLogic _wfLogic = new WFLogic();
            List<DrawFeeInvoiceDataContract> lstDraw = new List<DrawFeeInvoiceDataContract>();
            DrawFeeInvoiceDataContract DrawFeeDC = null;

            try
            {

                GetConfigSetting();
                lstDraw = _wfLogic.GeAllInvoicedMissingPDF("");
                //call invoice api and update status of invoice
                foreach (DrawFeeInvoiceDataContract df in lstDraw)
                {
                    try
                    {
                        DrawFeeDC = df;
                        string pdfFileName = CreateJsonToPDF(DrawFeeDC);
                        DrawFeeDC.FileName = pdfFileName;
                        DrawFeeDC.IsLogActivity = false;
                        _wfLogic.UpdateDrawFeeInvoiceDetailStatus("", DrawFeeDC);
                    }
                    catch (Exception ex)
                    {
                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while generating missing pdf from scheduler for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), "", ex.TargetSite.Name.ToString(), "", ex);
                    }

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
        //sync success notification
        public void SendInvoiceSyncSuccessNotification()
        {
            EmailDataContract emaildc = new EmailDataContract();
            emaildc.ModuleId = 707;
            emaildc.Subject = "Invoice data sync status";
            emaildc.Body = "Invoice data sync process completed successfully.";
            _iEmailNotification.SendInvoiceSyncNotification(emaildc);

        }

        public void CreateMissingQBDCustomerFromBatch()
        {
            GetConfigSetting();
            List<DrawFeeInvoiceDataContract> lstdrawDC = new List<DrawFeeInvoiceDataContract>();
            IActionResult actionResult = null;
            WFLogic wfLogic = new WFLogic();
            lstdrawDC = wfLogic.GetMissingQBDCustomerFromBatch();




            if (!IsUseDynamicForInvoice())
            {
                foreach (DrawFeeInvoiceDataContract drawFeeDC in lstdrawDC)
                {
                    try
                    {
                        QBDCustomerInputDataContract qbdcustomer = new QBDCustomerInputDataContract();
                        qbdcustomer.FirstName = drawFeeDC.FirstName;
                        qbdcustomer.LastName = drawFeeDC.LastName;
                        qbdcustomer.CompanyName = drawFeeDC.CompanyName;
                        qbdcustomer.Name = CommonHelper.FormatCustomerForQuickBook(drawFeeDC.DealName, drawFeeDC.CreDealID);
                        qbdcustomer.FullName = qbdcustomer.Name;
                        qbdcustomer.BillingAddress = FormateQBDCustomerAddress(drawFeeDC.Address);
                        qbdcustomer.BillingAddress.City = drawFeeDC.City;
                        qbdcustomer.BillingAddress.State = drawFeeDC.State;
                        qbdcustomer.BillingAddress.PostalCode = drawFeeDC.Zip;

                        //call customer api

                        string inputjsonstring = JsonConvert.SerializeObject(qbdcustomer);


                        // var jsonInputResult = JsonConvert.DeserializeObject<AutofyInvoiceInputDataContract>(jsonInput);
                        HttpClient _httpClient = new HttpClient();
                        //using (var content = new StringContent(JsonConvert.SerializeObject(jsonInput), System.Text.Encoding.UTF8, "application/json"))
                        using (var content = new StringContent(JsonConvert.SerializeObject(inputjsonstring), System.Text.Encoding.UTF8, "application/json"))
                        {
                            string Autofykey = Sectionroot.GetSection("Autofykey").Value;
                            string SchemaVersion = Sectionroot.GetSection("SchemaVersion").Value;
                            string AutofyBaseURL = Sectionroot.GetSection("AutofyBaseURL").Value;
                            string AutofyCompanyEndPoint = Sectionroot.GetSection("AutofyCompanyEndPoint").Value;
                            content.Headers.Add("x-api-key", Autofykey);
                            content.Headers.Add("SchemaVersion", "20.04");
                            string AutoFURL = AutofyBaseURL + "data/" + AutofyCompanyEndPoint + "/customer";
                            HttpResponseMessage result = _httpClient.PostAsync(AutoFURL, content).Result;
                            if (result.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                string resultString = result.Content.ReadAsStringAsync().Result;
                                var jsonOutputResult = JsonConvert.DeserializeObject<QBDCustomerOutputDataContract>(resultString);
                                if (string.IsNullOrEmpty(jsonOutputResult.HasErrors) || jsonOutputResult.HasErrors == "false")
                                {
                                    qbdcustomer.ID = jsonOutputResult.Contents[0].Object.ID;
                                    wfLogic.AddUpdateQBDCustomer("", qbdcustomer);
                                }
                                //if customer already there in the quickbook but not in our db then add in our db
                                else if (jsonOutputResult.Contents[0].Errors != null && jsonOutputResult.Contents[0].Errors.Count > 0
                                    && jsonOutputResult.Contents[0].Errors[0].Message.Contains("the list element is already in use")
                                    )
                                {

                                    AutoFURL = AutofyBaseURL + "data/" + AutofyCompanyEndPoint + "/customer?name=" + WebUtility.UrlEncode(qbdcustomer.Name);
                                    _httpClient.DefaultRequestHeaders.Add("x-api-key", Autofykey);
                                    _httpClient.DefaultRequestHeaders.Add("SchemaVersion", "20.04");
                                    HttpResponseMessage resultcust = _httpClient.GetAsync(AutoFURL).Result;
                                    if (resultcust.StatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        string resultStringcust = resultcust.Content.ReadAsStringAsync().Result;
                                        var jsonOutputResultcust = JsonConvert.DeserializeObject<QBDCustomerOutputDataContract>(resultStringcust);

                                        if (string.IsNullOrEmpty(jsonOutputResultcust.HasErrors) || jsonOutputResultcust.HasErrors == "false")
                                        {
                                            qbdcustomer.ID = jsonOutputResultcust.Contents[0].Object.ID;
                                            wfLogic.AddUpdateQBDCustomer("", qbdcustomer);
                                        }
                                    }
                                }
                                else
                                {
                                    string errormsg = "";
                                    if (jsonOutputResult.Errors != null && jsonOutputResult.Errors.Count > 0)
                                        errormsg = jsonOutputResult.Errors[0].Message;
                                    else if (jsonOutputResult.Contents != null && jsonOutputResult.Contents.Count > 0 && jsonOutputResult.Contents[0].Errors != null
                                        && jsonOutputResult.Contents[0].Errors.Count > 0)
                                        errormsg = jsonOutputResult.Contents[0].Errors[0].Message;

                                    if (!string.IsNullOrEmpty(errormsg))
                                    {
                                        LoggerLogic Log = new LoggerLogic();
                                        Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating customer " + drawFeeDC.DealName + " in QBD from scheduler", "", "", "CreateMissingQBDCustomer", errormsg);
                                    }
                                }
                            }
                            else
                            {
                                LoggerLogic Log = new LoggerLogic();
                                Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating customer " + drawFeeDC.DealName + " in QBD from scheduler", "", "", "CreateMissingQBDCustomer", result.ReasonPhrase);

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating customer " + drawFeeDC.DealName + " in QBD from scheduler", "", "", ex.TargetSite.Name.ToString(), "", ex);

                    }
                }
            }
            else
            {
                foreach (DrawFeeInvoiceDataContract drawFeeDC in lstdrawDC)
                {
                    drawFeeDC.CreatedFrom = "Scheduler";
                    actionResult = CreateQBDCustomer(drawFeeDC);
                }
            }
        }


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/wfcontroller/getreserveschedulebreakdown")]
        //Get Reserve Schedule BreakDown amout
        public IActionResult GetReserveScheduleBreakDown([FromBody] WFDetailDataContract _wfDetailDataContract)
        {
            GenericResult _authenticationResult = null;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            WFLogic _wfLogic = new WFLogic();
            List<ReserveAccountDataContract> lstReserveAccountbreakdown = new List<ReserveAccountDataContract>();

            lstReserveAccountbreakdown = _wfLogic.GetReserveScheduleBreakDown(_wfDetailDataContract, headerUserID.ToString());

            //}

            try
            {
                if (_wfDetailDataContract != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        ListReserveScheduleBreakDown = lstReserveAccountbreakdown

                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false
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
        public IActionResult CreateInvoiceFromBatchData()
        {
            GenericResult _authenticationResult = null;
            WFLogic _wfLogic = new WFLogic();
            List<DrawFeeInvoiceDataContract> lstDraw = new List<DrawFeeInvoiceDataContract>();
            DrawFeeInvoiceDataContract DrawFeeDC = null;
            var headerUserID = string.Empty;

            try
            {
                GetConfigSetting();
                CreateMissingQBDCustomerFromBatch();
                lstDraw = _wfLogic.GetAllBatchInvoice("");
                foreach (DrawFeeInvoiceDataContract df in lstDraw)
                {
                    CreateInvoiceForBatch(df, headerUserID);
                }
            }
            catch (Exception ex)
            {

                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = ex.Message
                };


                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in QBD ", "", "", ex.TargetSite.Name.ToString(), "", ex);

                EmailDataContract emaildc = new EmailDataContract();
                emaildc.ModuleId = 705;
                emaildc.Subject = "Quickbook Invoice Error Notification";
                emaildc.Body = "Error occurred while creating invoice in QBD for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString() + " - " + ex.Message;
                _iEmailNotification.SendErrorNotificationEmail(emaildc);
            }

            return Ok(_authenticationResult);
        }

        public DrawFeeInvoiceDataContract CreateInvoiceInQBDFromBatch([FromBody] DrawFeeInvoiceDataContract DrawFeeDC)
        {
            // generate invoice in below cases
            //if autosend invoice from workflow
            //if manual from dealdetail
            if (!IsUseDynamicForInvoice())
            {
                if (string.IsNullOrEmpty(DrawFeeDC.InvoiceNo) == true)
                {
                    GetConfigSetting();
                    var headerUserID = string.Empty;
                    //if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                    //{
                    //    headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
                    //}

                    try
                    {
                        WFLogic _wfLogicInvoiceConfig = new WFLogic();
                        InvoiceConfigDataContract invoiceDC = new InvoiceConfigDataContract();
                        invoiceDC = _wfLogicInvoiceConfig.GetInvoiceConfigByInvoiceType(DrawFeeDC.InvoiceTypeID, "");
                        if (invoiceDC != null && !string.IsNullOrEmpty(invoiceDC.InvoiceCode))
                        {
                            DrawFeeDC.InvoiceCode = invoiceDC.InvoiceCode;
                            DrawFeeDC.TemplateName = invoiceDC.Template;
                        }


                        WFLogic _wfLogic = new WFLogic();
                        if (string.IsNullOrEmpty(DrawFeeDC.InvoiceNo) == true)
                        {
                            AutofyInvoiceInputDataContract _autofyInput = new AutofyInvoiceInputDataContract();
                            //_autofyInput.Number = DrawFeeDC.CreDealID + "-DW-" + DrawFeeDC.DrawNo;
                            //Random generator = new Random();
                            //String rNum = generator.Next(0, 1000000).ToString("D6");
                            //_autofyInput.Number = rNum;
                            _autofyInput.Customer = new Customer();
                            _autofyInput.Customer.FullName = CommonHelper.FormatCustomerForQuickBook(DrawFeeDC.DealName, DrawFeeDC.CreDealID);
                            _autofyInput.Date = DrawFeeDC.InvoiceDate;
                            _autofyInput.DueDate = DrawFeeDC.InvoiceDate;
                            //_autofyInput.SubTotal = 201;
                            //_autofyInput.Balance = 3000;
                            _autofyInput.ToBePrinted = true;
                            _autofyInput.ToBeEmailed = false;
                            _autofyInput.ItemLines = new List<ItemLines>();
                            //split functionality
                            List<InvoiceSplitOutputDataContract> lstSplit = new List<InvoiceSplitOutputDataContract>();
                            if (invoiceDC != null && invoiceDC.IsApplySplit == true)
                            {
                                InvoiceSplitParamDataContract param = new InvoiceSplitParamDataContract();
                                param.DealID = DrawFeeDC.CreDealID;
                                param.InvoiceTypeID = DrawFeeDC.InvoiceTypeID;
                                param.FeeAmount = DrawFeeDC.Amount;
                                lstSplit = _wfLogic.GetInvoiceSplit(param, headerUserID);
                                if (lstSplit.Count > 0)
                                {
                                    foreach (InvoiceSplitOutputDataContract itm in lstSplit)
                                    {
                                        _autofyInput.ItemLines.Add(
                                        new ItemLines { Description = DrawFeeDC.DrawNo, Amount = Convert.ToDecimal(itm.SplitAmount), Rate = Convert.ToDecimal(itm.SplitAmount), Item = new Item() { FullName = itm.QBItemName } }
                                        );
                                    }
                                }
                            }
                            else
                            {
                                _autofyInput.ItemLines.Add(
                                    new ItemLines { Description = DrawFeeDC.DrawNo, Amount = Convert.ToDecimal(DrawFeeDC.Amount), Rate = Convert.ToDecimal(DrawFeeDC.Amount), Item = new Item() { FullName = DrawFeeDC.InvoiceCode } }
                                    );
                            }

                            //
                            _autofyInput.BillingAddress = new BillingAddress();
                            _autofyInput.BillingAddress.City = DrawFeeDC.City;
                            _autofyInput.BillingAddress.State = DrawFeeDC.State;
                            _autofyInput.IsPaid = false;
                            _autofyInput.Template = new Template();
                            _autofyInput.Template.FullName = DrawFeeDC.TemplateName;
                            _autofyInput.Memo = DrawFeeDC.InvoiceNoUI;
                            _autofyInput.DateShipped = DateTime.Now;
                            string inputjsonstring = JsonConvert.SerializeObject(_autofyInput);

                            // var jsonInputResult = JsonConvert.DeserializeObject<AutofyInvoiceInputDataContract>(jsonInput);
                            HttpClient _httpClient = new HttpClient();
                            //using (var content = new StringContent(JsonConvert.SerializeObject(jsonInput), System.Text.Encoding.UTF8, "application/json"))
                            using (var content = new StringContent(JsonConvert.SerializeObject(inputjsonstring), System.Text.Encoding.UTF8, "application/json"))
                            {
                                string Autofykey = Sectionroot.GetSection("Autofykey").Value;
                                string SchemaVersion = Sectionroot.GetSection("SchemaVersion").Value;
                                string AutofyBaseURL = Sectionroot.GetSection("AutofyBaseURL").Value;
                                string AutofyCompanyEndPoint = Sectionroot.GetSection("AutofyCompanyEndPoint").Value;
                                content.Headers.Add("x-api-key", Autofykey);
                                content.Headers.Add("SchemaVersion", "20.04");
                                string AutoFURL = AutofyBaseURL + "data/" + AutofyCompanyEndPoint + "/Invoice";
                                HttpResponseMessage result = _httpClient.PostAsync(AutoFURL, content).Result;
                                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    string resultString = result.Content.ReadAsStringAsync().Result;
                                    var jsonOutputResult = JsonConvert.DeserializeObject<AutofyInvoiceOutputDataContract>(resultString);
                                    if (string.IsNullOrEmpty(jsonOutputResult.HasErrors) || jsonOutputResult.HasErrors == "false")
                                    {
                                        DrawFeeDC.IsSuccess = true;
                                        DrawFeeDC.InvoiceNo = jsonOutputResult.Contents[0].Object.Number;
                                    }
                                    else
                                    {
                                        string errorMsg = "";
                                        if (jsonOutputResult.Contents[0].Errors != null && jsonOutputResult.Contents[0].Errors.Count > 0)
                                        {
                                            errorMsg = jsonOutputResult.Contents[0].Errors[0].Message;
                                        }
                                        else if (jsonOutputResult.Errors != null && jsonOutputResult.Errors.Count > 0)
                                        {
                                            errorMsg = jsonOutputResult.Errors[0].Message;
                                        }

                                        LoggerLogic Log = new LoggerLogic();
                                        Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in QBD for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), "", headerUserID.ToString(), "CreateInvoiceInQBD", errorMsg);

                                        EmailDataContract emaildc = new EmailDataContract();
                                        emaildc.ModuleId = 705;
                                        emaildc.Subject = "Quickbook Invoice Error Notification";
                                        emaildc.Body = "Error occurred while creating invoice in QBD for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString() + " - " + errorMsg;
                                        _iEmailNotification.SendErrorNotificationEmail(emaildc);

                                    }

                                }
                                else
                                {
                                    LoggerLogic Log = new LoggerLogic();
                                    Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in QBD for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), "", headerUserID.ToString(), "CreateInvoiceInQBD", result.ReasonPhrase);
                                    EmailDataContract emaildc = new EmailDataContract();
                                    emaildc.ModuleId = 705;
                                    emaildc.Subject = "Quickbook Invoice Error Notification";
                                    emaildc.Body = "Error occurred while creating invoice in QBD for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString() + " - " + result.ReasonPhrase;
                                    _iEmailNotification.SendErrorNotificationEmail(emaildc);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in QBD for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);
                        DrawFeeDC.IsSuccess = false;
                        EmailDataContract emaildc = new EmailDataContract();
                        emaildc.ModuleId = 705;
                        emaildc.Subject = "Quickbook Invoice Error Notification";
                        emaildc.Body = "Error occurred while creating invoice in QBD for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString() + " - " + ex.Message;
                        _iEmailNotification.SendErrorNotificationEmail(emaildc);
                    }
                }
            }
            else // create invoice in dynamics
            {
                if (string.IsNullOrEmpty(DrawFeeDC.InvoiceNo) == true)
                {
                    GetConfigSetting();
                    SetDynamicsAppConfig();
                    var headerUserID = string.Empty;
                    try
                    {
                        WFLogic _wfLogicInvoiceConfig = new WFLogic();
                        InvoiceConfigDataContract invoiceDC = new InvoiceConfigDataContract();
                        invoiceDC = _wfLogicInvoiceConfig.GetInvoiceConfigByInvoiceType(DrawFeeDC.InvoiceTypeID, "");
                        if (invoiceDC != null && !string.IsNullOrEmpty(invoiceDC.InvoiceCode))
                        {
                            DrawFeeDC.InvoiceCode = invoiceDC.InvoiceCode;
                            DrawFeeDC.TemplateName = invoiceDC.Template;
                        }

                        WFLogic _wfLogic = new WFLogic();
                        if (string.IsNullOrEmpty(DrawFeeDC.InvoiceNo) == true)
                        {
                            //create invoice in dynamic 365
                            GetConfigRoot();
                            IConfiguration config = configroot.GetSection("Dynamics365BusinessCentralDetail");
                            string CompanyID = config.GetSection("CompanyID").Value;
                            string BusinessCentralAPIURL = config.GetSection("BusinessCentralBaseURL").Value
                                + "companies(" + CompanyID + ")/salesInvoices";
                            string authToken = GetBusinessCentralAccessToken();

                            if (string.IsNullOrEmpty(authToken))
                            {
                                LoggerLogic Log = new LoggerLogic();
                                Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in Dynamics for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), headerUserID.ToString(), "CreateInvoiceInQBD", "Unauthentication token");
                                DrawFeeDC.IsSuccess = false;
                                return DrawFeeDC;
                            }

                            string accountName = CommonHelper.FormatCustomerForQuickBook(DrawFeeDC.DealName, DrawFeeDC.CreDealID);
                            DynamicsCustomer dcust = _wfLogic.GetCustomerByAccountName(accountName);
                            DynamicsSalesInvoiceInput dinput = new DynamicsSalesInvoiceInput();
                            dinput.invoiceDate = DrawFeeDC.InvoiceDate.ToString("yyyy-MM-dd");
                            dinput.dueDate = DrawFeeDC.InvoiceDate.ToString("yyyy-MM-dd");
                            dinput.postingDate = DrawFeeDC.InvoiceDate.ToString("yyyy-MM-dd");
                            dinput.customerNumber = dcust.CustomerNo;
                            //dinput.currencyCode = "USD";
                            //dinput.currencyId = "00000000-0000-0000-0000-000000000000";
                            //dinput.paymentTermsId = "7e5ed7ef-a1b1-ec11-8aa5-0022482b5b4b";
                            //dinput.paymentTermsId = DynamicsAppConfig.Where(i => i.Key == "Dynamics_paymentTermsId").FirstOrDefault().Value;
                            //dinput.currencyCode = DynamicsAppConfig.Where(i => i.Key == "Dynamics_currencyCode").FirstOrDefault().Value;
                            //dinput.currencyId = DynamicsAppConfig.Where(i => i.Key == "Dynamics_currencyId").FirstOrDefault().Value;
                            dinput.externalDocumentNumber = DrawFeeDC.InvoiceNoUI;
                            dinput.customerPurchaseOrderReference = string.IsNullOrEmpty(DrawFeeDC.Comment) ? DrawFeeDC.DrawNo : DrawFeeDC.Comment;
                            if (dinput.customerPurchaseOrderReference.Length > 35)
                            {
                                dinput.customerPurchaseOrderReference = dinput.customerPurchaseOrderReference.Substring(0, 35);
                            }
                            dinput.salesInvoiceLines = new List<DynamicsSalesInvoiceLineInput>();

                            List<InvoiceSplitOutputDataContract> lstSplit = new List<InvoiceSplitOutputDataContract>();
                            if (invoiceDC != null && invoiceDC.IsApplySplit == true)
                            {
                                InvoiceSplitParamDataContract param = new InvoiceSplitParamDataContract();
                                param.DealID = DrawFeeDC.CreDealID;
                                param.InvoiceTypeID = DrawFeeDC.InvoiceTypeID;
                                param.FeeAmount = DrawFeeDC.Amount;
                                lstSplit = _wfLogic.GetInvoiceSplit(param, headerUserID);
                                if (lstSplit.Count > 0)
                                {
                                    foreach (InvoiceSplitOutputDataContract itm in lstSplit)
                                    {
                                        dinput.salesInvoiceLines.Add(
                                        new DynamicsSalesInvoiceLineInput
                                        {
                                            lineType = "Account",
                                            description = itm.QBItemName,
                                            lineObjectNumber = itm.QBAccountNo,
                                            quantity = 1,
                                            unitPrice = itm.SplitAmount
                                        }
                                        );
                                    }
                                }
                            }
                            else
                            {
                                dinput.salesInvoiceLines.Add(
                                    new DynamicsSalesInvoiceLineInput
                                    {
                                        lineType = "Account",
                                        description = invoiceDC.InvoiceCode,
                                        lineObjectNumber = invoiceDC.InvoiceAccountNo,
                                        quantity = 1,
                                        unitPrice = DrawFeeDC.Amount
                                    }
                                    );
                            }
                            string inputjsonstring = JsonConvert.SerializeObject(dinput);
                            HttpResponseMessage result = null;
                            HttpClient _httpClient = new HttpClient();
                            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            //Set the Authorization header with the Access Token received specifying the Credentials
                            using (var content = new StringContent(inputjsonstring, System.Text.Encoding.UTF8, "application/json"))
                            {
                                result = _httpClient.PostAsync(BusinessCentralAPIURL, content).Result;
                                if (result.StatusCode == System.Net.HttpStatusCode.Created || result.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    string resultString = result.Content.ReadAsStringAsync().Result;
                                    var jsonOutputResult = JsonConvert.DeserializeObject<DynamicsSalesInvoiceOutput>(resultString);
                                    DrawFeeDC.IsSuccess = true;
                                    DrawFeeDC.InvoiceNo = jsonOutputResult.number;
                                    DrawFeeDC.PreAssignedInvoiceNo = jsonOutputResult.number;
                                    DrawFeeDC.InvoiceGuid = jsonOutputResult.id;
                                }

                                else
                                {
                                    string resultString = result.Content.ReadAsStringAsync().Result;
                                    var jsonResultError = JsonConvert.DeserializeObject<error>(resultString);
                                    string errmessage = string.IsNullOrEmpty(jsonResultError.message) ? resultString : jsonResultError.message;
                                    errmessage = errmessage + "-Url-" + BusinessCentralAPIURL + "-status code-" + result.StatusCode;
                                    LoggerLogic Log = new LoggerLogic();
                                    Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in QBD for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), "", headerUserID.ToString(), "CreateInvoiceInQBD", errmessage);
                                    EmailDataContract emaildc = new EmailDataContract();
                                    emaildc.ModuleId = 705;
                                    emaildc.Subject = "Dynamics 365 Invoice Error Notification";
                                    emaildc.Body = "Error occurred while creating invoice in Dynamics for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString() + " - " + errmessage;
                                    _iEmailNotification.SendErrorNotificationEmail(emaildc);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in Dynamics for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);
                        DrawFeeDC.IsSuccess = false;
                        EmailDataContract emaildc = new EmailDataContract();
                        emaildc.ModuleId = 705;
                        emaildc.Subject = "Dynamics 365 Invoice Error Notification";
                        emaildc.Body = "Error occurred while creating invoice in QBD for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString() + " - " + ex.Message;
                        _iEmailNotification.SendErrorNotificationEmail(emaildc);
                    }
                }
            }



            return DrawFeeDC;
        }
        [HttpGet]
        [Route("api/wfcontroller/processinvoicequeuedtoinvoiced")]
        public IActionResult ProcessInvoiceQueuedToInvoiced()
        {

            GenericResult _authenticationResult = null;
            WFLogic _wfLogic = new WFLogic();
            List<DrawFeeInvoiceDataContract> lstDraw = new List<DrawFeeInvoiceDataContract>();
            DrawFeeInvoiceDataContract DrawFeeDC = null;

            try
            {

                GetConfigSetting();
                CreateMissingQBDCustomerFromBatch();
                lstDraw = _wfLogic.GeAllInvoiceQueued("");

                //call invoice api and update status of invoice
                foreach (DrawFeeInvoiceDataContract df in lstDraw)
                {
                    try
                    {
                        DrawFeeDC = df;
                        // AttachInvoiceSendEmail(df);
                        DrawFeeDC = CreateInvoiceInQBDFromBatch(df);
                        if (DrawFeeDC.IsSuccess)
                        {
                            DrawFeeDC.DrawFeeStatus = 693;
                            DrawFeeDC.IsLogActivity = true;
                            _wfLogic.UpdateDrawFeeInvoiceDetailStatus("", DrawFeeDC);
                            //DrawFeeDC.InvoiceNo = jsonOutputResult.Contents[0].Object.Number;
                            //DrawFeeDC.InvoiceNoUI = DrawFeeDC.CreDealID + "-DR-" + DrawFeeDC.DrawNo;
                            string pdfFileName = CreateJsonToPDFForBatchUpload(DrawFeeDC);
                            DrawFeeDC.FileName = pdfFileName;
                            DrawFeeDC.IsLogActivity = false;
                            _wfLogic.UpdateDrawFeeInvoiceDetailStatus("", DrawFeeDC);
                            AttachGenericInvoiceSendEmail(DrawFeeDC);
                        }
                    }
                    catch (Exception ex)
                    {

                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in QBD from batch upload for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), "", ex.TargetSite.Name.ToString(), "", ex);

                        EmailDataContract emaildc = new EmailDataContract();
                        emaildc.ModuleId = 705;
                        emaildc.Subject = "Quickbook Invoice Error Notification";
                        emaildc.Body = "Error occurred while creating invoice in QBD from batch upload for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString() + " - " + ex.Message;
                        _iEmailNotification.SendErrorNotificationEmail(emaildc);
                    }

                }
            }
            catch (Exception ex)
            {

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in Dynamics for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), "", "ProcessInvoiceQueuedToInvoiced", "", ex);

            }
            return Ok(_authenticationResult);

        }

        public void ProcessInvoiceUploadedByBatch()
        {
            //commented this for latest update
            CreateInvoiceFromBatchData();
            ProcessInvoiceQueuedToInvoiced();
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/wfcontroller/completeworkflowviascript")]
        //CompleteWorkflowViaScript
        public IActionResult CompleteWorkflowViaScript([FromBody] WFDetailDataContract _wfDetailDataContract)
        {
            GenericResult _authenticationResult = null;
            List<WFNotificationDataContract> lstWFDetail = new List<WFNotificationDataContract>();
            var headerUserID = string.Empty;
            int status = 0;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            try
            {
                WFLogic _wfLogic = new WFLogic();
                //
                WFConcurrencyParams conparams = new WFConcurrencyParams();
                conparams.TaskID = _wfDetailDataContract.TaskID.ToString();
                conparams.TaskTypeID = _wfDetailDataContract.TaskTypeID;
                conparams.WFStatusPurposeMappingID = _wfDetailDataContract.OriginalWFStatusPurposeMappingID;

                status = _wfLogic.CheckWFConcurrency(headerUserID, conparams);
                if (status == 1)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        StatusCode = status
                    };
                    return Ok(_authenticationResult);
                }
                //



                string res = _wfLogic.CompleteWorkflowViaScript(_wfDetailDataContract, headerUserID);
                string message = "Changes were saved successfully.";

                if (headerUserID != null)
                {
                    if (res == "success")
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = message
                        };
                    }
                    else
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "Some Error Occured."
                        };
                    }
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
        public QBDCustomerBillingAddress FormateQBDCustomerAddress(string address)
        {
            int AllowedLen = 41;
            int addLength = address.Length <= AllowedLen ? address.Length : AllowedLen;
            int LineNo = 1;
            QBDCustomerBillingAddress FinalAdd = new QBDCustomerBillingAddress();
            if (!string.IsNullOrEmpty(address))
            {
                while (LineNo < 5)
                {

                    if (LineNo == 1)
                    {
                        FinalAdd.Line1 = address.Substring(0, addLength);
                    }
                    if (LineNo == 2)
                    {
                        FinalAdd.Line2 = address.Substring(0, addLength);
                    }

                    if (LineNo == 3)
                    {
                        FinalAdd.Line3 = address.Substring(0, addLength);
                    }
                    if (LineNo == 4)
                    {
                        FinalAdd.Line4 = address.Substring(0, addLength);
                    }

                    if (address.Length <= 43)
                        break;
                    else
                        address = address.Remove(0, addLength);
                    addLength = address.Length <= AllowedLen ? address.Length : AllowedLen;
                    LineNo += 1;

                }
            }
            return FinalAdd;
        }

        [HttpPost]
        [Route("api/CreateInvoice")]
        public BackshopInvoiceResult CreateInvoiceFromBackshop([FromBody] dynamic inputjson)
        {

            BackshopInvoiceResult _invoiceResult = null;
            int InvoiceDeatailID = 0;
            GetConfigSetting();
            var BackshopAuthKey = Sectionroot.GetSection("BackshopAuthKey").Value;
            string Message = "";
            string UserName = "";
            var headerUserID = string.Empty;
            var headerBackshopAuthKey = string.Empty;
            string ErrorMandatory = "";
            string ErrorInvalid = "";
            string ErrorAll = "";
            string formatedEmailEmail1 = "";
            string formatedEmailEmail2 = "";
            WFLogic _wfLogic = new WFLogic();

            var ZipRegEx = @"^\d{5}(?:[-\s]\d{4})?$";
            var PhoneRexEx = @"^(\([0-9]{3}\) |[0-9]{3}-)[0-9]{3}-[0-9]{4}$";
            var DateRegEx = @"(((0|1)[0-9]|2[0-9]|3[0-1])\/(0[1-9]|1[0-2])\/((19|20)\d\d))$";
            string InvoiceNo = "";

            DataTable apidata = new DataTable();
            DynamicSizerLogic _dynamicsizer = new DynamicSizerLogic();
            try
            {

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(inputjson);
                dynamic data = JObject.Parse(jsonString);

                //var jsonData = JObject.Parse(jsonString);
                //Dictionary<string, object> dictObj = jsonData.ToObject<Dictionary<string, object>>();
                ////JArray jArray = (JArray)jsonData["Invoice"]["ReferenceData"];
                //var displayName = jsonData["Invoice"].SelectToken("ReferenceData");
                //List<GenericEntityDataContract> lstGenericEntity = new List<GenericEntityDataContract>();
                //List<JToken> tokens = jsonData.Children().Children().ToList();
                //List<JToken> tokensWithStateTrue = tokens.Select(each => each["ReferenceData"]).ToList();

                var genericEntityobject = data["Invoice"];

                if (!string.IsNullOrEmpty(Request.Headers["AuthenticationKey"]))
                {
                    headerBackshopAuthKey = Convert.ToString(Request.Headers["AuthenticationKey"]);
                }
                if (string.IsNullOrEmpty(headerBackshopAuthKey) == true || BackshopAuthKey != headerBackshopAuthKey)
                {
                    _invoiceResult = new BackshopInvoiceResult()
                    {
                        Succeeded = false,
                        Message = "User Authentication failed."
                    };
                    return _invoiceResult;
                }


                //validations

                if (genericEntityobject.DealID == null || string.IsNullOrEmpty(((Object)genericEntityobject.DealID).ToString()))
                {
                    ErrorMandatory += "DealID,";
                }
                if (genericEntityobject.InvoiceDate == null || string.IsNullOrEmpty(((Object)genericEntityobject.InvoiceDate).ToString()))
                {
                    ErrorMandatory += "InvoiceDate,";
                }
                else
                {
                    if (!IsValidDate(((Object)genericEntityobject.InvoiceDate).ToString()))
                    {
                        ErrorInvalid += "InvoiceDate,";
                    }
                }
                if (genericEntityobject.InvoiceNo == null || string.IsNullOrEmpty(((Object)genericEntityobject.InvoiceNo).ToString()))
                {
                    ErrorMandatory += "InvoiceNo,";
                }
                if (genericEntityobject.InvoiceDueDate == null || string.IsNullOrEmpty(((Object)genericEntityobject.InvoiceDueDate).ToString()))
                {
                    ErrorMandatory += "InvoiceDueDate,";
                }
                else
                {
                    if (!IsValidDate(((Object)genericEntityobject.InvoiceDueDate).ToString()))
                    {
                        ErrorInvalid += "InvoiceDueDate,";
                    }
                }
                if (genericEntityobject.Amount == null || string.IsNullOrEmpty(((Object)genericEntityobject.Amount).ToString()))
                {
                    ErrorMandatory += "Amount,";
                }
                if (genericEntityobject.InvoiceType == null || string.IsNullOrEmpty(((Object)genericEntityobject.InvoiceType).ToString()))
                {
                    ErrorMandatory += "InvoiceType,";
                }
                if (genericEntityobject.FirstName == null || string.IsNullOrEmpty(((Object)genericEntityobject.FirstName).ToString()))
                {
                    ErrorMandatory += "FirstName,";
                }
                if (genericEntityobject.LastName == null || string.IsNullOrEmpty(((Object)genericEntityobject.LastName).ToString()))
                {
                    ErrorMandatory += "LastName,";
                }
                if (genericEntityobject.CompanyName == null || string.IsNullOrEmpty(((Object)genericEntityobject.CompanyName).ToString()))
                {
                    ErrorMandatory += "CompanyName,";
                }
                if (genericEntityobject.Address == null || string.IsNullOrEmpty(((Object)genericEntityobject.Address).ToString()))
                {
                    ErrorMandatory += "Address,";
                }
                if (genericEntityobject.City == null || string.IsNullOrEmpty(((Object)genericEntityobject.City).ToString()))
                {
                    ErrorMandatory += "City,";
                }
                if (genericEntityobject.State == null || string.IsNullOrEmpty(((Object)genericEntityobject.State).ToString()))
                {
                    ErrorMandatory += "State,";
                }
                if (genericEntityobject.Email1 == null || string.IsNullOrEmpty(((Object)genericEntityobject.Email1).ToString()))
                {
                    ErrorMandatory += "Email1,";
                }
                else
                {
                    //if (!Regex.Match(((Object)genericEntityobject.Email1).ToString(), EmailRegEx).Success)

                    if (!IsEmailAddressOK(((Object)genericEntityobject.Email1).ToString(), out formatedEmailEmail1))
                    {
                        ErrorInvalid += "Email1,";
                    }
                    else
                    {
                        genericEntityobject.Email1 = formatedEmailEmail1;
                    }
                }

                if (genericEntityobject.Email2 != null && !string.IsNullOrEmpty(((Object)genericEntityobject.Email2).ToString()))
                {
                    //if (!Regex.Match(((Object)genericEntityobject.Email1).ToString(), EmailRegEx).Success)

                    if (!IsEmailAddressOK(((Object)genericEntityobject.Email2).ToString(), out formatedEmailEmail2))
                    {
                        ErrorInvalid += "Email2,";
                    }
                    else
                    {
                        genericEntityobject.Email2 = formatedEmailEmail2;
                    }
                }

                if (genericEntityobject.Zip != null && !string.IsNullOrEmpty(((Object)genericEntityobject.Zip).ToString()))
                {
                    if (!Regex.Match(((Object)genericEntityobject.Zip).ToString(), ZipRegEx).Success)
                    {
                        ErrorInvalid += "Zip,";
                    }
                }

                if (genericEntityobject.PhoneNo != null && !string.IsNullOrEmpty(((Object)genericEntityobject.PhoneNo).ToString()))
                {
                    if (!Regex.Match(((Object)genericEntityobject.PhoneNo).ToString(), PhoneRexEx).Success)
                    {
                        ErrorInvalid += "PhoneNo,";
                    }
                }
                if (genericEntityobject.AlternatePhone != null && !string.IsNullOrEmpty(((Object)genericEntityobject.AlternatePhone).ToString()))
                {
                    if (!Regex.Match(((Object)genericEntityobject.AlternatePhone).ToString(), PhoneRexEx).Success)
                    {
                        ErrorInvalid += "AlternatePhone,";
                    }
                }

                if (genericEntityobject.EmailCC != null && !string.IsNullOrEmpty(((Object)genericEntityobject.EmailCC).ToString()))
                {
                    //if (!Regex.Match(((Object)genericEntityobject.Email1).ToString(), EmailRegEx).Success)
                    string formatedEmailCC = "";
                    if (!IsEmailAddressOK(((Object)genericEntityobject.EmailCC).ToString(), out formatedEmailCC))
                    {
                        ErrorInvalid += "EmailCC,";
                    }
                    else
                    {
                        genericEntityobject.EmailCC = formatedEmailCC;
                    }
                }
                if (genericEntityobject.SenderFirstName == null || string.IsNullOrEmpty(((Object)genericEntityobject.SenderFirstName).ToString()))
                {
                    ErrorMandatory += "SenderFirstName,";
                }
                if (genericEntityobject.SenderLastName == null || string.IsNullOrEmpty(((Object)genericEntityobject.SenderLastName).ToString()))
                {
                    ErrorMandatory += "SenderLastName,";
                }
                if (genericEntityobject.SenderEmail == null || string.IsNullOrEmpty(((Object)genericEntityobject.SenderEmail).ToString()))
                {
                    ErrorMandatory += "SenderEmail,";
                }
                else
                {
                    if (!IsSingleEmailAddressOK(((Object)genericEntityobject.SenderEmail).ToString()))
                    {
                        ErrorInvalid += "SenderEmail,";
                    }
                }

                DrawFeeInvoiceDataContract dc = new DrawFeeInvoiceDataContract();
                InvoiceAPIDataContract dcInvoice = new InvoiceAPIDataContract();
                if (genericEntityobject.DealID != null)
                    dc.CreDealID = ((Object)genericEntityobject.DealID).ToString();
                if (genericEntityobject.InvoiceNo != null)
                    dc.InvoiceNoUI = ((Object)genericEntityobject.InvoiceNo).ToString();
                if (genericEntityobject.State != null)
                    dc.State = ((Object)genericEntityobject.State).ToString();
                if (genericEntityobject.InvoiceType != null)
                    dc.InvoiceTypeName = ((Object)genericEntityobject.InvoiceType).ToString();
                dcInvoice = _wfLogic.ValidateInvoiceAPIParams(dc, "");

                if (dcInvoice.StateID == 0)
                {
                    ErrorInvalid += "State,";
                }
                if (dcInvoice.InvoiceTypeID == 0)
                {
                    ErrorInvalid += "InvoiceType,";
                }


                if (!string.IsNullOrEmpty(ErrorMandatory))
                {
                    ErrorMandatory = "Mandatory field(s)- " + ErrorMandatory.Trim().Trim(',') + ".";
                    ErrorAll += ErrorMandatory;

                }
                if (!string.IsNullOrEmpty(ErrorInvalid))
                {
                    ErrorInvalid = " Field(s) with invalid format- " + ErrorInvalid.Trim().Trim(',') + ".";
                    ErrorAll += ErrorInvalid;
                }
                if (dcInvoice.IsDuplicateInvoice)
                {
                    ErrorAll += " Duplicate InvoiceNo Found.";
                }

                if (string.IsNullOrEmpty(dcInvoice.DealName))
                {
                    ErrorAll += " DealID does not exist.";
                }

                if (ErrorAll != "")
                {
                    _invoiceResult = new BackshopInvoiceResult()
                    {
                        Succeeded = false,
                        Message = ErrorAll
                    };
                    return _invoiceResult;
                }
                //create customer in qbd
                DrawFeeInvoiceDataContract _dcCust = new DrawFeeInvoiceDataContract();
                DrawFeeInvoiceDataContract _dtInvoice = new DrawFeeInvoiceDataContract();
                DrawFeeInvoiceDataContract _dcQBCompany = new DrawFeeInvoiceDataContract();
                _dcCust.DealName = CommonHelper.FormatCustomerForQuickBook(dcInvoice.DealName, dc.CreDealID);
                _dcCust.CreDealID = dc.CreDealID;
                _dcCust.FirstName = ((Object)genericEntityobject.FirstName).ToString();
                _dcCust.LastName = ((Object)genericEntityobject.LastName).ToString();
                _dcCust.CompanyName = ((Object)genericEntityobject.CompanyName).ToString();
                if (genericEntityobject.Designation != null)
                    _dcCust.Designation = ((Object)genericEntityobject.Designation).ToString();
                _dcCust.Address = ((Object)genericEntityobject.Address).ToString();
                _dcCust.City = ((Object)genericEntityobject.City).ToString();
                _dcCust.StateID = dcInvoice.StateID;
                if (!string.IsNullOrEmpty(((Object)genericEntityobject.Zip).ToString()))
                {
                    _dcCust.Zip = ((Object)genericEntityobject.Zip).ToString();
                }
                _dcCust.Email1 = ((Object)genericEntityobject.Email1).ToString();
                if (genericEntityobject.Email2 != null)
                    _dcCust.Email2 = ((Object)genericEntityobject.Email2).ToString();
                if (genericEntityobject.PhoneNo != null)
                    _dcCust.PhoneNo = ((Object)genericEntityobject.PhoneNo).ToString();
                if (genericEntityobject.AlternatePhone != null)
                    _dcCust.AlternatePhone = ((Object)genericEntityobject.AlternatePhone).ToString();
                _dcCust.SenderFirstName = ((Object)genericEntityobject.SenderFirstName).ToString();
                _dcCust.SenderLastName = ((Object)genericEntityobject.SenderLastName).ToString();
                _dcCust.SenderEmail = ((Object)genericEntityobject.SenderEmail).ToString();
                if (genericEntityobject.InvoiceDetails != null)
                    _dcCust.InvoiceComment = ((Object)genericEntityobject.InvoiceDetails).ToString();
                if (genericEntityobject.EmailCC != null)
                    _dcCust.EmailCC = ((Object)genericEntityobject.EmailCC).ToString();
                _dcCust.EmailCC = (_dcCust.EmailCC + "," + dcInvoice.AMEmails).TrimStart(',');
                _dcCust.Amount = Convert.ToDecimal((Object)genericEntityobject.Amount);
                _dcCust.InvoiceNoUI = ((Object)genericEntityobject.InvoiceNo).ToString();
                if (genericEntityobject.Comment != null)
                    _dcCust.Comment = ((Object)genericEntityobject.Comment).ToString();
                _dcCust.InvoiceDate = Convert.ToDateTime((Object)genericEntityobject.InvoiceDate);
                _dcCust.InvoiceDueDate = Convert.ToDateTime((Object)genericEntityobject.InvoiceDueDate);
                _dcCust.InvoiceTypeID = dcInvoice.InvoiceTypeID;
                _dcCust.ObjectTypeID = 697;
                _dcCust.UploadedFrom = "BackshopAPI";
                //if (!string.IsNullOrEmpty(((Object)genericEntityobject.ReferenceData).ToString()))
                //{
                //    var objectReferenceData = ((Object)genericEntityobject.ReferenceData);
                //    var dReference = data["Invoice"]["ReferenceData"];
                //    foreach (var key in dReference)
                //    {
                //        string s = ((Object)dReference.LoanModID).ToString();
                //    }
                //    for (var i = 0; i < ((Newtonsoft.Json.Linq.JContainer)dReference).Count; i++)
                //    {
                //        //var key = ((Newtonsoft.Json.Linq.JProperty)((Newtonsoft.Json.Linq.JObject)dReference).ChildrenTokens[0]).Name;
                //        //var val = ((Newtonsoft.Json.Linq.JProperty)((Newtonsoft.Json.Linq.JObject)dReference).ChildrenTokens[i]).Value.ToString();
                //    }

                //}

                _dcQBCompany = _wfLogic.CheckQBDCompanyCustomer(_dcCust);
                if (_dcQBCompany != null)
                {
                    if (_dcQBCompany.IsExistCompany)
                    {
                        CreateQBDCustomer(_dcCust);
                        //if (!_dcQBCompany.IsExistCustomer)
                        //{
                        //    CreateQBDCustomer(_dcCust);
                        //}
                        InvoiceDeatailID = _wfLogic.InsertUpdateInvoice(headerUserID, _dcCust);
                        if (InvoiceDeatailID != 0)
                        {
                            _dcCust.DrawFeeInvoiceDetailID = InvoiceDeatailID;
                            _dtInvoice = _wfLogic.GetInvoiceDetailByID("", InvoiceDeatailID);
                            _invoiceResult = new BackshopInvoiceResult();
                            _invoiceResult = CreateInvoiceForBatch(_dtInvoice, "");
                            _invoiceResult.Succeeded = true;
                            _invoiceResult.Message = "Invoice request sent successfully";
                            _invoiceResult.InvoiceNo = dc.InvoiceNoUI;
                            return _invoiceResult;
                        }
                        else
                        {
                            _invoiceResult = new BackshopInvoiceResult()
                            {
                                Succeeded = false,
                                Message = "Exception occured while inserting Invoice detail"
                            };
                            return _invoiceResult;
                        }

                    }
                    else
                    {
                        _invoiceResult = new BackshopInvoiceResult()
                        {
                            Succeeded = false,
                            Message = "This Customer does not belongs to the Quickbook company configured."
                        };
                        return _invoiceResult;
                    }
                }
                else
                {
                    _invoiceResult = new BackshopInvoiceResult()
                    {
                        Succeeded = false,
                        Message = "Quickbook company does not exist."
                    };
                    return _invoiceResult;
                }
            }
            catch (Exception ex)
            {
                _invoiceResult = new BackshopInvoiceResult()
                {
                    Succeeded = true,
                    Message = ex.Message,
                };

                //LoggerLogic Log = new LoggerLogic();
                //Log.WriteLogException(CRESEnums.Module.VSTO.ToString(), "Error occurred in vsto CreateInvoiceFromBackshop ", "", "", ex.TargetSite.Name.ToString(), "", ex);
            }

            return _invoiceResult;
        }

        public static bool IsEmailAddressOK(string Addresses, out string formatedEmail)
        {
            Addresses = Addresses.Replace(":", ",").Replace(";", ",");
            Addresses = Regex.Replace(Addresses, @",+", ",");
            string[] addressArray = Addresses.Split(',');
            bool answer = true;
            try
            {
                foreach (string address in addressArray)
                {
                    MailAddress Addy = new MailAddress(address);
                }
            }
            catch (Exception)
            {
                answer = false;
            }
            formatedEmail = Addresses;
            return answer;
        }
        public static bool IsSingleEmailAddressOK(string Addresses)
        {
            bool answer = true;
            try
            {
                MailAddress Addy = new MailAddress(Addresses);
            }
            catch (Exception)
            {
                answer = false;
            }
            return answer;
        }
        public static bool IsValidDate(string dt)
        {
            DateTime Test;
            if (DateTime.TryParseExact(dt, "MM/dd/yyyy", null, DateTimeStyles.None, out Test) == true)
                return true;
            else
                return false;
        }
        public BackshopInvoiceResult CreateInvoiceForBatch(DrawFeeInvoiceDataContract DrawFeeDC, string headerUserID)
        {
            WFLogic _wfLogic = new WFLogic();
            BackshopInvoiceResult invoiceResult = new BackshopInvoiceResult();
            try
            {
                if (string.IsNullOrEmpty(DrawFeeDC.InvoiceNo))
                {
                    //if draw date is greater than wire confirmed date than change invoice status to “Invoice Queued” 
                    //and the Draw Fee invoice will be sent at 5 PM EST on the Funding Date.
                    //this logic is only for the Draw fee(InvoiceTypeID== 558)
                    if (DrawFeeDC.InvoiceDate.Date > DrawFeeDC.CurrentDate.Date && DrawFeeDC.InvoiceTypeID == 558)
                    {
                        DrawFeeDC.DrawFeeStatus = 696;
                        if (DrawFeeDC.DrawFeeInvoiceDetailID == 0 || string.IsNullOrEmpty(DrawFeeDC.FirstName))
                        {
                            DrawFeeInvoiceDataContract drawDC = new DrawFeeInvoiceDataContract();
                            drawDC = _wfLogic.GetInvoiceDetailByObjectTypeID(DrawFeeDC.ObjectTypeID, DrawFeeDC.ObjectID.ToString(), headerUserID.ToString());
                            DrawFeeDC.DrawFeeInvoiceDetailID = drawDC.DrawFeeInvoiceDetailID;
                        }
                        DrawFeeDC.IsLogActivity = true;
                        _wfLogic.UpdateDrawFeeInvoiceDetailStatus(headerUserID, DrawFeeDC);
                    }
                    else
                    {
                        //if draw date is less than wire confirmed date than 
                        //Draw Fee will be sent right away, and the Draw Fee Invoice status will changed to “Invoiced”
                        DrawFeeDC = CreateInvoiceInQBDFromBatch(DrawFeeDC);
                        if (DrawFeeDC.IsSuccess)
                        {
                            DrawFeeDC.DrawFeeStatus = 693;
                            DrawFeeDC.IsLogActivity = true;
                            _wfLogic.UpdateDrawFeeInvoiceDetailStatus(headerUserID, DrawFeeDC);
                            //DrawFeeDC.InvoiceNo = jsonOutputResult.Contents[0].Object.Number;
                            //DrawFeeDC.InvoiceNoUI = DrawFeeDC.CreDealID + "-DR-" + DrawFeeDC.DrawNo;
                            string pdfFileName = CreateJsonToPDFForBatchUpload(DrawFeeDC);
                            DrawFeeDC.FileName = pdfFileName;
                            DrawFeeDC.IsLogActivity = false;
                            _wfLogic.UpdateDrawFeeInvoiceDetailStatus(headerUserID, DrawFeeDC);
                            invoiceResult.InvoiceNo = DrawFeeDC.InvoiceNo;
                            invoiceResult.FileName = DrawFeeDC.FileName;
                            AttachGenericInvoiceSendEmail(DrawFeeDC);

                        }
                        else//if creating invoice is not successfull then put it in the queue(update invoice status as invoice queued)
                        {
                            DrawFeeDC.DrawFeeStatus = 696;
                            if (DrawFeeDC.DrawFeeInvoiceDetailID == 0 || string.IsNullOrEmpty(DrawFeeDC.FirstName))
                            {
                                DrawFeeInvoiceDataContract drawDC = new DrawFeeInvoiceDataContract();
                                drawDC = _wfLogic.GetDrawFeeInvoiceDetailByTaskID(DrawFeeDC.TaskID.ToString(), headerUserID.ToString());
                                DrawFeeDC.DrawFeeInvoiceDetailID = drawDC.DrawFeeInvoiceDetailID;
                            }
                            DrawFeeDC.IsLogActivity = true;
                            _wfLogic.UpdateDrawFeeInvoiceDetailStatus(headerUserID, DrawFeeDC);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in QBD from batch upload for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), "", ex.TargetSite.Name.ToString(), "", ex);

                EmailDataContract emaildc = new EmailDataContract();
                emaildc.ModuleId = 705;
                emaildc.Subject = "Quickbook Invoice Error Notification";
                emaildc.Body = "Error occurred while creating invoice in QBD from batch upload for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString() + " - " + ex.Message;
                _iEmailNotification.SendErrorNotificationEmail(emaildc);
            }
            return invoiceResult;
        }

        public string GetBusinessCentralAccessToken()
        {
            if (!string.IsNullOrEmpty(DynamicsAuthToken))
            {
                return DynamicsAuthToken;
            }
            try
            {
                /* generate token by user crediantial
                var Root = configroot.GetSection("Dynamics365BusinessCentralDetail");
                var Username = Root.GetSection("Username").Value;
                var Password = Root.GetSection("Password").Value;
                //prod act
                //var tenantId = "77be5eb1-c09a-4093-b65b-a73ae39864d9";
                //var clientId = "36703c6e-b6fc-4abe-9ba6-b8af9cc1bfc4";
                //power bi act
                var tenantId = Root.GetSection("AzurApptenantId").Value;
                var clientId = Root.GetSection("AzurAppClientId").Value;
                var authorityUri = Root.GetSection("AuthorityUrl").Value + tenantId;
                var redirectUri = Root.GetSection("RedirectUrl").Value;
                var scopes = new List<string> { Root.GetSection("Scope" +
                "URL").Value };
                var publicClient = Microsoft.Identity.Client.PublicClientApplicationBuilder
                              .Create(clientId)
                              .WithAuthority(new Uri(authorityUri))
                              .WithRedirectUri(redirectUri)
                              .Build();
                SecureString secureStringPwd = new SecureString();
                foreach (char c in Password.ToCharArray())
                {
                    secureStringPwd.AppendChar(c);
                }
                var accessTokenRequest = publicClient.AcquireTokenByUsernamePassword(scopes, Username, secureStringPwd);
                DynamicsAuthToken = accessTokenRequest.ExecuteAsync().Result.AccessToken;
                */

                //generate token by app secret key
                DynamicsAuthToken = Task.Run(GetDynamicsTokenAsync).Result;
                //
            }
            catch (Exception ex)
            {
                DynamicsAuthToken = "";
            }
            return DynamicsAuthToken;
        }

        public bool IsUseDynamicForInvoice()
        {
            AppConfigLogic appl = new CRES.BusinessLogic.AppConfigLogic();
            List<AppConfigDataContract> value = null;
            value = appl.GetAppConfigByKey(null, "UseDynamicsForInvoice");
            if (value != null && value.FirstOrDefault() != null && value.FirstOrDefault().Value == "1")
            {
                return true;
            }
            return false;
        }

        public string CheckCustomerinDynamics(DynamicsCustomerInput cust, string authToken)
        {
            string custId = "";
            WFLogic _wfLogic = new WFLogic();
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            try
            {
                IConfiguration config = configroot.GetSection("Dynamics365BusinessCentralDetail");
                string CompanyID = config.GetSection("CompanyID").Value;
                string BusinessCentralAPIURL = config.GetSection("BusinessCentralBaseURL").Value
                    + "companies(" + CompanyID + "/customers?$filter=displayName eq " + "'" + cust.displayName + "'";

                HttpClient _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //Set the Authorization header with the Access Token received specifying the Credentials

                //using (var content = new StringContent(JsonConvert.SerializeObject(jsonInput), System.Text.Encoding.UTF8, "application/json"))

                HttpResponseMessage result = _httpClient.GetAsync(BusinessCentralAPIURL).Result;

                if (result.StatusCode == System.Net.HttpStatusCode.OK || result.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    string resultString = result.Content.ReadAsStringAsync().Result;
                    var jsonOutputResult = JsonConvert.DeserializeObject<SearchCustomerOutput>(resultString);
                    QBDCustomerInputDataContract qbdcustomer = new QBDCustomerInputDataContract();
                    if (jsonOutputResult.value != null && jsonOutputResult.value.Count > 0)
                    {
                        qbdcustomer.ID = jsonOutputResult.value.FirstOrDefault().id;
                        custId = qbdcustomer.ID;
                        qbdcustomer.CustomerNo = jsonOutputResult.value.FirstOrDefault().number;
                        qbdcustomer.FullName = jsonOutputResult.value.FirstOrDefault().displayName;
                        _wfLogic.AddUpdateQBDCustomer(headerUserID.ToString(), qbdcustomer);

                    }


                }
            }
            catch (Exception ex)
            {
                custId = "";
            }
            return custId;
        }

        public string CheckCustomerinDynamicsByOdata(DynamicsODataCustomerInput cust, string authToken)
        {
            string CustomerNo = "";
            WFLogic _wfLogic = new WFLogic();
            var headerUserID = new Guid();
            string[] custname = { };
            try
            {
                if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                {
                    headerUserID = new Guid(Request.Headers["TokenUId"]);
                }
            }
            catch { }
            try
            {
                IConfiguration config = configroot.GetSection("Dynamics365BusinessCentralDetail");
                custname = cust.Name.Split("&");
                string CompanyID = config.GetSection("CompanyID").Value;
                string BusinessCentralAPIURL = config.GetSection("BusinessCentralBaseURL").Value
                    + "companies(" + CompanyID + ")/customers?$filter=displayName eq " + "'" + cust.Name + "'";

                if (custname.Length > 1)
                {
                    BusinessCentralAPIURL = config.GetSection("BusinessCentralBaseURL").Value
                       + "companies(" + CompanyID + ")/customers?$filter=startswith(displayName," + "'" + custname[0].Trim() + "') and endswith(displayName," + "'" + custname[custname.Length - 1].Trim() + "')";
                }

                HttpClient _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //Set the Authorization header with the Access Token received specifying the Credentials

                //using (var content = new StringContent(JsonConvert.SerializeObject(jsonInput), System.Text.Encoding.UTF8, "application/json"))

                HttpResponseMessage result = _httpClient.GetAsync(BusinessCentralAPIURL).Result;

                if (result.StatusCode == System.Net.HttpStatusCode.OK || result.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    string resultString = result.Content.ReadAsStringAsync().Result;
                    var jsonOutputResult = JsonConvert.DeserializeObject<SearchCustomerOutput>(resultString);
                    QBDCustomerInputDataContract qbdcustomer = new QBDCustomerInputDataContract();
                    if (jsonOutputResult.value != null && jsonOutputResult.value.Count > 0)
                    {
                        qbdcustomer.ID = jsonOutputResult.value.FirstOrDefault().id;
                        qbdcustomer.CustomerNo = jsonOutputResult.value.FirstOrDefault().number;
                        CustomerNo = qbdcustomer.CustomerNo;
                        qbdcustomer.FullName = jsonOutputResult.value.FirstOrDefault().displayName;
                        _wfLogic.AddUpdateQBDCustomer(headerUserID.ToString(), qbdcustomer);

                    }


                }
            }
            catch (Exception ex)
            {
                CustomerNo = "";
            }
            return CustomerNo;
        }
        public DynamicsSalesInvoiceOutput GetInvoiceDetailDynamicByGuid(string InvoiceGuid, string authToken)
        {
            DynamicsSalesInvoiceOutput SalesInvoiceOutput = new DynamicsSalesInvoiceOutput();
            GetConfigRoot();
            IConfiguration config = configroot.GetSection("Dynamics365BusinessCentralDetail");
            string CompanyID = config.GetSection("CompanyID").Value;
            string BusinessCentralAPIURL = config.GetSection("BusinessCentralBaseURL").Value
                + "companies(" + CompanyID + ")/salesInvoices(" + InvoiceGuid + ")";
            if (string.IsNullOrEmpty(authToken))
            {
                authToken = GetBusinessCentralAccessToken();
            }

            HttpClient _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //Set the Authorization header with the Access Token received specifying the Credentials

            //using (var content = new StringContent(JsonConvert.SerializeObject(jsonInput), System.Text.Encoding.UTF8, "application/json"))

            HttpResponseMessage result = _httpClient.GetAsync(BusinessCentralAPIURL).Result;

            if (result.StatusCode == System.Net.HttpStatusCode.OK || result.StatusCode == System.Net.HttpStatusCode.Created)
            {
                string resultString = result.Content.ReadAsStringAsync().Result;
                SalesInvoiceOutput = JsonConvert.DeserializeObject<DynamicsSalesInvoiceOutput>(resultString);
            }

            else
            {
                SalesInvoiceOutput.IsError = true;
                string resultString = result.Content.ReadAsStringAsync().Result;
                var jsonResultError = JsonConvert.DeserializeObject<error>(resultString);
                string errmessage = string.IsNullOrEmpty(jsonResultError.message) ? resultString : jsonResultError.message;
                errmessage = errmessage + "-Url-" + BusinessCentralAPIURL + "-status code-" + result.StatusCode;

                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while getting invoice detail in dynamics for InvoiceGuid " + InvoiceGuid, "", "", "GetInvoiceDetailDynamicByGuid", errmessage);

            }
            return SalesInvoiceOutput;
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/wfcontroller/updatepropertymanageremail")]
        public IActionResult UpdatePropertyManagerEmail([FromBody] WFDetailDataContract _wfDetailDataContract)
        {
            GenericResult _authenticationResult = null;

            var headerUserID = string.Empty;
            var delegateuserid = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            try
            {
                _wfDetailDataContract.CreatedBy = headerUserID;
                WFLogic _wfLogic = new WFLogic();
                string res = _wfLogic.UpdatePropertyManagerEmail(_wfDetailDataContract);
                string message = "Changes were saved successfully.";
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = message
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

        [HttpGet]
        [Route("api/wfcontroller/TestUploadFileToblob")]
        public IActionResult TestUploadFileToblob()
        {
            GenericResult _authenticationResult = null;
            try
            {
                //get config
                GetConfigSetting();
                var Container = Sectionroot.GetSection("storage:container:name").Value;
                CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);
                string filenm = "C:\\Users\\Admin\\Downloads\\DrawFee_Test.pdf";
                string fileName = "DrawFee_Test";

                CloudBlobDirectory blobDirectory = container.GetDirectoryReference("Invoice");
                CloudBlockBlob blockBlob = blobDirectory.GetBlockBlobReference(fileName);

                byte[] byteArray = System.IO.File.ReadAllBytes(filenm);

                blockBlob.Properties.ContentType = "application/pdf";
                blockBlob.UploadFromByteArray(byteArray, 0, byteArray.Length);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                };
            }
            catch (Exception ex)
            {

            }
            return Ok(_authenticationResult);
        }

        [HttpGet]
        [Route("api/wfcontroller/TestCheckBusinessCentralAccessToken")]
        public IActionResult TestCheckBusinessCentralAccessToken()
        {
            GenericResult _authenticationResult = null;
            try
            {
                GetConfigRoot();
                var Root = configroot.GetSection("Dynamics365BusinessCentralDetail");
                var Username = Root.GetSection("Username").Value;
                var Password = Root.GetSection("Password").Value;
                //prod act
                //var tenantId = "77be5eb1-c09a-4093-b65b-a73ae39864d9";
                //var clientId = "36703c6e-b6fc-4abe-9ba6-b8af9cc1bfc4";
                //power bi act
                var tenantId = Root.GetSection("AzurApptenantId").Value;
                var clientId = Root.GetSection("AzurAppClientId").Value;
                //var authorityUri = Root.GetSection("AuthorityUrl").Value + tenantId;
                var authorityUri = Root.GetSection("AuthorityUrl").Value;

                var redirectUri = Root.GetSection("RedirectUrl").Value;
                var scopes = new List<string> { Root.GetSection("Scope" +
                "URL").Value };
                var publicClient = Microsoft.Identity.Client.PublicClientApplicationBuilder
                              .Create(clientId)
                              .WithAuthority(new Uri(authorityUri))
                              .WithRedirectUri(redirectUri)
                              .Build();
                SecureString secureStringPwd = new SecureString();
                foreach (char c in Password.ToCharArray())
                {
                    secureStringPwd.AppendChar(c);
                }
                var accessTokenRequest = publicClient.AcquireTokenByUsernamePassword(scopes, Username, secureStringPwd);
                DynamicsAuthToken = accessTokenRequest.ExecuteAsync().Result.AccessToken;
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
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
        [HttpGet]
        [Route("api/wfcontroller/TestGetTokenAsync")]
        public async Task<string> TestGetTokenAsync()
        {

            //
            try
            {
                GetConfigRoot();
                var Root = configroot.GetSection("Dynamics365BusinessCentralDetail");
                //prod act
                //var tenantId = "77be5eb1-c09a-4093-b65b-a73ae39864d9";
                //var clientId = "36703c6e-b6fc-4abe-9ba6-b8af9cc1bfc4";
                //power bi act
                var tenantId = Root.GetSection("AzurApptenantId").Value;
                var clientId = Root.GetSection("AzurAppClientId").Value;
                var authorityUri = Root.GetSection("AuthorityUrl").Value + tenantId;
                var redirectUri = Root.GetSection("RedirectUrl").Value;
                var scopes = Root.GetSection("ScopeURLWithoutDefault").Value;
                var clientsecret = Root.GetSection("AzurAppClientSecretValue").Value;
                AuthenticationContext authContext = new AuthenticationContext(authorityUri);
                ClientCredential clientCredential = new ClientCredential(clientId, clientsecret);
                AuthenticationResult authResult = await authContext.AcquireTokenAsync(scopes, clientCredential);
                return authResult.AccessToken;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string> GetDynamicsTokenAsync()
        {

            //
            try
            {
                GetConfigRoot();
                var Root = configroot.GetSection("Dynamics365BusinessCentralDetail");
                //prod act
                //var tenantId = "77be5eb1-c09a-4093-b65b-a73ae39864d9";
                //var clientId = "36703c6e-b6fc-4abe-9ba6-b8af9cc1bfc4";
                //power bi act
                var tenantId = Root.GetSection("AzurApptenantId").Value;
                var clientId = Root.GetSection("AzurAppClientId").Value;
                var authorityUri = Root.GetSection("AuthorityUrl").Value + tenantId;
                var redirectUri = Root.GetSection("RedirectUrl").Value;
                var scopes = Root.GetSection("ScopeURLWithoutDefault").Value;
                var clientsecret = Root.GetSection("AzurAppClientSecretValue").Value;
                AuthenticationContext authContext = new AuthenticationContext(authorityUri);
                ClientCredential clientCredential = new ClientCredential(clientId, clientsecret);
                AuthenticationResult authResult = await authContext.AcquireTokenAsync(scopes, clientCredential);
                return authResult.AccessToken;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        List<AppConfigDataContract> DynamicsAppConfig = new List<AppConfigDataContract>();
        public void SetDynamicsAppConfig()
        {
            if (DynamicsAppConfig == null || DynamicsAppConfig.Count == 0)
            {
                AppConfigLogic appl = new CRES.BusinessLogic.AppConfigLogic();
                DynamicsAppConfig = appl.GetAppConfigByKey(null, "Dynamics_");
            }
        }

        [HttpGet]
        [Route("api/wfcontroller/createmissingqbdcustomerbyapi")]
        public void CreateMissingQBDCustomerAPI()
        {
            GetConfigSetting();
            List<DrawFeeInvoiceDataContract> lstdrawDC = new List<DrawFeeInvoiceDataContract>();
            WFLogic wfLogic = new WFLogic();
            lstdrawDC = wfLogic.GetMissingQBDCustomer();
            IActionResult ActionResult = null;

            if (!IsUseDynamicForInvoice())
            {
                foreach (DrawFeeInvoiceDataContract drawFeeDC in lstdrawDC)
                {
                    try
                    {
                        QBDCustomerInputDataContract qbdcustomer = new QBDCustomerInputDataContract();
                        qbdcustomer.FirstName = drawFeeDC.FirstName;
                        qbdcustomer.LastName = drawFeeDC.LastName;
                        qbdcustomer.CompanyName = drawFeeDC.CompanyName;
                        qbdcustomer.Name = CommonHelper.FormatCustomerForQuickBook(drawFeeDC.DealName, drawFeeDC.CreDealID);
                        qbdcustomer.FullName = qbdcustomer.Name;
                        qbdcustomer.BillingAddress = FormateQBDCustomerAddress(drawFeeDC.Address);
                        qbdcustomer.BillingAddress.City = drawFeeDC.City;
                        qbdcustomer.BillingAddress.State = drawFeeDC.State;
                        qbdcustomer.BillingAddress.PostalCode = drawFeeDC.Zip;

                        //call customer api

                        string inputjsonstring = JsonConvert.SerializeObject(qbdcustomer);


                        // var jsonInputResult = JsonConvert.DeserializeObject<AutofyInvoiceInputDataContract>(jsonInput);
                        HttpClient _httpClient = new HttpClient();
                        //using (var content = new StringContent(JsonConvert.SerializeObject(jsonInput), System.Text.Encoding.UTF8, "application/json"))
                        using (var content = new StringContent(JsonConvert.SerializeObject(inputjsonstring), System.Text.Encoding.UTF8, "application/json"))
                        {
                            string Autofykey = Sectionroot.GetSection("Autofykey").Value;
                            string SchemaVersion = Sectionroot.GetSection("SchemaVersion").Value;
                            string AutofyBaseURL = Sectionroot.GetSection("AutofyBaseURL").Value;
                            string AutofyCompanyEndPoint = Sectionroot.GetSection("AutofyCompanyEndPoint").Value;
                            content.Headers.Add("x-api-key", Autofykey);
                            content.Headers.Add("SchemaVersion", "20.04");
                            string AutoFURL = AutofyBaseURL + "data/" + AutofyCompanyEndPoint + "/customer";
                            HttpResponseMessage result = _httpClient.PostAsync(AutoFURL, content).Result;
                            if (result.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                string resultString = result.Content.ReadAsStringAsync().Result;
                                var jsonOutputResult = JsonConvert.DeserializeObject<QBDCustomerOutputDataContract>(resultString);
                                if (string.IsNullOrEmpty(jsonOutputResult.HasErrors) || jsonOutputResult.HasErrors == "false")
                                {
                                    qbdcustomer.ID = jsonOutputResult.Contents[0].Object.ID;
                                    wfLogic.AddUpdateQBDCustomer("", qbdcustomer);
                                }
                                //if customer already there in the quickbook but not in our db then add in our db
                                else if (jsonOutputResult.Contents[0].Errors != null && jsonOutputResult.Contents[0].Errors.Count > 0
                                    && jsonOutputResult.Contents[0].Errors[0].Message.Contains("the list element is already in use")
                                    )
                                {

                                    AutoFURL = AutofyBaseURL + "data/" + AutofyCompanyEndPoint + "/customer?name=" + WebUtility.UrlEncode(qbdcustomer.Name);
                                    _httpClient.DefaultRequestHeaders.Add("x-api-key", Autofykey);
                                    _httpClient.DefaultRequestHeaders.Add("SchemaVersion", "20.04");
                                    HttpResponseMessage resultcust = _httpClient.GetAsync(AutoFURL).Result;
                                    if (resultcust.StatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        string resultStringcust = resultcust.Content.ReadAsStringAsync().Result;
                                        var jsonOutputResultcust = JsonConvert.DeserializeObject<QBDCustomerOutputDataContract>(resultStringcust);

                                        if (string.IsNullOrEmpty(jsonOutputResultcust.HasErrors) || jsonOutputResultcust.HasErrors == "false")
                                        {
                                            qbdcustomer.ID = jsonOutputResultcust.Contents[0].Object.ID;
                                            wfLogic.AddUpdateQBDCustomer("", qbdcustomer);
                                        }
                                    }
                                }
                                else
                                {
                                    string errormsg = "";
                                    if (jsonOutputResult.Errors != null && jsonOutputResult.Errors.Count > 0)
                                        errormsg = jsonOutputResult.Errors[0].Message;
                                    else if (jsonOutputResult.Contents != null && jsonOutputResult.Contents.Count > 0 && jsonOutputResult.Contents[0].Errors != null
                                        && jsonOutputResult.Contents[0].Errors.Count > 0)
                                        errormsg = jsonOutputResult.Contents[0].Errors[0].Message;

                                    if (!string.IsNullOrEmpty(errormsg))
                                    {
                                        LoggerLogic Log = new LoggerLogic();
                                        Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating customer " + drawFeeDC.DealName + " in QBD from scheduler", "", "", "CreateMissingQBDCustomer", errormsg);
                                    }
                                }
                            }
                            else
                            {
                                LoggerLogic Log = new LoggerLogic();
                                Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating customer " + drawFeeDC.DealName + " in QBD from scheduler", "", "", "CreateMissingQBDCustomer", result.ReasonPhrase);

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating customer " + drawFeeDC.DealName + " in QBD from scheduler", "", "", ex.TargetSite.Name.ToString(), "", ex);

                    }
                }
            }
            else
            {
                foreach (DrawFeeInvoiceDataContract drawFeeDC in lstdrawDC)
                {
                    drawFeeDC.CreatedFrom = "Scheduler";
                    ActionResult = CreateQBDCustomer(drawFeeDC);
                }
            }
        }


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/wfcontroller/sendinternalnotificion")]
        public IActionResult SendInternalNotificion([FromBody] WFDetailDataContract _wfDetailDataContract)
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            List<WFNotificationDataContract> lstWFDetail = new List<WFNotificationDataContract>();
            string PreHeaderText = "";
            var headerUserID = string.Empty;
            var delegateuserid = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            if (!string.IsNullOrEmpty(Request.Headers["DelegatedUser"]))
            {
                delegateuserid = Convert.ToString(Request.Headers["DelegatedUser"]);
            }

            try
            {
                _wfDetailDataContract.CreatedBy = headerUserID;
                _wfDetailDataContract.DelegatedUserID = delegateuserid;

                WFLogic _wfLogic = new WFLogic();


                string message = "Changes were saved successfully.";

                if (headerUserID != null)
                {
                    //send email except Save,Save & Draft
                    if (_wfDetailDataContract.SubmitType != 551 && _wfDetailDataContract.SubmitType != 497)
                    {
                        //notifiy user by sending email
                        lstWFDetail = _wfLogic.GetWorkflowNotificationDetailByTaskId(_wfDetailDataContract.TaskID, Convert.ToInt32(_wfDetailDataContract.TaskTypeID), headerUserID);
                        if (lstWFDetail.Count > 0)
                        {
                            string DwarApprovalList = "";
                            foreach (WFCheckListDataContract checklist in _wfDetailDataContract.WFCheckList)
                            {
                                var CheckListStatusText = _wfDetailDataContract.WFCheckListStatus.Where(x => x.LookupID == checklist.CheckListStatus).FirstOrDefault().Name;
                                DwarApprovalList += "<tr><td style=" + "text-align:left;padding-left:5px!important; padding-right:5px!important;" + ">" + checklist.CheckListName + "</td><td style=" + "text-align:left;padding-left:5px!important; padding-right:5px!important;" + ">" + CheckListStatusText + "</td><td style=" + "text-align:left;padding-left:5px!important; padding-right:5px!important;" + ">" + checklist.Comment + "</td></tr>";
                            }

                            //get activity log
                            string commentHistory = "";
                            List<WFDetailDataContract> lstWFComments = new List<WFDetailDataContract>();
                            //userid= "00000000-0000-0000-0000-000000000000" means timezone will be Easten Standard Time
                            lstWFComments = _wfLogic.GetWFCommentsByTaskId(_wfDetailDataContract, "00000000-0000-0000-0000-000000000000");
                            lstWFComments = lstWFComments.Where(
                                x => (((x.WFStatusMasterID != 1 && x.WFStatusMasterID != 2) || x.SubmitType == 496) && x.Comment != "Checklist updated"
                                && x.Comment.Contains("Changed the funding date") == false && x.Comment.Contains("Changed the funding amount") == false)
                                ).ToList();
                            //CreatedDate
                            foreach (WFDetailDataContract wfd in lstWFComments)
                            {
                                if (!String.IsNullOrEmpty(wfd.DelegatedUserName))
                                {
                                    wfd.Login = wfd.DelegatedUserName + "(on behalf of " + wfd.Login + " )";
                                }
                                //Wednesday, November 27th, 2019 12:9:44 PM
                                commentHistory += "<i>" + wfd.Login + "  " + ((wfd.SubmitType == 496 && string.IsNullOrEmpty(wfd.Comment)) ? "rejected transaction back to " : "") + wfd.StatusName.FormatWFStatusName() + "  " + Convert.ToDateTime(wfd.CreatedDate).ToString("dddd, MMMM dd, yyyy hh:mm:tt") + " " + wfd.Abbreviation + "</i>" + "<br/>";
                                if (!string.IsNullOrEmpty(wfd.Comment))
                                {
                                    commentHistory += wfd.Comment + "<br/>";
                                }
                            }

                            if (!string.IsNullOrEmpty(commentHistory))
                            {
                                commentHistory = "<b>Activity log are below:</b><br/>" + commentHistory;
                            }
                            //Add “Funding Team’s Approval Required” to beginning of the subject line if value of checklist 'Funding Team’s Approval Required'  is 'YES'
                            var FundingApprovalRequired = _wfDetailDataContract.WFCheckList.Where(x => x.CheckListMasterId == 6 && x.CheckListStatus == 499);
                            if (FundingApprovalRequired != null && FundingApprovalRequired.Count() > 0 && _wfDetailDataContract.TaskTypeID == 502)
                            {
                                PreHeaderText = FundingApprovalRequired.FirstOrDefault().CheckListName;
                            }

                            var ReserveFundingApprovalRequired = _wfDetailDataContract.WFCheckList.Where(x => x.CheckListMasterId == 15 && x.CheckListStatus == 499);
                            if (ReserveFundingApprovalRequired != null && ReserveFundingApprovalRequired.Count() > 0 && _wfDetailDataContract.TaskTypeID == 719)
                            {
                                PreHeaderText = ReserveFundingApprovalRequired.FirstOrDefault().CheckListName;
                            }

                            lstWFDetail.ForEach(x =>
                            {
                                x.DealName = _wfDetailDataContract.DealName;
                                x.Comment = _wfDetailDataContract.DrawComment;
                                x.ActivityLog = commentHistory;
                                x.FooterText = _wfDetailDataContract.FooterText;
                                x.SenderName = _wfDetailDataContract.SenderName;
                                x.DwarApprovalList = DwarApprovalList;
                                x.SpecialInstructions = _wfDetailDataContract.SpecialInstructions;
                                x.AdditionalComments = _wfDetailDataContract.AdditionalComments;
                                x.NoteswithAmount = x.FundingAmount > 0 ? _wfDetailDataContract.NoteswithAmount : "";
                                x.ReserveScheduleBreakDown = x.FundingAmount > 0 ? _wfDetailDataContract.ReserveScheduleBreakDown : "";
                                x.PreHeaderText = PreHeaderText;
                                x.TaskTypeID = _wfDetailDataContract.TaskTypeID;
                            });

                            if (_wfDetailDataContract.TaskTypeID == 502)
                            {
                                _iEmailNotification.SendWorkFlowNotification(lstWFDetail);
                            }
                            else if (_wfDetailDataContract.TaskTypeID == 719)
                            {
                                _iEmailNotification.SendReserveWorkFlowInternalNotification(lstWFDetail);
                            }
                        }
                    }

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = message
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


        [HttpGet]
        [Route("api/wfcontroller/createinvoicedinvoicesindyanamics")]
        public IActionResult CreateInvoicedInvoicesInDyanamics()
        {

            GenericResult _authenticationResult = null;
            WFLogic _wfLogic = new WFLogic();
            List<DrawFeeInvoiceDataContract> lstDraw = new List<DrawFeeInvoiceDataContract>();
            DrawFeeInvoiceDataContract DrawFeeDC = null;

            try
            {

                GetConfigSetting();
                CreateMissingQBDCustomer();
                lstDraw = _wfLogic.GetAllInvoicedInvoice("");

                //call invoice api and update status of invoice
                foreach (DrawFeeInvoiceDataContract df in lstDraw)
                {
                    try
                    {
                        DrawFeeDC = df;
                        DrawFeeDC = CreateInvoiceInQBD(df);
                        if (DrawFeeDC.IsSuccess)
                        {
                            _wfLogic = new WFLogic();
                            _wfLogic.UpdateDrawFeeInvoiceDetailStatus("", DrawFeeDC);
                        }
                    }
                    catch (Exception ex)
                    {

                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in Dynamics from CreateInvoicedInvoicesInDyanamics method for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), "", ex.TargetSite.Name.ToString(), "", ex);
                    }

                }
            }
            catch (Exception ex)
            {

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in Dynamics from CreateInvoicedInvoicesInDyanamics method", "", "", ex.TargetSite.Name.ToString(), "", ex);

            }
            return Ok(_authenticationResult);
        }

        [HttpGet]
        [Route("api/wfcontroller/createinvoicedinvoicesindyanamicsandbox")]
        public IActionResult CreateInvoicedInvoicesInDyanamicSandbox()
        {

            GenericResult _authenticationResult = null;
            WFLogic _wfLogic = new WFLogic();
            List<DrawFeeInvoiceDataContract> lstDraw = new List<DrawFeeInvoiceDataContract>();
            DrawFeeInvoiceDataContract DrawFeeDC = null;

            try
            {

                GetConfigSetting();
                CreateMissingCustomerInSandbox();
                lstDraw = _wfLogic.GetAllInvoicedInvoice("");

                //call invoice api and update status of invoice
                foreach (DrawFeeInvoiceDataContract df in lstDraw)
                {
                    try
                    {
                        DrawFeeDC = df;
                        DrawFeeDC = CreateInvoiceInDynamicSandbox(df);
                        if (DrawFeeDC.IsSuccess)
                        {
                            _wfLogic = new WFLogic();
                            _wfLogic.UpdateDrawFeeInvoiceDetailStatusForSandbox("", DrawFeeDC);
                        }
                    }
                    catch (Exception ex)
                    {

                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in Dynamics from CreateInvoicedInvoicesInDyanamics method for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), "", ex.TargetSite.Name.ToString(), "", ex);
                    }

                }
            }
            catch (Exception ex)
            {

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Error occurred while creating invoice in Dynamics from CreateInvoicedInvoicesInDyanamics method", "", "", ex.TargetSite.Name.ToString(), "", ex);

            }
            return Ok(_authenticationResult);
        }


        public WFNotificationDataContract GetInternalNotificionDetail(WFDetailDataContract _wfDetailDataContract)
        {
            WFNotificationDataContract WFDetail = new WFNotificationDataContract();
            string PreHeaderText = "";
            var headerUserID = string.Empty;
            string DwarApprovalList = "";
            try
            {
                _wfDetailDataContract.WFCheckList = new List<WFCheckListDataContract>();
                _wfDetailDataContract.CreatedBy = headerUserID;
                WFLogic _wfLogic = new WFLogic();

                _wfDetailDataContract.WFCheckList = _wfLogic.GetCheckListByTaskId(_wfDetailDataContract, headerUserID.ToString());
                foreach (WFCheckListDataContract checklist in _wfDetailDataContract.WFCheckList)
                {
                    DwarApprovalList += "<tr><td style=" + "text-align:left;padding-left:5px!important; padding-right:5px!important;" + ">" + checklist.CheckListName + "</td><td style=" + "text-align:left;padding-left:5px!important; padding-right:5px!important;" + ">" + checklist.CheckListStatusText + "</td><td style=" + "text-align:left;padding-left:5px!important; padding-right:5px!important;" + ">" + checklist.Comment + "</td></tr>";
                }

                //get activity log
                string commentHistory = "";
                List<WFDetailDataContract> lstWFComments = new List<WFDetailDataContract>();
                //userid= "00000000-0000-0000-0000-000000000000" means timezone will be Easten Standard Time
                lstWFComments = _wfLogic.GetWFCommentsByTaskId(_wfDetailDataContract, "00000000-0000-0000-0000-000000000000");
                lstWFComments = lstWFComments.Where(
                    x => (((x.WFStatusMasterID != 1 && x.WFStatusMasterID != 2) || x.SubmitType == 496) && x.Comment != "Checklist updated"
                    && x.Comment.Contains("Changed the funding date") == false && x.Comment.Contains("Changed the funding amount") == false)
                    ).ToList();
                //CreatedDate
                foreach (WFDetailDataContract wfd in lstWFComments)
                {
                    if (!String.IsNullOrEmpty(wfd.DelegatedUserName))
                    {
                        wfd.Login = wfd.DelegatedUserName + "(on behalf of " + wfd.Login + " )";
                    }
                    //Wednesday, November 27th, 2019 12:9:44 PM
                    commentHistory += "<i>" + wfd.Login + "  " + ((wfd.SubmitType == 496 && string.IsNullOrEmpty(wfd.Comment)) ? "rejected transaction back to " : "") + wfd.StatusName.FormatWFStatusName() + "  " + Convert.ToDateTime(wfd.CreatedDate).ToString("dddd, MMMM dd, yyyy hh:mm:tt") + " " + wfd.Abbreviation + "</i>" + "<br/>";
                    if (!string.IsNullOrEmpty(wfd.Comment))
                    {
                        commentHistory += wfd.Comment + "<br/>";
                    }
                }

                if (!string.IsNullOrEmpty(commentHistory))
                {
                    commentHistory = "<b>Activity log are below:</b><br/>" + commentHistory;
                }
                //Add “Funding Team’s Approval Required” to beginning of the subject line if value of checklist 'Funding Team’s Approval Required'  is 'YES'
                var FundingApprovalRequired = _wfDetailDataContract.WFCheckList.Where(x => x.CheckListMasterId == 6 && x.CheckListStatus == 499);
                if (FundingApprovalRequired != null && FundingApprovalRequired.Count() > 0 && _wfDetailDataContract.TaskTypeID == 502)
                {
                    PreHeaderText = FundingApprovalRequired.FirstOrDefault().CheckListName;
                }

                var ReserveFundingApprovalRequired = _wfDetailDataContract.WFCheckList.Where(x => x.CheckListMasterId == 15 && x.CheckListStatus == 499);
                if (ReserveFundingApprovalRequired != null && ReserveFundingApprovalRequired.Count() > 0 && _wfDetailDataContract.TaskTypeID == 719)
                {
                    PreHeaderText = ReserveFundingApprovalRequired.FirstOrDefault().CheckListName;
                }
                _wfDetailDataContract.NoteswithAmount = GetNoteBreakDownDetail(_wfDetailDataContract.TaskID);
                _wfDetailDataContract.WFAdditionalList = _wfLogic.GetWorkflowAdditionalDetailByTaskId(_wfDetailDataContract, headerUserID.ToString());

                WFDetail.DealName = _wfDetailDataContract.WFAdditionalList.DealName;
                WFDetail.FundingAmount = Convert.ToDouble(_wfDetailDataContract.WFAdditionalList.Amount);
                WFDetail.DealLine = Convert.ToDateTime(_wfDetailDataContract.WFAdditionalList.DeadLineDate);
                WFDetail.FundingDate = Convert.ToDateTime(_wfDetailDataContract.WFAdditionalList.Date);
                WFDetail.Comment = _wfDetailDataContract.DrawComment;
                WFDetail.ActivityLog = commentHistory;
                WFDetail.FooterText = _wfDetailDataContract.FooterText;
                WFDetail.SenderName = _wfDetailDataContract.SenderName;
                WFDetail.DwarApprovalList = DwarApprovalList;
                WFDetail.SpecialInstructions = _wfDetailDataContract.WFAdditionalList.SpecialInstructions;
                WFDetail.AdditionalComments = _wfDetailDataContract.WFAdditionalList.AdditionalComments;
                WFDetail.NoteswithAmount = WFDetail.FundingAmount > 0 ? _wfDetailDataContract.NoteswithAmount : "";
                WFDetail.ReserveScheduleBreakDown = WFDetail.FundingAmount > 0 ? _wfDetailDataContract.ReserveScheduleBreakDown : "";
                WFDetail.PreHeaderText = PreHeaderText;
                WFDetail.TaskTypeID = _wfDetailDataContract.TaskTypeID;
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.WFNotification.ToString(), "Error occurred while fetching internal notificion detail", _wfDetailDataContract.TaskID, "", ex.TargetSite.Name.ToString(), "", ex);
            }
            return WFDetail;
        }


        public string GetNoteBreakDownDetail(string dealFundingId)
        {
            string strnotes = "";
            decimal total = 0;
            string trHeader = "";
            //"<p style='padding: 0px; margin: 28px 0px 0px 28px;font-size:12px;'><strong> Details by note are below:</b><br/></strong>"+
            //"<table id='noteinfo' border='1' style='border-collapse:collapse;font-size:12px;margin:7px 0 0 28px'><tr style='font-weight:bold'><td style='padding-left:5px!important;padding-right:5px!important;'>Loan#</td><td style='padding-left:5px!important;padding-right:5px!important;'>Note ID</td><td style='padding-left:5px!important;padding-right:5px!important;'>Financing Source</td><td style='padding-left:5px!important;padding-right:5px!important;'>Note Name</td><td style='padding-left:5px!important;padding-right:5px!important;'>Current Request</td> </tr>";
            DealLogic _dealLogic = new DealLogic();
            DataTable dt;
            dt = _dealLogic.GetWFNoteFunding(new Guid(dealFundingId), "00000000-0000-0000-0000-000000000000");
            if (dt != null && dt.Rows.Count > 0)
            {

                foreach (DataRow dr in dt.Rows)
                {
                    if (Convert.ToDecimal(dr["Value"]) != 0 && Convert.ToString(dr["TaxVendorLoanNumber"]) != "" && Convert.ToInt32(dr["FinancingSourceID"]) != 26)
                    {

                        strnotes = strnotes +
                        "<tr><td style='text-align:right;padding-left:5px!important;padding-right:5px!important;'>" + Convert.ToString(dr["TaxVendorLoanNumber"]) + "</td><td style='padding-left:5px!important;padding-right:5px!important;'>" + dr["CRENoteID"] + "</td><td style='text-align:left;padding-left:5px!important;padding-right:5px!important;'>" + dr["FinancingSourceName"] + "</td><td style='text-align:left;text-align:left;padding-left:5px!important;padding-right:5px!important;'>" + dr["Name"] + "</td><td style='text-align:right;padding-left:5px!important;padding-right:5px!important;'>" + String.Format("{0:c}", Convert.ToDecimal(dr["Value"])) + "</td> </tr>";

                        total += Convert.ToDecimal(dr["Value"]);
                    }


                }

                if (strnotes != "")
                {
                    strnotes = strnotes + "<tr><td></td><td></td><td></td><td style='text-align:left;font-weight:bold;text-align:right;padding-left:5px!important;padding-right:5px!important;'>Total Loan Funds</td><td style='text-align:right;font-weight:bold;text-align:right;padding-left:5px!important;padding-right:5px!important;'>" + String.Format("{0:c}", total) + "</td></tr>";

                    trHeader = trHeader + strnotes + "</table>";
                }
                else
                {
                    trHeader = "";
                }
            }
            else
            {
                trHeader = "";
            }
            return trHeader;
        }



        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/wfcontroller/cancelnotification")]
        //CancelNotification
        public IActionResult CancelNotification([FromBody] WFNotificationDetailDataContract _wfDetailDataContract)
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            var delegatedUserID = string.Empty;
            string htmlContent = "";
            WFNotificationDetailDataContract _wDC = new WFNotificationDetailDataContract();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            if (!string.IsNullOrEmpty(Request.Headers["DelegatedUser"]))
            {
                delegatedUserID = Convert.ToString(Request.Headers["DelegatedUser"]);
            }

            try
            {
                _wfDetailDataContract.CreatedBy = headerUserID;
                _wfDetailDataContract.DelegatedUserID = delegatedUserID;
                WFLogic _wfLogic = new WFLogic();

                try
                {
                    ////send email except Save,Save & Draft
                    //  EmailNotification notification = new EmailNotification();
                    _wfDetailDataContract.MessageHTML = Regex.Replace(_wfDetailDataContract.MessageHTML, @"\r\n?|\n", "<br />");

                    _wDC = _wfLogic.GetEmailsForCancelFinalNotifiction(_wfDetailDataContract.TaskID.ToString(), Convert.ToInt32(_wfDetailDataContract.TaskTypeID), headerUserID);

                    _wfDetailDataContract.EmailToIds = _wDC.EmailToIds;
                    _wfDetailDataContract.EmailCCIds = _wDC.EmailCCIds;
                    if (!string.IsNullOrEmpty(_wfDetailDataContract.EmailToIds))
                    {
                        _wfDetailDataContract.EmailToIds = _wfDetailDataContract.EmailToIds.Replace(";", ",");
                    }
                    if (!string.IsNullOrEmpty(_wfDetailDataContract.EmailCCIds))
                    {
                        _wfDetailDataContract.EmailCCIds = _wfDetailDataContract.EmailCCIds.Replace(";", ",");
                    }
                    //LogDB("sending email");
                    _iEmailNotification.SendWFNotification(_wfDetailDataContract, out htmlContent);
                }
                catch (Exception ex)
                {

                    //LogDB(ex.Message);
                }

                _wfDetailDataContract.MessageHTML = htmlContent;
                if (!string.IsNullOrEmpty(_wfDetailDataContract.EnvironmentName))
                {
                    _wfDetailDataContract.Subject = _wfDetailDataContract.Subject.Replace(_wfDetailDataContract.EnvironmentName, "");
                }

                string res = _wfLogic.InsertUpdateWFNotification(_wfDetailDataContract, headerUserID);

                string message = "Changes were saved successfully.";

                if (headerUserID != null)
                {
                    if (res == "success")
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = message
                        };
                    }
                    else
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "Some Error Occured."
                        };
                    }
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
        [Route("api/wfcontroller/savewfdashboard")]
        //SaveWFDashboard
        public IActionResult SaveWFDashboard([FromBody] List<WFDashboardDataContract> _lstWorkflow)
        {
            GenericResult _authenticationResult = null;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            WFLogic _wfLogic = new WFLogic();
            try
            {
                //_lstWorkflow.Where(x => x.UserID == null).ToList().ForEach(x =>
                //{
                //    x.UserID = new Guid("00000000-0000-0000-0000-000000000000");
                //});

                string res = _wfLogic.SaveWFDashboard(_lstWorkflow, headerUserID);

                string message = "Changes were saved successfully.";

                if (headerUserID != null)
                {
                    if (res == "success")
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = message
                        };
                    }
                    else
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "Some Error Occured."
                        };
                    }
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

                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in SaveWFDashboard", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_authenticationResult);
        }

        //sandbox code-start
        [HttpGet]
        [Route("api/wfcontroller/createinvoiceinsandbox")]
        public IActionResult InvoiceQueuedToInvoicedForSandBox()
        {

            GenericResult _authenticationResult = null;
            WFLogic _wfLogic = new WFLogic();
            List<DrawFeeInvoiceDataContract> lstDraw = new List<DrawFeeInvoiceDataContract>();
            DrawFeeInvoiceDataContract DrawFeeDC = null;

            try
            {

                GetConfigSetting();
                CreateMissingCustomerInSandbox();
                lstDraw = _wfLogic.GetAllInvoiceQueuedForSandbox("");

                //call invoice api and update status of invoice
                foreach (DrawFeeInvoiceDataContract df in lstDraw)
                {
                    try
                    {
                        DrawFeeDC = df;
                        DrawFeeDC = CreateInvoiceInDynamicSandbox(df);
                        if (DrawFeeDC.IsSuccess)
                        {
                            _wfLogic = new WFLogic();
                            _wfLogic.UpdateDrawFeeInvoiceDetailStatusForSandbox("", DrawFeeDC);
                        }


                    }
                    catch (Exception ex)
                    {

                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Sandbox-Error occurred while creating invoice in Sandbox from scheduler for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), "", ex.TargetSite.Name.ToString(), "", ex);

                        EmailDataContract emaildc = new EmailDataContract();
                        emaildc.ModuleId = 705;
                        emaildc.Subject = "Sandbox Invoice Error Notification";
                        emaildc.Body = "Sandbox-Error occurred while creating invoice in Sandbox from scheduler for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString() + " - " + ex.Message;
                        _iEmailNotification.SendErrorNotificationEmail(emaildc);
                    }

                }
            }
            catch (Exception ex)
            {

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Sandbox-Error occurred while creating invoice in Sandbox from scheduler", "", "", ex.TargetSite.Name.ToString(), "", ex);

            }
            return Ok(_authenticationResult);
        }

        public DrawFeeInvoiceDataContract CreateInvoiceInDynamicSandbox([FromBody] DrawFeeInvoiceDataContract DrawFeeDC)
        {

            // generate invoice in below cases
            //if autosend invoice from workflow
            //if manual from dealdetail
            if ((DrawFeeDC.AutoSendInvoice == 571 || DrawFeeDC.IsManualInvoice == true || DrawFeeDC.InvoiceSource == "System")
            && string.IsNullOrEmpty(DrawFeeDC.InvoiceNo) == false)
            {
                GetConfigSetting();
                SetDynamicsAppConfig();
                var headerUserID = string.Empty;
                //if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                //{
                //    headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
                //}

                try
                {
                    WFLogic _wfLogicInvoiceConfig = new WFLogic();
                    InvoiceConfigDataContract invoiceDC = new InvoiceConfigDataContract();
                    invoiceDC = _wfLogicInvoiceConfig.GetInvoiceConfigByInvoiceType(DrawFeeDC.InvoiceTypeID, "");
                    if (invoiceDC != null && !string.IsNullOrEmpty(invoiceDC.InvoiceCode))
                    {
                        DrawFeeDC.InvoiceCode = invoiceDC.InvoiceCode;
                        DrawFeeDC.TemplateName = invoiceDC.Template;
                    }

                    WFLogic _wfLogic = new WFLogic();
                    if (DrawFeeDC.DrawFeeInvoiceDetailID == 0 || string.IsNullOrEmpty(DrawFeeDC.FirstName))
                    {
                        DrawFeeInvoiceDataContract drawDC = new DrawFeeInvoiceDataContract();
                        drawDC = _wfLogic.GetDrawFeeInvoiceDetailByTaskID(DrawFeeDC.TaskID.ToString(), headerUserID.ToString());
                        DrawFeeDC.DrawFeeInvoiceDetailID = drawDC.DrawFeeInvoiceDetailID;
                        DrawFeeDC.FirstName = drawDC.FirstName;
                        DrawFeeDC.LastName = drawDC.LastName;
                        DrawFeeDC.Amount = drawDC.Amount;
                        DrawFeeDC.City = drawDC.City;
                        DrawFeeDC.State = drawDC.State;
                        DrawFeeDC.Zip = drawDC.Zip;
                        DrawFeeDC.Address = drawDC.Address;
                        DrawFeeDC.Email1 = drawDC.Email1;
                        DrawFeeDC.Email2 = drawDC.Email2;
                        DrawFeeDC.PhoneNo = drawDC.PhoneNo;
                        DrawFeeDC.AlternatePhone = drawDC.AlternatePhone;
                        DrawFeeDC.CompanyName = drawDC.CompanyName;
                        DrawFeeDC.InvoiceNoUI = drawDC.InvoiceNoUI;
                        DrawFeeDC.InvoiceNo = drawDC.InvoiceNo;
                        DrawFeeDC.AMEmails = drawDC.AMEmails;
                        DrawFeeDC.SenderFirstName = drawDC.SenderFirstName;
                        DrawFeeDC.SenderLastName = drawDC.SenderLastName;
                        DrawFeeDC.SenderEmail = drawDC.SenderEmail;
                    }

                    if (string.IsNullOrEmpty(DrawFeeDC.InvoiceNo) == false)
                    {
                        //create invoice in dynamic 365
                        GetConfigRoot();
                        IConfiguration config = configroot.GetSection("Dynamics365BusinessCentralDetail");
                        string CompanyID = config.GetSection("CompanyID").Value;
                        string BusinessCentralAPIURL = config.GetSection("BusinessCentralBaseURL").Value
                            + "companies(" + CompanyID + ")/salesInvoices";
                        string authToken = GetBusinessCentralAccessToken();

                        if (string.IsNullOrEmpty(authToken))
                        {
                            LoggerLogic Log = new LoggerLogic();
                            Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Sanbbox - Error occurred while creating invoice in Dynamics for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), headerUserID.ToString(), "CreateInvoiceInDynamicSandbox", "Unauthentication token");
                            DrawFeeDC.IsSuccess = false;
                            return DrawFeeDC;
                        }

                        string accountName = CommonHelper.FormatCustomerForQuickBook(DrawFeeDC.DealName, DrawFeeDC.CreDealID);
                        DynamicsCustomer dcust = _wfLogic.GetCustomerByAccountName(accountName);
                        DynamicsSalesInvoiceInput dinput = new DynamicsSalesInvoiceInput();
                        dinput.invoiceDate = DrawFeeDC.FundingDate.ToString("yyyy-MM-dd");
                        dinput.dueDate = DrawFeeDC.FundingDate.ToString("yyyy-MM-dd");
                        dinput.postingDate = DrawFeeDC.FundingDate.ToString("yyyy-MM-dd");
                        dinput.customerNumber = dcust.CustomerNo;
                        //dinput.paymentTermsId = "5335f8d6-c141-ec11-bb7b-000d3a256200";
                        //dinput.paymentTermsId = "7e5ed7ef-a1b1-ec11-8aa5-0022482b5b4b";
                        //dinput.currencyCode = "USD";
                        //dinput.currencyId = "00000000-0000-0000-0000-000000000000";

                        //dinput.paymentTermsId = DynamicsAppConfig.Where(i => i.Key == "Dynamics_paymentTermsId").FirstOrDefault().Value;
                        //dinput.currencyCode = DynamicsAppConfig.Where(i => i.Key == "Dynamics_currencyCode").FirstOrDefault().Value;
                        //dinput.currencyId = DynamicsAppConfig.Where(i => i.Key == "Dynamics_currencyId").FirstOrDefault().Value;

                        dinput.externalDocumentNumber = DrawFeeDC.InvoiceNoUI;
                        //dinput.customerPurchaseOrderReference = DrawFeeDC.InvoiceNo;
                        dinput.customerPurchaseOrderReference = string.IsNullOrEmpty(DrawFeeDC.Comment) ? DrawFeeDC.DrawNo : DrawFeeDC.Comment;

                        if (dinput.customerPurchaseOrderReference.Length > 35)
                        {
                            dinput.customerPurchaseOrderReference = dinput.customerPurchaseOrderReference.Substring(0, 35);
                        }
                        dinput.salesInvoiceLines = new List<DynamicsSalesInvoiceLineInput>();
                        //call invoice split
                        //get invoice split by financial source and adjustedcommitment
                        List<InvoiceSplitOutputDataContract> lstSplit = new List<InvoiceSplitOutputDataContract>();
                        if (invoiceDC != null && invoiceDC.IsApplySplit == true)
                        {
                            InvoiceSplitParamDataContract param = new InvoiceSplitParamDataContract();
                            param.DealID = DrawFeeDC.CreDealID;
                            param.InvoiceTypeID = DrawFeeDC.InvoiceTypeID;
                            param.FeeAmount = DrawFeeDC.Amount;
                            lstSplit = _wfLogic.GetInvoiceSplit(param, headerUserID);
                            if (lstSplit.Count > 0)
                            {
                                foreach (InvoiceSplitOutputDataContract itm in lstSplit)
                                {
                                    dinput.salesInvoiceLines.Add(
                                    new DynamicsSalesInvoiceLineInput
                                    {
                                        lineType = "Account",
                                        description = itm.QBItemName,
                                        lineObjectNumber = itm.QBAccountNo,
                                        quantity = 1,
                                        unitPrice = itm.SplitAmount
                                    }
                                    );
                                }
                            }
                        }
                        else
                        {
                            dinput.salesInvoiceLines.Add(
                                    new DynamicsSalesInvoiceLineInput
                                    {
                                        lineType = "Account",
                                        description = invoiceDC.InvoiceCode,
                                        lineObjectNumber = invoiceDC.InvoiceAccountNo,
                                        quantity = 1,
                                        unitPrice = DrawFeeDC.Amount
                                    }
                                    );
                        }

                        string inputjsonstring = JsonConvert.SerializeObject(dinput);
                        HttpResponseMessage result = null;
                        HttpClient _httpClient = new HttpClient();
                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        //Set the Authorization header with the Access Token received specifying the Credentials
                        using (var content = new StringContent(inputjsonstring, System.Text.Encoding.UTF8, "application/json"))
                        {
                            result = _httpClient.PostAsync(BusinessCentralAPIURL, content).Result;
                            if (result.StatusCode == System.Net.HttpStatusCode.Created || result.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                string resultString = result.Content.ReadAsStringAsync().Result;
                                var jsonOutputResult = JsonConvert.DeserializeObject<DynamicsSalesInvoiceOutput>(resultString);
                                DrawFeeDC.IsSuccess = true;
                                DrawFeeDC.InvoiceNo = jsonOutputResult.number;
                                DrawFeeDC.PreAssignedInvoiceNo = jsonOutputResult.number;
                                DrawFeeDC.InvoiceGuid = jsonOutputResult.id;
                            }

                            else
                            {
                                string resultString = result.Content.ReadAsStringAsync().Result;
                                var jsonResultError = JsonConvert.DeserializeObject<error>(resultString);
                                string errmessage = string.IsNullOrEmpty(jsonResultError.message) ? resultString : jsonResultError.message;
                                errmessage = errmessage + "-Url-" + BusinessCentralAPIURL + "-status code-" + result.StatusCode;
                                LoggerLogic Log = new LoggerLogic();
                                Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Sanbbox - Error occurred while creating invoice in dynamics for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), "", headerUserID.ToString(), "CreateInvoiceInDynamicSandbox", errmessage);
                                //EmailDataContract emaildc = new EmailDataContract();
                                //emaildc.ModuleId = 705;
                                //emaildc.Subject = "Sanbbox - Dynamics 365 Invoice Error Notification";
                                //emaildc.Body = "Sanbbox - Error occurred while creating invoice in Dynamics for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString() + " - " + errmessage;
                                //_iEmailNotification.SendErrorNotificationEmail(emaildc);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerLogic Log = new LoggerLogic();
                    Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Sanbbox - Error occurred while creating invoice in Dynamics for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), DrawFeeDC.DrawFeeInvoiceDetailID.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);
                    DrawFeeDC.IsSuccess = false;
                    EmailDataContract emaildc = new EmailDataContract();
                    emaildc.ModuleId = 705;
                    emaildc.Subject = "Sanbbox - Dynamics 365 Invoice Error Notification";
                    emaildc.Body = "Sanbbox - Error occurred while creating invoice in QBD for InvoiceDetailID " + DrawFeeDC.DrawFeeInvoiceDetailID.ToString() + " - " + ex.Message;
                    _iEmailNotification.SendErrorNotificationEmail(emaildc);
                }
            }

            return DrawFeeDC;
        }

        public void CreateMissingCustomerInSandbox()
        {
            GetConfigSetting();
            List<DrawFeeInvoiceDataContract> lstdrawDC = new List<DrawFeeInvoiceDataContract>();
            WFLogic wfLogic = new WFLogic();
            lstdrawDC = wfLogic.GetMissingQBDCustomerInSandbox();

            List<DynamicsODataCustomerOutput> lstCustSandbox = GetAllSandboxCustomer();
            var result = lstdrawDC.Where(p => !lstCustSandbox.Any(x => x.Name == p.DealName)).ToList();

            IActionResult ActionResult = null;
            foreach (DrawFeeInvoiceDataContract drawFeeDC in result)
            {
                drawFeeDC.CreatedFrom = "Scheduler";
                ActionResult = CreateCustomerInSandbox(drawFeeDC);
            }
        }

        public IActionResult CreateCustomerInSandbox([FromBody] DrawFeeInvoiceDataContract drawFeeDC)
        {
            GenericResult _authenticationResult = null;
            DrawFeeInvoiceDataContract drawDC = new DrawFeeInvoiceDataContract();
            WFLogic wfLogic = new WFLogic();
            var headerUserID = new Guid();
            try
            {
                if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                {
                    headerUserID = new Guid(Request.Headers["TokenUId"]);
                }
            }
            catch { }
            string CreatedFrom = "";
            try
            {
                SetDynamicsAppConfig();
                if (!string.IsNullOrEmpty(drawFeeDC.CreatedFrom))
                {
                    CreatedFrom = " From " + drawFeeDC.CreatedFrom;
                }
                GetConfigRoot();
                IConfiguration config = configroot.GetSection("Dynamics365BusinessCentralDetail");
                string BusinessCentralAPIURL = config.GetSection("BusinessCentralOdataBaseURL").Value;
                string BusinessCentralCompanyName = config.GetSection("CompanyName").Value;
                BusinessCentralAPIURL = BusinessCentralAPIURL + "Company('" + BusinessCentralCompanyName + "')/"
                + "CustomerListAPI";
                string authToken = GetBusinessCentralAccessToken();
                if (string.IsNullOrEmpty(authToken))
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed"
                    };
                    LoggerLogic Log = new LoggerLogic();
                    Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Sandbox - Error occurred while creating customer " + drawFeeDC.DealName + " in Dynamics" + CreatedFrom, "", headerUserID.ToString(), "CreateCustomerInSandbox", "Unauthentication token");
                    return Ok(_authenticationResult);
                }

                //  end - customer by dynamic api
                //start-customer by Odata api
                DynamicsODataCustomerInput cust = new DynamicsODataCustomerInput();
                cust.Name = CommonHelper.FormatCustomerForQuickBook(drawFeeDC.DealName, drawFeeDC.CreDealID); ;
                cust.No = drawFeeDC.CreDealID;

                //cust.type = "Company";
                cust.Contact = drawFeeDC.FirstName + " " + drawFeeDC.LastName;
                string add = drawFeeDC.Address;
                cust.Address = add.Length > 100 ? add.Substring(0, 100) : add;
                cust.Address_2 = add.Length > 100 ? add.Substring(100, add.Remove(0, 100).Length) : "";
                //cust.addressLine2 = "Company";
                cust.City = drawFeeDC.City;
                cust.State = drawFeeDC.State;
                //cust.country = "Company";
                cust.Post_Code = drawFeeDC.Zip;
                cust.Phone_No = drawFeeDC.PhoneNo;
                cust.E_Mail = drawFeeDC.Email1;
                //cust.taxLiable = true;
                //cust.currencyId = "00000000-0000-0000-0000-000000000000";
                //cust.Currency_Code = "USD";
                //cust.paymentTermsId = "5335f8d6-c141-ec11-bb7b-000d3a256200";
                //cust.Payment_Method_Code = "BANK";
                cust.Payment_Method_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Payment_Method_Code").FirstOrDefault().Value;
                //cust.Payment_Terms_Code = "1M(8D)";
                //cust.Payment_Terms_Code = "30 DAYS";
                cust.Payment_Terms_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Payment_Terms_Code").FirstOrDefault().Value;
                cust.Blocked = "";
                //cust.Customer_Posting_Group = "ACCOUNTS REC";
                cust.Customer_Posting_Group = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Customer_Posting_Group").FirstOrDefault().Value;
                //cust.Gen_Bus_Posting_Group = "DOMESTIC";
                cust.Gen_Bus_Posting_Group = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Gen_Bus_Posting_Group").FirstOrDefault().Value;
                cust.Country_Region_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Country_Region_Code").FirstOrDefault().Value;

                //cust.id = "00000000-0000-0000-0000-000000000000";
                //cust.id = "";

                string CustomerNo = CheckCustomerinDynamicsByOdataForSandbox(cust, authToken);
                //string CustomerNo = "" ;

                HttpResponseMessage result = null;

                // call business central api
                DynamicsODataCustomerUpdate updt = new DynamicsODataCustomerUpdate();
                updt.Contact = drawFeeDC.FirstName + " " + drawFeeDC.LastName;
                updt.Address = cust.Address;
                updt.Address_2 = cust.Address_2;
                updt.City = cust.City;
                updt.State = cust.State;
                updt.Post_Code = cust.Post_Code;
                updt.Phone_No = cust.Phone_No;
                updt.E_Mail = cust.E_Mail;
                //updt.Country_Region_Code = "US";
                updt.Country_Region_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Country_Region_Code").FirstOrDefault().Value;
                updt.Payment_Method_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Payment_Method_Code").FirstOrDefault().Value;
                updt.Payment_Terms_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Payment_Terms_Code").FirstOrDefault().Value;
                updt.Customer_Posting_Group = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Customer_Posting_Group").FirstOrDefault().Value;
                updt.Gen_Bus_Posting_Group = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Gen_Bus_Posting_Group").FirstOrDefault().Value;
                updt.Country_Region_Code = DynamicsAppConfig.Where(i => i.Key == "Dynamics_Country_Region_Code").FirstOrDefault().Value;
                string inputjsonstring = "";
                HttpClient _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                //Set the Authorization header with the Access Token received specifying the Credentials
                if (string.IsNullOrEmpty(CustomerNo))
                {
                    inputjsonstring = JsonConvert.SerializeObject(cust);
                }
                else
                {
                    inputjsonstring = JsonConvert.SerializeObject(updt);
                }


                //using (var content = new StringContent(JsonConvert.SerializeObject(jsonInput), System.Text.Encoding.UTF8, "application/json"))
                using (var content = new StringContent(inputjsonstring, System.Text.Encoding.UTF8, "application/json"))
                {

                    if (string.IsNullOrEmpty(CustomerNo))
                    {
                        result = _httpClient.PostAsync(BusinessCentralAPIURL, content).Result;

                    }
                    else //Update customer
                    {
                        BusinessCentralAPIURL = BusinessCentralAPIURL + "('" + CustomerNo + "')";
                        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "*");
                        result = _httpClient.PatchAsync(BusinessCentralAPIURL, content).Result;
                    }
                    if (result.StatusCode == System.Net.HttpStatusCode.Created || result.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string resultString = result.Content.ReadAsStringAsync().Result;
                        var jsonOutputResult = JsonConvert.DeserializeObject<DynamicsODataCustomerOutput>(resultString);
                        QBDCustomerInputDataContract qbdcustomer = new QBDCustomerInputDataContract();
                        //qbdcustomer.ID = jsonOutputResult.id;
                        qbdcustomer.FullName = jsonOutputResult.Name;
                        qbdcustomer.CustomerNo = jsonOutputResult.No;
                        qbdcustomer.ID = jsonOutputResult.SystemId;
                        qbdcustomer.ContactID = jsonOutputResult.Primary_Contact_No;
                        wfLogic.AddUpdateQBDCustomerForSandbox(headerUserID.ToString(), qbdcustomer);
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "Authentication succeeded"
                        };
                    }
                    else
                    {
                        string resultString = result.Content.ReadAsStringAsync().Result;
                        var jsonResultError = JsonConvert.DeserializeObject<error>(resultString);
                        string errmessage = string.IsNullOrEmpty(jsonResultError.message) ? resultString : jsonResultError.message;
                        errmessage = errmessage + "-Url-" + BusinessCentralAPIURL + "-status code-" + result.StatusCode;
                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Sandbox - Error occurred while creating customer " + drawFeeDC.DealName + " in Dynamics" + CreatedFrom, "", headerUserID.ToString(), "CreateQBDCustomer", errmessage);

                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = "Authentication failed"
                        };
                        EmailDataContract emaildc = new EmailDataContract();
                        emaildc.ModuleId = 705;
                        emaildc.Subject = "Sandbox - Dynamics 365 Customer Error Notification";
                        emaildc.Body = "Sandbox - Error occurred while creating customer " + drawFeeDC.DealName + " in Dynamics - " + errmessage;
                        _iEmailNotification.SendErrorNotificationEmail(emaildc);
                    }

                    //End-customer by Odata api

                }
            }

            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Sandbox - Error occurred while creating customer " + drawFeeDC.DealName + " in dynamics" + CreatedFrom, "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }

        //[HttpGet]
        //[Route("api/wfcontroller/getallsandboxcustomer")]
        public List<DynamicsODataCustomerOutput> GetAllSandboxCustomer()
        {
            GenericResult _authenticationResult = null;
            List<DynamicsODataCustomerOutput> lstcust = new List<DynamicsODataCustomerOutput>();
            WFLogic wfLogic = new WFLogic();
            var headerUserID = new Guid();
            try
            {
                if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                {
                    headerUserID = new Guid(Request.Headers["TokenUId"]);
                }
            }
            catch { }
            try
            {
                SetDynamicsAppConfig();
                GetConfigRoot();
                IConfiguration config = configroot.GetSection("Dynamics365BusinessCentralDetail");
                string BusinessCentralAPIURL = config.GetSection("BusinessCentralOdataBaseURL").Value;
                string BusinessCentralCompanyName = config.GetSection("CompanyName").Value;
                BusinessCentralAPIURL = BusinessCentralAPIURL + "Company('" + BusinessCentralCompanyName + "')/"
                + "CustomerListAPI";
                string authToken = GetBusinessCentralAccessToken();
                if (string.IsNullOrEmpty(authToken))
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed"
                    };
                    LoggerLogic Log = new LoggerLogic();
                    Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Sandbox - Error occurred while getting customer in Dynamics - GetAllSandboxCustomer", "", headerUserID.ToString(), "GetAllSandboxCustomer", "Unauthentication token");

                }

                HttpClient _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                HttpResponseMessage result = _httpClient.GetAsync(BusinessCentralAPIURL).Result;
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string resultString = result.Content.ReadAsStringAsync().Result;
                    resultString = resultString.Substring(resultString.IndexOf('['), resultString.Length - resultString.IndexOf('[') - 1);
                    var jsonOutputResult = JsonConvert.DeserializeObject<List<DynamicsODataCustomerOutput>>(resultString);
                    lstcust = jsonOutputResult;
                }
                else
                {
                    string resultString = result.Content.ReadAsStringAsync().Result;
                    var jsonResultError = JsonConvert.DeserializeObject<error>(resultString);
                    string errmessage = string.IsNullOrEmpty(jsonResultError.message) ? resultString : jsonResultError.message;
                    errmessage = errmessage + "-Url-" + BusinessCentralAPIURL + "-status code-" + result.StatusCode;
                    LoggerLogic Log = new LoggerLogic();
                    Log.WriteLogExceptionMessage(CRESEnums.Module.DrawFee.ToString(), "Sanbbox - Error occurred while getting customer in dynamics-- GetAllSandboxCustomer ", "", headerUserID.ToString(), "GetAllSandboxCustomer", errmessage);
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.DrawFee.ToString(), "Sandbox - Error occurred while getting customer in dynamics - GetAllSandboxCustomer", "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);
            }
            return lstcust;
        }

        public string CheckCustomerinDynamicsByOdataForSandbox(DynamicsODataCustomerInput cust, string authToken)
        {
            string CustomerNo = "";
            WFLogic _wfLogic = new WFLogic();
            var headerUserID = new Guid();
            string[] custname = { };
            try
            {
                if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                {
                    headerUserID = new Guid(Request.Headers["TokenUId"]);
                }
            }
            catch { }
            try
            {
                IConfiguration config = configroot.GetSection("Dynamics365BusinessCentralDetail");
                custname = cust.Name.Split("&");
                string CompanyID = config.GetSection("CompanyID").Value;
                string BusinessCentralAPIURL = config.GetSection("BusinessCentralBaseURL").Value
                    + "companies(" + CompanyID + ")/customers?$filter=displayName eq " + "'" + cust.Name + "'";

                if (custname.Length > 1)
                {
                    BusinessCentralAPIURL = config.GetSection("BusinessCentralBaseURL").Value
                       + "companies(" + CompanyID + ")/customers?$filter=startswith(displayName," + "'" + custname[0].Trim() + "') and endswith(displayName," + "'" + custname[custname.Length - 1].Trim() + "')";
                }


                HttpClient _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //Set the Authorization header with the Access Token received specifying the Credentials

                //using (var content = new StringContent(JsonConvert.SerializeObject(jsonInput), System.Text.Encoding.UTF8, "application/json"))

                HttpResponseMessage result = _httpClient.GetAsync(BusinessCentralAPIURL).Result;

                if (result.StatusCode == System.Net.HttpStatusCode.OK || result.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    string resultString = result.Content.ReadAsStringAsync().Result;
                    var jsonOutputResult = JsonConvert.DeserializeObject<SearchCustomerOutput>(resultString);
                    QBDCustomerInputDataContract qbdcustomer = new QBDCustomerInputDataContract();
                    if (jsonOutputResult.value != null && jsonOutputResult.value.Count > 0)
                    {
                        qbdcustomer.ID = jsonOutputResult.value.FirstOrDefault().id;
                        qbdcustomer.CustomerNo = jsonOutputResult.value.FirstOrDefault().number;
                        CustomerNo = qbdcustomer.CustomerNo;
                        qbdcustomer.FullName = jsonOutputResult.value.FirstOrDefault().displayName;

                    }


                }
            }
            catch (Exception ex)
            {
                CustomerNo = "";
            }
            return CustomerNo;
        }

        //sandbox code-end

        [HttpGet]
        [Route("api/wfcontroller/createmissingcustomerinsandboxbyapi")]
        public void CreateMissingCustomerInSandboxAPI()
        {
            GetConfigSetting();
            List<DrawFeeInvoiceDataContract> lstdrawDC = new List<DrawFeeInvoiceDataContract>();
            WFLogic wfLogic = new WFLogic();
            lstdrawDC = wfLogic.GetMissingQBDCustomerInSandbox();

            List<DynamicsODataCustomerOutput> lstCustSandbox = GetAllSandboxCustomer();
            var result = lstdrawDC.Where(p => !lstCustSandbox.Any(x => x.Name == p.DealName)).ToList();

            IActionResult ActionResult = null;
            foreach (DrawFeeInvoiceDataContract drawFeeDC in result)
            {
                drawFeeDC.CreatedFrom = "Scheduler";
                ActionResult = CreateCustomerInSandbox(drawFeeDC);
            }
        }

        /*
        [HttpGet]
        [Services.Controllers.DeflateCompression]
        [Route("api/downloadinvoiceinbytes")]
        public DownloadInvoiceResult DownloadInvoiceInBytes(string Key,string Id)
        {
            var headerBackshopAuthKey = string.Empty;
            //string filename = "ACORE Draw Fee Invoice – 10 West End - 18-2204-DR-0001.pdf";
            DownloadInvoiceResult _invoiceResult = null;
            WFLogic _wfLogic = new WFLogic();
            DrawFeeInvoiceDataContract _dtInvoice = new DrawFeeInvoiceDataContract();

            //

            GetConfigSetting();
            string Location = Sectionroot.GetSection("storage:invoiceStorageLocation").Value;
            var invoiceStorageTypeId = Sectionroot.GetSection("storage:invoiceStorageTypeId").Value;
            var BackshopAuthKey = Sectionroot.GetSection("BackshopAuthKey").Value;
            try
            {


                //if (!string.IsNullOrEmpty(Request.Headers["AuthenticationKey"]))
                //{
                //    headerBackshopAuthKey = Convert.ToString(Request.Headers["AuthenticationKey"]);
                //}
                //if (string.IsNullOrEmpty(headerBackshopAuthKey) == true || BackshopAuthKey != headerBackshopAuthKey)
                //{
                //    _invoiceResult = new DownloadInvoiceResult()
                //    {
                //        Succeeded = false,
                //        Message = "User Authentication failed."
                //    };
                //    return NotFound("Authentication failed");
                //}
                if (string.IsNullOrEmpty(Key))
                {
                    //return NotFound("Authentication key is required");

                    _invoiceResult = new DownloadInvoiceResult()
                    {
                        Succeeded = false,
                        Message = "Invoice id is required."
                    };
                    //return NotFound("Authentication failed");
                }
                else if (string.IsNullOrEmpty(Id))
                {
                    _invoiceResult = new DownloadInvoiceResult()
                    {
                        Succeeded = false,
                        Message = "Invoice id is required."
                    };
                    //return NotFound("Invoice id is required");
                }


                if (BackshopAuthKey != Key)
                {
                    _invoiceResult = new DownloadInvoiceResult()
                    {
                        Succeeded = false,
                        Message = "User Authentication failed."
                    };
                    //return NotFound("Authentication failed");
                }


                _dtInvoice = _wfLogic.GetInvoiceDetailByInvoiceNo("", Id);

                if (string.IsNullOrEmpty(_dtInvoice.InvoiceNo))
                {
                    _invoiceResult = new DownloadInvoiceResult()
                    {
                        Succeeded = false,
                        Message = "Invoice is not generated"
                    };
                    //return NotFound("Invoice is not generated");
                }

                MemoryStream memStreamDownloaded = new MemoryStream();
                if (invoiceStorageTypeId == "392")
                {

                    var Container = Sectionroot.GetSection("storage:container:name").Value;
                    var accountName = Sectionroot.GetSection("storage:account:name").Value;
                    var accountKey = Sectionroot.GetSection("storage:account:key").Value;
                    var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer excelContainer = blobClient.GetContainerReference(Container);
                    CloudBlobDirectory dr = excelContainer.GetDirectoryReference(Location);
                    CloudBlockBlob cloudBlockBlob = dr.GetBlockBlobReference(_dtInvoice.FileName);
                    cloudBlockBlob.DownloadToStream(memStreamDownloaded);
                    MemoryStream memStream = new MemoryStream(memStreamDownloaded.ToArray());
                    //return File(memStream, "text/plain", _dtInvoice.FileName);
                }
                else
                {
                    DocumentDataContract _docDC = new DocumentDataContract();
                    _docDC.FolderName = Location;
                    _docDC.FileName = _dtInvoice.FileName;

                    var memStreamDown = new BoxHelper().DownloadFile(_docDC);
                    // MemoryStream memStream = new MemoryStream(memStreamDown.Result.ToArray());
                    memStreamDownloaded = new MemoryStream(memStreamDown.Result.ToArray());
                    //return File(memStream, "text/plain", _dtInvoice.FileName);
                }

                _invoiceResult = new DownloadInvoiceResult()
                {
                    Succeeded = true,
                    Message = "file Downloaded successfully",
                    FileName = _dtInvoice.FileName,
                    FileByte = memStreamDownloaded.ToArray()
                };

            }
            catch (Exception ex)
            {
                _invoiceResult = new DownloadInvoiceResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
                //return NotFound("File not found");
            }
            return _invoiceResult;
            //return File(memStreamDownloaded, "application/pdf", filename);
        }
        */


        [HttpGet]
        [Route("api/downloadinvoice")]
        public async Task<IActionResult> DownloadInvoice(string InvNo)
        {
            var headerBackshopAuthKey = string.Empty;
            //string filename = "ACORE Draw Fee Invoice – 10 West End - 18-2204-DR-0001.pdf";
            DownloadInvoiceResult _invoiceResult = null;
            WFLogic _wfLogic = new WFLogic();
            DrawFeeInvoiceDataContract _dtInvoice = new DrawFeeInvoiceDataContract();
            MemoryStream memStreamDownloaded = new MemoryStream();
            //
            string filename = "";
            GetConfigSetting();
            string Location = Sectionroot.GetSection("storage:invoiceStorageLocation").Value;
            var invoiceStorageTypeId = Sectionroot.GetSection("storage:invoiceStorageTypeId").Value;
            var BackshopAuthKey = Sectionroot.GetSection("BackshopAuthKey").Value;
            try
            {

                if (!string.IsNullOrEmpty(Request.Headers["AuthenticationKey"]))
                {
                    headerBackshopAuthKey = Convert.ToString(Request.Headers["AuthenticationKey"]);
                }
                if (string.IsNullOrEmpty(headerBackshopAuthKey) == true || BackshopAuthKey != headerBackshopAuthKey)
                {
                    _invoiceResult = new DownloadInvoiceResult()
                    {
                        Succeeded = false,
                        Message = "User Authentication failed."
                    };
                    return NotFound("User Authentication failed");
                }

                if (string.IsNullOrEmpty(InvNo))
                {
                    _invoiceResult = new DownloadInvoiceResult()
                    {
                        Succeeded = false,
                        Message = "Invoice No is required."
                    };
                    return NotFound("Invoice No is required");
                }



                _dtInvoice = _wfLogic.GetInvoiceDetailByInvoiceNo("", InvNo);

                if (string.IsNullOrEmpty(_dtInvoice.InvoiceNo))
                {
                    _invoiceResult = new DownloadInvoiceResult()
                    {
                        Succeeded = false,
                        Message = "File not found"
                    };
                    return NotFound("File not found");
                }


                if (invoiceStorageTypeId == "392")
                {

                    var Container = Sectionroot.GetSection("storage:container:name").Value;
                    var accountName = Sectionroot.GetSection("storage:account:name").Value;
                    var accountKey = Sectionroot.GetSection("storage:account:key").Value;
                    var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer excelContainer = blobClient.GetContainerReference(Container);
                    CloudBlobDirectory dr = excelContainer.GetDirectoryReference(Location);
                    CloudBlockBlob cloudBlockBlob = dr.GetBlockBlobReference(_dtInvoice.FileName);
                    await cloudBlockBlob.DownloadToStreamAsync(memStreamDownloaded);
                    MemoryStream memStream = new MemoryStream(memStreamDownloaded.ToArray());
                    filename = _dtInvoice.FileName;
                    return File(memStream, "text/plain", _dtInvoice.FileName);
                }
                else
                {
                    DocumentDataContract _docDC = new DocumentDataContract();
                    _docDC.FolderName = Location;
                    _docDC.FileName = _dtInvoice.FileName;

                    memStreamDownloaded = await new BoxHelper().DownloadFile(_docDC);
                    // MemoryStream memStream = new MemoryStream(memStreamDown.Result.ToArray());
                    MemoryStream memStream = new MemoryStream(memStreamDownloaded.ToArray());
                    return File(memStream, "text/plain", _dtInvoice.FileName);
                }
            }
            catch (Exception ex)
            {
                return NotFound("There is some error Downloading the file.");
            }
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/deal/checkwfconcurrency")]
        public IActionResult CheckWFConcurrency([FromBody] WFDetailDataContract _wfDetailDataContract)
        {
            GenericResult _authenticationResult = null;
            DealDataContract Deal = new DealDataContract();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            WFLogic _WFLogic = new WFLogic();
            WFConcurrencyParams prms = new WFConcurrencyParams();
            prms.TaskTypeID = _wfDetailDataContract.TaskTypeID;
            prms.TaskID = _wfDetailDataContract.TaskID;
            prms.WFStatusPurposeMappingID = _wfDetailDataContract.WFStatusPurposeMappingID;
            int status = _WFLogic.CheckWFConcurrency(headerUserID, prms);

            try
            {
                if (status == 0)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "",
                        StatusCode = status
                    };
                }
                if (status == 1)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "",
                        StatusCode = status

                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Deal.ToString(), "Error occurred in CheckWFConcurrency", "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);
                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message,
                    StatusCode = 2
                };
            }
            return Ok(_authenticationResult);
        }
        //AttachGenericInvoiceSendEmail
        public void AttachGenericInvoiceSendEmail(DrawFeeInvoiceDataContract DrawFeeDC)
        {
            GetConfigSetting();
            string Location = Sectionroot.GetSection("storage:invoiceStorageLocation").Value;
            var invoiceStorageTypeId = Sectionroot.GetSection("storage:invoiceStorageTypeId").Value;
            MemoryStream memStreamDownloaded = new MemoryStream();
            if (invoiceStorageTypeId == "392")
            {

                var Container = Sectionroot.GetSection("storage:container:name").Value;
                var accountName = Sectionroot.GetSection("storage:account:name").Value;
                var accountKey = Sectionroot.GetSection("storage:account:key").Value;
                var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer excelContainer = blobClient.GetContainerReference(Container);
                CloudBlobDirectory dr = excelContainer.GetDirectoryReference(Location);
                CloudBlockBlob cloudBlockBlob = dr.GetBlockBlobReference(DrawFeeDC.FileName);
                cloudBlockBlob.DownloadToStream(memStreamDownloaded);
                // MemoryStream memStream = new MemoryStream(memStreamDownloaded.ToArray());
            }
            else
            {
                DocumentDataContract _docDC = new DocumentDataContract();
                _docDC.FolderName = Location;
                _docDC.FileName = DrawFeeDC.FileName;
                var memStreamDown = new BoxHelper().DownloadFile(_docDC);
                // MemoryStream memStream = new MemoryStream(memStreamDown.Result.ToArray());
                memStreamDownloaded = new MemoryStream(memStreamDown.Result.ToArray());
            }
            DrawFeeDC.filestream = memStreamDownloaded;
            _iEmailNotification.SendGenericInvoiceNotification(DrawFeeDC);

        }
    }
}