using CRES.BusinessLogic;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using CRES.BusinessLogic;
using CRES.DataContract;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Data;
using CRES.Utilities;
using Amazon.CodePipeline.Model;
using System.Threading;
//using Syncfusion.DocIO.DLS;
using Microsoft.Identity.Client;

namespace CRES.ServiceMVC.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class EquityController : ControllerBase
    {
        private string useridforSys_Scheduler = "3D6DB33D-2B3A-4415-991D-A3DA5CEB8B50";
        Microsoft.Extensions.Configuration.IConfigurationSection Sectionroot = null;
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
        private IHostingEnvironment _env;

        private readonly IEmailNotification _iEmailNotification;
        public EquityController(IEmailNotification iemailNotification, IHostingEnvironment env)
        {
            _iEmailNotification = iemailNotification;
            _env = env;
        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/equity/getallLookup")]
        public IActionResult GetAllLookup()
        {

            string getAllLookup = "1,2,51";
            GenericResult _authenticationResult = null;
            List<LookupDataContract> lstlookupDC = new List<LookupDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            LookupLogic lookupLogic = new LookupLogic();
            lstlookupDC = lookupLogic.GetAllLookups(getAllLookup);
            lstlookupDC = lstlookupDC.OrderBy(x => x.SortOrder).ToList();

            LiabilityNoteLogic LiabilityNoteLogic = new LiabilityNoteLogic();
            DataTable dt = LiabilityNoteLogic.GetAccountCategoryList();
            try
            {
                if (lstlookupDC != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstLookups = lstlookupDC,
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
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Deal.ToString(), "Error occured in GetAllLookup for deal id", "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/equity/addnewequity")]
        [Services.Controllers.DeflateCompression]
        public IActionResult AddNewEquity([FromBody] EquityDataContract _equityDC)
        {
            GenericResult _actionResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            string actiontype = "";

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            EquityLogic _equityLogic = new EquityLogic();
            LiabilityNoteLogic liabilityNoteLogic = new LiabilityNoteLogic();
            if (_equityDC.EquityID == null)
            {
                _equityDC.EquityID = 0;
                actiontype = "Insert";
            }
            if (_equityDC.EquityName != _equityDC.OriginalEquityName && actiontype == "")
            {
                actiontype = "Update";
            }
            EquityDataContract result = _equityLogic.InsertUpdateEquity(new Guid(headerUserID), _equityDC);

            liabilityNoteLogic.InsertUpdateLiabilityFundingScheduleAggregate(_equityDC.liabilityMasterFunding, headerUserID);
            if (_equityDC.ListLiabilityFundingSchedule != null)
            {
                if (_equityDC.ListLiabilityFundingSchedule.Count > 0)
                {
                    int rowno = 1;
                    foreach (var item in _equityDC.ListLiabilityFundingSchedule)
                    {
                        if (item.AssetAccountID == null || item.AssetAccountID == "")
                        {
                            item.AssetAccountID = "00000000-0000-0000-0000-000000000000";
                        }
                        item.RowNo = rowno;
                        rowno = rowno + 1;
                    }
                    liabilityNoteLogic.InsertUpdatedLiabilityFundingSchedule(_equityDC.ListLiabilityFundingSchedule, headerUserID);

                }
            }

            //_equityLogic.InsertUpdatedDebtAdditionalTrans(addTrans, headerUserID);
            string LiabilityTypeID = result.EquityGUID.ToString();
            if (_equityDC.ListSelectedXIRRTags != null)
            {
                TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
                tagXIRRLogic.InsertUpdateTagAccountMappingXIRR(result.EquityAccountID, _equityDC.ListSelectedXIRRTags, headerUserID);
            }
            if (actiontype != "")
            {
                Thread SecondThread = new Thread(() => InsertUpdateAIEntities(_equityDC.EquityName, headerUserID, actiontype, _equityDC.OriginalEquityName));
                SecondThread.Start();
            }

            try
            {
                if (result != null)
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Changes were saved successfully.",
                        LiabilityTypeID = LiabilityTypeID
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
                string message = ExceptionHelper.GetFullMessage(ex);
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Equity.ToString(), "Error occurred while saving Equity with : Equity ID " + _equityDC.EquityName + " :" + message, _equityDC.EquityName.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_actionResult);
        }


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/equity/getequitybyequityId")]
        [Services.Controllers.DeflateCompression]
        public IActionResult GetEquityByEquityId([FromBody] EquityDataContract _equityDC)
        {
            GenericResult _acationResult = null;
            EquityDataContract objEquity = new EquityDataContract();
            EquityLogic _equityLogic = new EquityLogic();
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            if (_equityDC.EquityGUID == null)
                _equityDC.EquityGUID = "00000000-0000-0000-0000-000000000000";

            objEquity = _equityLogic.GetEquityByEquityID(new Guid(_equityDC.EquityGUID));
            List<ScheduleEffectiveDateLiabilityDataContract> ListEffectiveDateCount = new List<ScheduleEffectiveDateLiabilityDataContract>();
            LiabilityNoteLogic LiabilityNotelogic = new LiabilityNoteLogic();
            ListEffectiveDateCount = LiabilityNotelogic.GetScheduleEffectiveDateCount(new Guid(objEquity.EquityAccountID));

            objEquity.ListSelectedXIRRTags = tagXIRRLogic.GetTagMasterXIRRByAccountID(objEquity.EquityAccountID);

            NoteLogic _NoteLogic = new NoteLogic();
            List<HolidayListDataContract> ListHoliday = new List<HolidayListDataContract>();

            if (objEquity.CapitalCallNoticeBusinessDays != null && objEquity.CapitalCallNoticeBusinessDays != null)
            {
                ListHoliday = _NoteLogic.GetHolidayList();
                var todaydate = DateTime.Now.Date;
                objEquity.EarliestEquityArrival = DateExtensions.GetnextWorkingDays(Convert.ToDateTime(todaydate), Convert.ToInt16(objEquity.CapitalCallNoticeBusinessDays), "US", ListHoliday).Date;
            }
            try
            {
                if (objEquity != null)
                {
                    _acationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        EquityData = objEquity,
                        ListEffectiveDateCount = ListEffectiveDateCount

                    };
                }
                else
                {
                    _acationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Not Exists",
                        StatusCode = 404
                    };
                }
            }
            catch (Exception ex)
            {
                string message = ExceptionHelper.GetFullMessage(ex);
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Equity.ToString(), "Error in GetEquityByEquityId :" + message, objEquity.EquityAccountID.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _acationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_acationResult);
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/equity/getEquityNoteByLiabilityTypeID")]
        public IActionResult GetEquityNoteByLiabilityTypeID([FromBody] string LiabilityTypeID)
        {
            GenericResult _authenticationResult = null;
            List<LiabilityNoteDataContract> ListLiabilityNotes = new List<LiabilityNoteDataContract>();
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            LiabilityNoteLogic _LiabilityLogic = new LiabilityNoteLogic();
            if (LiabilityTypeID != null && LiabilityTypeID != "" && LiabilityTypeID != "00000000-0000-0000-0000-000000000000")
            {
                ListLiabilityNotes = _LiabilityLogic.GetDebtorEquityNoteByLiabilityTypeID(new Guid(LiabilityTypeID.ToString()));
            }

            try
            {
                if (ListLiabilityNotes != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstNote = ListLiabilityNotes,
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
                string message = ExceptionHelper.GetFullMessage(ex);
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Equity.ToString(), "Error in GetEquityNoteByLiabilityTypeID :" + message, LiabilityTypeID.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/equity/GetEquityAdditionalTransByEquityAccountID")]
        public IActionResult GetEquityAdditionalTransByEquityAccountID([FromBody] string EquityAccountID)
        {
            GenericResult _authenticationResult = null;
            List<AdditionalTransactionDataContract> ListAddTrans = new List<AdditionalTransactionDataContract>();
            IEnumerable<string> headerValues;

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            EquityLogic _equityLogic = new EquityLogic();
            // ListAddTrans = _equityLogic.GetEquityAdditionalTransByEquityAccountID(new Guid(EquityAccountID.ToString()));
            try
            {
                //ListAddTrans.Add(new AdditionalTransactionDataContract
                //{
                //    TransactionDate = DateTime.Now,
                //    TransactionAmount = 100.00M,
                //    TransactionTypeText = "Misc Fees",
                //    Comments = "Sample comment"
                //});

                if (ListAddTrans != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        ListAddTrans = ListAddTrans
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
                string message = ExceptionHelper.GetFullMessage(ex);
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Equity.ToString(), "Error in GetEquityAdditionalTransByEquityAccountID :" + message, EquityAccountID.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/equity/GetEquityTransactionByEquityAccountID")]
        public IActionResult GetEquityTransactionByEquityAccountID([FromBody] string EquityAccountID)
        {
            GenericResult _authenticationResult = null;
            DataTable dt = new DataTable();
            string AnalysisId = "c10f3372-0fc2-4861-a9f5-148f1f80804f";
            var headerUserID = string.Empty;
            EquityDataContract eq = new EquityDataContract();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            LiabilityNoteLogic _Logic = new LiabilityNoteLogic();
            dt = _Logic.GeDebtOrEquityTransactionByAccountID(EquityAccountID, AnalysisId);

            EquityLogic _equityLogic = new EquityLogic();
            eq = _equityLogic.GetEquityCalcInfoByEquityAccountID(new Guid(EquityAccountID), new Guid(headerUserID));
            try
            {
                if (dt != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        LiabilityCashFlow = dt,
                        eqstatus = eq
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
                string message = ExceptionHelper.GetFullMessage(ex);
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Equity.ToString(), "Error in GeDebtOrEquityTransactionByAccountID :" + message, EquityAccountID.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/search/getAutosuggestDebtNameSubline")]
        public IActionResult GetAutosuggestDebtNameSubline([FromBody] string searchkey)
        {
            GenericResult _auctionResult = null;
            List<SearchDataContract> lstSearchResult = new List<SearchDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            EquityLogic _equityLogic = new EquityLogic();
            lstSearchResult = _equityLogic.GetAutosuggestDebtNameSubline(searchkey);

            try
            {
                if (lstSearchResult != null)
                {
                    _auctionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstSearch = lstSearchResult
                    };
                }
                else
                {
                    _auctionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed"
                    };
                }
            }
            catch (Exception ex)
            {
                _auctionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_auctionResult);
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/equity/GetEquityJournalLedgerbyJournalEntryMasterID")]
        public IActionResult GetEquityJournalLedgerbyJournalEntryMasterID([FromBody] string DebtEquityAccountID)
        {
            GenericResult _authenticationResult = null;
            List<JournalLedgerDataContract> ListjournalLedger = new List<JournalLedgerDataContract>();
            IEnumerable<string> headerValues;

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            LiabilityNoteLogic LiabilityNotelogic = new LiabilityNoteLogic();

            JournalEntryLogic _JournalLogic = new JournalEntryLogic();
            ListjournalLedger = _JournalLogic.GetJournalEntryByDebtEquityAccountID(new Guid(DebtEquityAccountID.ToString()));
            try
            {
                if (ListjournalLedger != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        ListjournalLedger = ListjournalLedger
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
                string message = ExceptionHelper.GetFullMessage(ex);
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Equity.ToString(), "Error in GetJournalEntryByDebtEquityAccountID :" + message, DebtEquityAccountID.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);


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
        [Route("api/equity/GetHistoricalDataOfModuleByAccountId_Liability")]
        public IActionResult GetHistoricalDataOfModuleByAccountId_Liability([FromBody] EquityDataContract _noteDC, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;

            string modulename = _noteDC.modulename;

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            LiabilityNoteLogic LiabilityNotelogic = new LiabilityNoteLogic();

            try
            {
                bool flag = false;
                int? totalCount = 0;

                DataTable dtGeneralSetupDetails = new DataTable();
                switch (modulename)
                {
                    case "GeneralSetupDetailsEquity":

                        dtGeneralSetupDetails = LiabilityNotelogic.GetHistoricalDataOfModuleByNoteId(new Guid(_noteDC.EquityAccountID), headerUserID, modulename);
                        if (dtGeneralSetupDetails.Rows.Count > 0) flag = true;

                        break;

                    default:
                        break;
                }

                if (flag)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        lstGeneralSetupDetails = dtGeneralSetupDetails
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Not Exists",
                        StatusCode = 404
                    };
                }
            }
            catch (Exception ex)
            {
                string message = ExceptionHelper.GetFullMessage(ex);
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Equity.ToString(), "Error in GetHistoricalDataOfModuleByNoteId :" + message, _noteDC.EquityAccountID.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);


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
        [Route("api/equity/checkduplicateforliabilities")]
        public IActionResult CheckDuplicateforLiabilities([FromBody] EquityDataContract _equityDC)
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            string Status = "";
            string msg = "";

            LiabilityNoteLogic LiabilityNotelogic = new LiabilityNoteLogic();
            Status = LiabilityNotelogic.CheckDuplicateforLiabilities(_equityDC.EquityName, _equityDC.modulename, new Guid(_equityDC.EquityAccountID));
            if (Status == "True")
            {
                msg = "Equity " + _equityDC.EquityName + " already exist. Please enter unique Equity Name.";

            }
            else if (Status == "False")
            {
                msg = "Save";
            }
            try
            {
                if (msg != "")
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = msg
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Error.",
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
        [Route("api/equity/queueEquityForCalculation")]
        public IActionResult QueueEquityForCalculation([FromBody] string EquityAccountID)
        {
            v1GenericResult _authenticationResult = null;
            LoggerLogic Log = new LoggerLogic();

            try
            {
                GenerateAutomationLogic GenerateAutomationLogic = new GenerateAutomationLogic();
                List<GenerateAutomationDataContract> list = new List<GenerateAutomationDataContract>();
                GenerateAutomationDataContract gad = new GenerateAutomationDataContract();
                gad.DealID = Convert.ToString(EquityAccountID);
                gad.StatusText = "Processing";
                gad.AutomationType = 853;
                gad.BatchType = "LiabilityCalculation";
                list.Add(gad);

                if (list != null && list.Count > 0)
                {
                    GenerateAutomationLogic.QueueDealForAutomation(list, useridforSys_Scheduler);
                    Log.WriteLogInfo(CRESEnums.Module.EquityCalculator.ToString(), "QueueEquityForCalculation ended  ", "", "");
                }
                else
                {
                    Log.WriteLogInfo(CRESEnums.Module.EquityCalculator.ToString(), "QueueEquityForCalculation no record found ", "", "");
                }
                _authenticationResult = new v1GenericResult()
                {
                    Status = Microsoft.AspNetCore.Http.StatusCodes.Status200OK,
                    Succeeded = true,
                    Message = "Equity Queued for Calculation. ",
                    ErrorDetails = ""
                };
            }
            catch (Exception ex)
            {
                Log.WriteLogExceptionMessage(CRESEnums.Module.EquityCalculator.ToString(), ex.StackTrace, "", "", "QueueEquityForCalculation", "Error occurred " + " " + ex.Message);
                _authenticationResult = new v1GenericResult()
                {
                    Status = Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError,
                    Succeeded = false,
                    Message = "Error occurred while Queuing Equity for Calculation. Please contact administrator.",
                    ErrorDetails = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/equity/getEquityCalcInfoByEquityAccountID")]
        public IActionResult GetEquityCalcInfoByEquityAccountID([FromBody] EquityDataContract equityData)
        {
            GenericResult _authenticationResult = null;
            EquityDataContract eq = new EquityDataContract();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            EquityLogic _equityLogic = new EquityLogic();
            eq = _equityLogic.GetEquityCalcInfoByEquityAccountID(new Guid(equityData.EquityAccountID), new Guid(headerUserID));

            try
            {
                if (eq != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        eqstatus = eq
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
        [Route("api/equity/getEquityCashflowsExportExcel")]
        public IActionResult GetEquityCashflowsExportExcel([FromBody] string EquityAccountID)
        {
            GenericResult _authenticationResult = null;
            DataTable lstEquityCashflowsExportData = new DataTable();
            DataTable lstEquityCashflowsDetail = new DataTable();
            var headerUserID = new Guid();
            string AnalysisId = "c10f3372-0fc2-4861-a9f5-148f1f80804f";

            try
            {
                if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                {
                    headerUserID = new Guid(Request.Headers["TokenUId"]);
                };

                EquityLogic _Logic = new EquityLogic();
                string AccountId = EquityAccountID.Split("||")[0];
                string Type = EquityAccountID.Split("||")[1];

                lstEquityCashflowsExportData = _Logic.GeDebtOrEquityCashflowExportData(AccountId, AnalysisId, Type);
                lstEquityCashflowsDetail = _Logic.GetCashflowExportDataDetail(AccountId, AnalysisId, Type);

                // Export to excel
                DataSet ds = new DataSet();
                lstEquityCashflowsExportData.TableName = "Cashflow Aggregated";
                ds.Tables.Add(lstEquityCashflowsExportData);

                lstEquityCashflowsDetail.TableName = "Cashflow";
                ds.Tables.Add(lstEquityCashflowsDetail);


                Stream stream = new StreamReader(_env.WebRootPath + "/ExcelTemplate/" + "DebtorEquityCashflow_download.xlsx").BaseStream;
                MemoryStream ms = WriteDataToExcel(ds, stream);
                return File(ms, "application/octet-stream");

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.CashFlowDownload.ToString(), "Error occurred in cashflow download ", "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
                return Ok(_authenticationResult);
            }

        }

        [HttpPost]
        [Route("api/equity/getEquityCapitalContributionExportExcel")]
        public IActionResult GetEquityCapitalContributionExportExcel([FromBody] string EquityAccountID)
        {
            GenericResult _authenticationResult = null;
            DataTable lstEquityCapitalContributionExportData = new DataTable();
            var headerUserID = new Guid();

            try
            {
                if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                {
                    headerUserID = new Guid(Request.Headers["TokenUId"]);
                };

                EquityLogic _Logic = new EquityLogic();
                lstEquityCapitalContributionExportData = _Logic.GetEquityCapitalContributionExportExcel(EquityAccountID);

                // Export to excel
                DataSet ds = new DataSet();
                lstEquityCapitalContributionExportData.TableName = "Transactions";
                ds.Tables.Add(lstEquityCapitalContributionExportData);

                Stream stream = new StreamReader(_env.WebRootPath + "/ExcelTemplate/" + "DebtorEquityTransactionsData.xlsx").BaseStream;
                MemoryStream ms = WriteDataToExcel(ds, stream);
                return File(ms, "application/octet-stream");

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.CashFlowDownload.ToString(), "Error occurred in Capital Contribution download ", "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
                return Ok(_authenticationResult);
            }

        }

        public MemoryStream WriteDataToExcel(DataSet dsData, Stream strm)
        {

            DataTable dt = new DataTable();
            Stream TemplateMemoryStream = new MemoryStream();
            List<string> lstTemplateLines = new List<string>();
            try
            {
                using (var package = new OfficeOpenXml.ExcelPackage(strm))
                {

                    int iSheetsCount = 0;
                    try
                    {
                        iSheetsCount = package.Workbook.Worksheets.Count;
                    }
                    catch (Exception)
                    {
                        iSheetsCount = package.Workbook.Worksheets.Count;
                    }
                    if (iSheetsCount > 0)
                    {
                        for (int i = 0; i < iSheetsCount; i++)
                        {
                            OfficeOpenXml.ExcelWorksheet worksheet;
                            try
                            {
                                worksheet = package.Workbook.Worksheets[i];
                            }
                            catch (Exception)
                            {
                                worksheet = package.Workbook.Worksheets[i];
                            }

                            if (dsData.Tables[worksheet.Name] != null)
                            {
                                worksheet.Cells[1, 1].LoadFromDataTable(dsData.Tables[worksheet.Name], true);
                            }

                        }
                        Byte[] fileBytes = package.GetAsByteArray();
                        TemplateMemoryStream = new MemoryStream(fileBytes);
                    }
                }
                return (MemoryStream)TemplateMemoryStream;

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.CashFlowDownload.ToString(), "Error detail(method: ReadAndUploadTemplateFile) :  ", "", "", ex.TargetSite.Name.ToString(), "", ex);

                throw ex;
            }
        }

        public void InsertUpdateAIEntities(string entity_names, string userid, string actiontype, string originalname)
        {
            AIDynamicEntityUpdateLogic _dynamicentity = new AIDynamicEntityUpdateLogic();
            _dynamicentity.InsertUpdateAIEntitiesAsync("EquityName", entity_names, userid, actiontype, originalname);

        }

    }
}
