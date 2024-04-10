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
using Org.BouncyCastle.Asn1.Ocsp;
using CRES.Utilities;
using System.Data;
using Microsoft.Graph;
using System.Threading;

namespace CRES.ServiceMVC.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class LiabilityNoteController : ControllerBase
    {
        Microsoft.Extensions.Configuration.IConfigurationSection Sectionroot = null;

        public void GetConfigSetting()
        {
            if (Sectionroot == null)
            {
                IConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddJsonFile(Path.Combine(System.IO.Directory.GetCurrentDirectory(), "appsettings.json"));
                var root = builder.Build();
                Sectionroot = root.GetSection("Application");
            }
        }
        private IHostingEnvironment _env;

        private readonly IEmailNotification _iEmailNotification;

        public LiabilityNoteController(IEmailNotification iemailNotification, IHostingEnvironment env)
        {
            _iEmailNotification = iemailNotification;
            _env = env;
        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/liabilityNote/getallLookup")]
        public IActionResult GetAllLookup()
        {

            string getAllLookup = "1,51,19,25,32";
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

            LiabilityNoteLogic LiabilityNotelogic = new LiabilityNoteLogic();
            List<LookupDataContract> lstSearch = LiabilityNotelogic.GetAllLiabilityTypeLookup();
            try
            {
                if (lstlookupDC != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstLookups = lstlookupDC
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
        [Services.Controllers.DeflateCompression]
        [Route("api/liabilityNote/InsertUpdateLiabilityNote")]
        public IActionResult InsertUpdateLiabilityNote([FromBody] LiabilityNoteDataContract note)
        {
            LoggerLogic Log = new LoggerLogic();
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            string actiontype = "";
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            LiabilityNoteLogic LiabilityNotlogic = new LiabilityNoteLogic();
            List<LiabilityNoteAssetMapping> LiabilityAssetMap = new List<LiabilityNoteAssetMapping>();
            if (note.LiabilityTypeID == null)
            {
                note.LiabilityTypeID = new Guid();
            }
            if (note.LiabilityNoteAutoID == null)
            {
                note.LiabilityNoteAutoID = 0;
                actiontype = "Insert";
            }
            if (note.OriginalLiabilityNoteID != note.LiabilityNoteID && actiontype == "")
            {
                actiontype = "Update";
            }

            string res = LiabilityNotlogic.InsertUpdateLiabilityNote(note, headerUserID, LiabilityAssetMap);

            string LiabilityNoteAccountID = res;
            if (note.ListLiabilityRate != null)
            {
                foreach (var item in note.ListLiabilityRate)
                {
                    item.LiabilityNoteAccountID = LiabilityNoteAccountID;
                }
            }

            LiabilityNotlogic.InsertUpdateLiabilityRateSpreadSchedule(note.ListLiabilityRate, headerUserID);

            if (actiontype != "")
            {
                Thread SecondThread = new Thread(() => InsertUpdateAIEntities(note.LiabilityNoteID, headerUserID, actiontype, note.OriginalLiabilityNoteID));
                SecondThread.Start();
            }
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
                string message = ExceptionHelper.GetFullMessage(ex);
                Log.WriteLogException(CRESEnums.Module.LiabilityNote.ToString(), "Error in InsertUpdateLiabilityNote: Note ID " + note.LiabilityName, note.LiabilityNoteGUID.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);
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
        [Route("api/liabilityNote/getLiabilityNoteByLiabilityNoteID")]
        public IActionResult GetLiabilityNoteByLiabilityNoteID([FromBody] string LiabilityNoteGUID)
        {

            GenericResult _authenticationResult = null;
            LiabilityNoteDataContract LiabilityNote = new LiabilityNoteDataContract();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            LiabilityNoteLogic LiabilityNotelogic = new LiabilityNoteLogic();
            LiabilityNote = LiabilityNotelogic.GetLiabilityNoteByLiabilityNoteID(new Guid(LiabilityNoteGUID.ToString()));
            try
            {
                if (LiabilityNote != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstLiabilityNote = LiabilityNote
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
                Log.WriteLogException(CRESEnums.Module.LiabilityNote.ToString(), "Error in GetLiabilityNoteByLiabilityNoteID: Note ID " + LiabilityNoteGUID, LiabilityNoteGUID, headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);
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
        [Route("api/search/getAutosuggestDebtAndEquityName")]
        public IActionResult GetAutosuggestDebtAndEquityName([FromBody] string searchkey)
        {
            GenericResult _auctionResult = null;
            List<SearchDataContract> lstSearchResult = new List<SearchDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            LiabilityNoteLogic LiabilityNotelogic = new LiabilityNoteLogic();
            lstSearchResult = LiabilityNotelogic.GetAutosuggestDebtAndEquityName(searchkey);

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
        [Route("api/liabilityNote/getLiabilityFundingScheduleByDealAccountID")]
        public IActionResult GetLiabilityFundingScheduleByDealAccountID([FromBody] string DealAccountID)
        {

            GenericResult _authenticationResult = null;
            List<LiabilityFundingScheduleDataContract> ListLiabilityFundingScheduleDataContract = new List<LiabilityFundingScheduleDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            LiabilityNoteLogic LiabilityNotelogic = new LiabilityNoteLogic();
            ListLiabilityFundingScheduleDataContract = LiabilityNotelogic.GetLiabilityFundingScheduleByDealAccountID(DealAccountID);


            try
            {
                if (ListLiabilityFundingScheduleDataContract != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        ListLiabilityFundingSchedule = ListLiabilityFundingScheduleDataContract
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
        [Route("api/liabilityNote/getAssetListByDealAccountID")]
        public IActionResult GetAssetListByDealAccountID([FromBody] string DealAccountID)
        {

            GenericResult _authenticationResult = null;
            List<LookupDataContract> AssetList = new List<LookupDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            LiabilityNoteLogic LiabilityNotelogic = new LiabilityNoteLogic();
            AssetList = LiabilityNotelogic.GetAssetListByDealAccountID(DealAccountID.ToString());
            DataTable DealInfo = LiabilityNotelogic.GetDealInfoByDealAccountID(DealAccountID.ToString());

            try
            {
                if (AssetList != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        AssetList = AssetList,
                        DealInfo = DealInfo
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
        [Route("api/liabilityNote/getLiabilityFundingScheduleByLiabilityTypeID")]
        public IActionResult GetLiabilityFundingScheduleByLiabilityTypeID([FromBody] string LiabilityTypeID)
        {

            GenericResult _authenticationResult = null;
            List<LiabilityFundingScheduleDataContract> ListLiabilityFundingScheduleDataContract = new List<LiabilityFundingScheduleDataContract>();

            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            LiabilityNoteLogic LiabilityNotelogic = new LiabilityNoteLogic();
            if (LiabilityTypeID != null && LiabilityTypeID != "" && LiabilityTypeID != "00000000-0000-0000-0000-000000000000")
            {
                ListLiabilityFundingScheduleDataContract = LiabilityNotelogic.GetLiabilityFundingScheduleByLiabilityTypeID(LiabilityTypeID);

               
            }

            try
            {
                List<LookupDataContract> lstLookups = LiabilityNotelogic.GetTransactionTypesLookupForJournalEntry();

                if (ListLiabilityFundingScheduleDataContract != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        ListLiabilityFundingSchedule = ListLiabilityFundingScheduleDataContract,
                        lstLookups= lstLookups
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
                Log.WriteLogException(CRESEnums.Module.LiabilityNote.ToString(), "Error in GetLiabilityFundingScheduleByLiabilityTypeID: Note ID " + LiabilityTypeID, LiabilityTypeID, headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);
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
        [Route("api/liabilityNote/GetLiabilityRateSpreadScheduleByNoteAccountID")]
        public IActionResult GetLiabilityRateSpreadScheduleByNoteAccountID([FromBody] string LiabilityAccountID)
        {

            GenericResult _authenticationResult = null;
            List<LiabilityRateSpreadDataContract> LiabilityRate = new List<LiabilityRateSpreadDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            List<ScheduleEffectiveDateLiabilityDataContract> ListEffectiveDateCount = new List<ScheduleEffectiveDateLiabilityDataContract>();

            LiabilityNoteLogic LiabilityNotelogic = new LiabilityNoteLogic();
            LiabilityRate.Add(new LiabilityRateSpreadDataContract());
            LiabilityRate = LiabilityNotelogic.GetLiabilityRateSpreadScheduleByNoteAccountID(LiabilityAccountID.ToString());
            ListEffectiveDateCount = LiabilityNotelogic.GetScheduleEffectiveDateCount(new Guid(LiabilityAccountID));

            if (LiabilityRate == null)
            {
                LiabilityRateSpreadDataContract r1 = new LiabilityRateSpreadDataContract();
                r1.Value = 0;
                LiabilityRate.Add(r1);
            }
            try
            {
                if (LiabilityRate != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        ListLiabilityRate = LiabilityRate,
                        ListEffectiveDateCount = ListEffectiveDateCount
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
                Log.WriteLogException(CRESEnums.Module.LiabilityNote.ToString(), "Error in GetLiabilityRateSpreadScheduleByNoteAccountID: Note ID ", LiabilityAccountID, headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/liabilityNote/GetHistoricalDataOfModuleByAccountId_Liability")]
        public IActionResult GetHistoricalDataOfModuleByAccountId_Liability([FromBody] LiabilityNoteDataContract _noteDC, int? pageIndex, int? pageSize)
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

                DataTable dtGeneralSetupDetailsLiabilityNote = new DataTable();
                DataTable dtRateSpreadSchedule = new DataTable();
                switch (modulename)
                {
                    case "GeneralSetupDetailsLiabilityNote":

                        dtGeneralSetupDetailsLiabilityNote = LiabilityNotelogic.GetHistoricalDataOfModuleByNoteId(_noteDC.LiabilityNoteAccountID, headerUserID, modulename);
                        if (dtGeneralSetupDetailsLiabilityNote.Rows.Count > 0) flag = true;

                        break;

                    case "RateSpreadSchedule":

                        dtRateSpreadSchedule = LiabilityNotelogic.GetHistoricalDataOfModuleByNoteId(_noteDC.LiabilityNoteAccountID, headerUserID, modulename);
                        if (dtRateSpreadSchedule.Rows.Count > 0) flag = true;

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
                        lstGeneralSetupDetailsLiabilityNote = dtGeneralSetupDetailsLiabilityNote,
                        lstRateSpreadSchedule = dtRateSpreadSchedule
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
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.LiabilityNote.ToString(), "Error in GetHistoricalDataOfModuleByAccountId_Liability: Note ID ", _noteDC.LiabilityNoteAccountID.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);
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
        [Route("api/liabilityNote/checkduplicateforliabilities")]
        public IActionResult CheckDuplicateforLiabilities([FromBody] LiabilityNoteDataContract _noteDC)
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
            Status = LiabilityNotelogic.CheckDuplicateforLiabilities(_noteDC.LiabilityNoteID, _noteDC.modulename, _noteDC.LiabilityNoteAccountID);
            if (Status == "True")
            {
                msg = "Liability Note " + _noteDC.LiabilityNoteID + " already exist. Please enter unique Liability Note ID.";

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
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/liabilityNote/GetTransactionEntryLiabilityNoteByDealAccountId")]
        public IActionResult GetTransactionEntryLiabilityNoteByDealAccountId([FromBody] string DealAccountID)
        {
            GenericResult _authenticationResult = null;
            DataTable dt = new DataTable();
            string AnalysisId = "c10f3372-0fc2-4861-a9f5-148f1f80804f";
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            LiabilityNoteLogic _Logic = new LiabilityNoteLogic();
            dt = _Logic.GetTransactionEntryLiabilityNoteByDealAccountId(DealAccountID, AnalysisId);
            try
            {


                if (dt != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        LiabilityCashFlow = dt
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
                Log.WriteLogException(CRESEnums.Module.LiabilityNote.ToString(), "Error in GetTransactionEntryLiabilityNoteByDealAccountId: Note ID ", DealAccountID, headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }

        [HttpPost]
        [Route("api/liabilityNote/getDealLiabilityCashflowsExportExcel")]
        public IActionResult GetDealLiabilityCashflowsExportExcel([FromBody] string DealAccountID)
        {
            GenericResult _authenticationResult = null;
            DataTable lstCashflowsExportData = new DataTable();
            var headerUserID = new Guid();
            string AnalysisId = "c10f3372-0fc2-4861-a9f5-148f1f80804f";

            try
            {
                if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                {
                    headerUserID = new Guid(Request.Headers["TokenUId"]);
                };

                LiabilityNoteLogic _Logic = new LiabilityNoteLogic();
                lstCashflowsExportData = _Logic.GetDealLiabilityCashflowsExportExcel(DealAccountID, AnalysisId);

                // Export to excel
                DataSet ds = new DataSet();
                lstCashflowsExportData.TableName = "Cashflow";
                ds.Tables.Add(lstCashflowsExportData);

                Stream stream = new StreamReader(_env.WebRootPath + "/ExcelTemplate/" + "DealLiabilityCashflowDownload.xlsx").BaseStream;
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


        public void InsertUpdateAIEntities(string entity_names, string userid, string actiontype, string originalname)
        {
            AIDynamicEntityUpdateLogic _dynamicentity = new AIDynamicEntityUpdateLogic();
            _dynamicentity.InsertUpdateAIEntitiesAsync("LiabilityNoteID", entity_names, userid, actiontype, originalname);

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


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/liabilityNote/getLiabilityFundingScheduleAggregateByLiabilityTypeID")]
        public IActionResult GetLiabilityFundingScheduleAggregateByLiabilityTypeID([FromBody] string LiabilityTypeID)
        {
            DebtLogic _DebtLogic = new DebtLogic();
            GenericResult _authenticationResult = null;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            DataTable dt = _DebtLogic.GetLiabilityFundingScheduleAggregateByLiabilityTypeID(LiabilityTypeID);

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
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.LiabilityNote.ToString(), "Error in GetLiabilityFundingScheduleByLiabilityTypeID: Note ID " + LiabilityTypeID, LiabilityTypeID, headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);
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
        [Route("api/liabilityNote/getLiabilityFundingScheduleDetailByLiabilityID")]
        public IActionResult GetLiabilityFundingScheduleDetailByLiabilityID([FromBody] string LiabilityTypeID)
        {
            DebtLogic _DebtLogic = new DebtLogic();
            GenericResult _authenticationResult = null;

            List<LiabilityFundingScheduleDataContract> ListLiabilityFundingSchedule = new List<LiabilityFundingScheduleDataContract>();
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            ListLiabilityFundingSchedule = _DebtLogic.GetLiabilityFundingScheduleDetailByLiabilityTypeID(LiabilityTypeID);

            try
            {

                if (ListLiabilityFundingSchedule != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        ListLiabilityFundingSchedule = ListLiabilityFundingSchedule

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
                Log.WriteLogException(CRESEnums.Module.LiabilityNote.ToString(), "Error in GetLiabilityFundingScheduleByLiabilityTypeID: Note ID " + LiabilityTypeID, LiabilityTypeID, headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);
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
