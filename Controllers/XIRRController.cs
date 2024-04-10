using System;
using System.Collections.Generic;
using System.Linq;
using CRES.DataContract;
using CRES.BusinessLogic;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;
using CRES.Utilities;
using System.Data;
using Microsoft.Graph;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Graph.Auth;
using CRES.ServicesNew.Controllers;
using Newtonsoft.Json.Linq;
using System.Web.Helpers;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using CRES.NoteCalculator;
using Amazon.SimpleWorkflow.Model;
using System.Threading;

namespace CRES.ServiceMVC.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class XIRRController : ControllerBase
    {
        private IHostingEnvironment _env;

        public IEmailNotification _iEmailNotification;
        public XIRRController(IEmailNotification iemailNotification, IHostingEnvironment env)
        {
            _iEmailNotification = iemailNotification;
            _env = env;
        }


        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/XIRR/getallLookup")]
        public IActionResult GetAllLookup()
        {

            string getAllLookup = "144,2";
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

            List<ScenarioUserMapDataContract> listscenario = new List<ScenarioUserMapDataContract>();
            ScenarioLogic scenarioLogic = new ScenarioLogic();
            listscenario = scenarioLogic.GetAllScenarioDistinct(headerUserID.ToString());

            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();

            List<LookupDataContract> ReferencingLookup = new List<LookupDataContract>();
            ReferencingLookup = tagXIRRLogic.GetXIRRReferencingDealLevelReturnLookup();

            List<TransactionTypesDataContract> transactionTypes = new List<TransactionTypesDataContract>();
            transactionTypes = tagXIRRLogic.GetAllTransactionTypesforXIRRConfig();

            try
            {
                if (lstlookupDC != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstLookups = lstlookupDC,
                        lstScenarioUserMap = listscenario,
                        lstReferencingDealLevelLookup = ReferencingLookup,
                        TTList = transactionTypes
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
                Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occured in GetAllLookup", "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/XIRR/getXIRRConfigs")]
        public IActionResult GetXIRRConfigs()
        {
            UserPermissionLogic upl = new UserPermissionLogic();
            GenericResult _authenticationResult = null;
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            var headerUserID = new Guid();
            string tags = "";
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            try
            {
                List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "XIRRDetail");

                DataTable dt = tagXIRRLogic.GetXIRRConfig();
                List<XIRRConfigFilterDataContract> lstFilters = new List<XIRRConfigFilterDataContract>();
                lstFilters = tagXIRRLogic.GetXIRRFilters();

                var distinctXIRRConfigIDs = dt.AsEnumerable()
               .Select(row => Convert.ToInt32(row["XIRRConfigID"]))
               .Distinct()
               .ToList();

                List<XIRRConfigDataContract> ListXIRRConfig = new List<XIRRConfigDataContract>();

                foreach (int xirrConfigID in distinctXIRRConfigIDs)
                {
                    XIRRConfigDataContract xirr = new XIRRConfigDataContract();
                    DataRow[] rows = dt.Select($"XIRRConfigID = {xirrConfigID}");

                    List<string> namelist = new List<string>();
                    tags = "";
                    foreach (DataRow row in rows)
                    {
                        xirr.RowNumber = CommonHelper.ToInt32_NotNullable(row["RowNumber"]);
                        xirr.XIRRConfigID = CommonHelper.ToInt32_NotNullable(row["XIRRConfigID"]);
                        xirr.ReturnName = row["ReturnName"].ToString();
                        xirr.AnalysisID = row["AnalysisID"].ToString();
                        xirr.AnalysisName = row["AnalysisName"].ToString();
                        //xirr.UpdatedBy = row["LastCalculatedBy"].ToString();
                        xirr.Status = row["Status"].ToString();
                        xirr.UpdatedDate = row["LastCalculated"].ToDateTime();
                        xirr.Comments = row["Comments"].ToString();
                        xirr.ErrorDetails = row["ErrorDetails"].ToString();
                        xirr.Group1 = CommonHelper.ToInt32(row["Group1"]);
                        xirr.Group2 = CommonHelper.ToInt32(row["Group2"]);
                        xirr.Type = row["Type"].ToString();
                        xirr.ArchivalRequirement = CommonHelper.ToInt32(row["ArchivalRequirement"]);
                        xirr.ReferencingDealLevelReturn = CommonHelper.ToInt32(row["ReferencingDealLevelReturn"]);
                        xirr.UpdateXIRRLinkedDeal = CommonHelper.ToInt32(row["UpdateXIRRLinkedDeal"]);
                        xirr.FileNameInput = Convert.ToString(row["FileNameInput"]);
                        xirr.FileNameOutput = Convert.ToString(row["FileNameOutput"]);
                        xirr.XIRRConfigGUID = Convert.ToString(row["XIRRConfigGUID"]);

                        xirr.ArchivalRequirementText = row["ArchivalRequirementText"].ToString();
                        xirr.UpdateXIRRLinkedDealText = row["UpdateXIRRLinkedDealText"].ToString();

                        int tagID = row["TagID"] != DBNull.Value ? Convert.ToInt32(row["TagID"]) : 0;
                        string tagName = row["TagName"] != DBNull.Value ? row["TagName"].ToString() : string.Empty;

                        int transactionID = row["TransactionID"] != DBNull.Value ? Convert.ToInt32(row["TransactionID"]) : 0;
                        string transactionName = row["TransactionName"] != DBNull.Value ? row["TransactionName"].ToString() : string.Empty;

                        namelist.Add(tagName);
                        //tags = tags + tagName + ",";
                    }
                    if (namelist != null && namelist.Count>0)
                    {
                        var distinctitems= namelist.Distinct().ToList();
                        foreach (var item in distinctitems)
                        {
                            tags = tags + item + ",";
                        }
                        xirr.Tags = tags.Remove(tags.Length - 1, 1);

                    }
                  
                    ListXIRRConfig.Add(xirr);
                }
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    dt = dt,
                    lstXirrConfig = ListXIRRConfig,
                    ListXirrFilters = lstFilters,
                    UserPermissionList = permissionlist
                };

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in GetAllXIRRConfigs", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/XIRR/CheckDuplicateXIRRConfig")]
        public IActionResult CheckDuplicateXIRRConfig([FromBody] XIRRConfigDataContract _xirrDC)
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

            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            Status = tagXIRRLogic.CheckDuplicateXIRRConfig(_xirrDC.XIRRConfigID, _xirrDC.ReturnName);
            if (Status == "True")
            {
                msg = "Return Name " + _xirrDC.ReturnName + " already exist. Please enter unique Return Name.";

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
        [Route("api/XIRR/saveXIRRConfigs")]
        public IActionResult SaveXIRRConfigs([FromBody] XIRRConfigDataContract xirrConfig)
        {
            GenericResult _genericResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            string createdealreturn = "";
            XIRRConfigDataContract dealreturn = new XIRRConfigDataContract();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            if (xirrConfig.XIRRConfigID == 0 && xirrConfig.XIRRConfigGUID == "00000000-0000-0000-0000-000000000000" && xirrConfig.Type == "Portfolio" && xirrConfig.ReferencingDealLevelReturn == null)
            {
                createdealreturn = "create";
                dealreturn = xirrConfig;
            }

            if (string.IsNullOrEmpty(xirrConfig.Type))
            {
                xirrConfig.Type = "Deal";
            }
            if (string.IsNullOrEmpty(xirrConfig.Comments))
            {
                xirrConfig.Comments = "";
            }
            if (xirrConfig.ReferencingDealLevelReturn == null)
            {
                xirrConfig.ReferencingDealLevelReturn = 0;
            }

            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            int xirrconfigid = tagXIRRLogic.InsertUpdateXIRRConfigs(xirrConfig, headerUserID);

            if (xirrConfig.ListXirrConfig != null)
            {
                foreach (var item in xirrConfig.ListXirrConfig)
                {
                    item.XIRRConfigID = xirrconfigid;
                }
            }

            foreach (var item in xirrConfig.ListXirrConfigFilter)
            {
                item.XIRRConfigID = xirrconfigid;
            }

            tagXIRRLogic.InsertUpdateXIRRConfigDetail(xirrConfig.ListXirrConfig, xirrConfig.ListXirrConfigFilter, headerUserID);

            if (createdealreturn != "")
            {
                SaveXIRRConfigsforDealLevelReturnCopy(dealreturn, headerUserID, xirrconfigid);
            }

            //call xirr for calc
            XIRRConfigParamDataContract config = new XIRRConfigParamDataContract();
            config.XIRRConfigIDs = xirrconfigid.ToString();
            Thread thirdThread = new Thread(() => {
                tagXIRRLogic.InsertXIRRCalculationInput(config, headerUserID.ToString());
                GenerateXIRRInptFiles(config);
            });
            thirdThread.Start();

            try
            {
                _genericResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Save/Updated Successfully!",
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in SaveXIRRConfigs", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);
        }

        public void SaveXIRRConfigsforDealLevelReturnCopy(XIRRConfigDataContract CopyxirrConfig, Guid headerUserID, int XIRRConfigIdPortfolio)
        {

            try
            {
                TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
                int? RefrencingDealLevel_XIRRConfigID;

                RefrencingDealLevel_XIRRConfigID = tagXIRRLogic.GetReferencingDealLevelReturnWithSameConfig(XIRRConfigIdPortfolio);

                if (RefrencingDealLevel_XIRRConfigID == null)
                {

                    CopyxirrConfig.ReturnName = CopyxirrConfig.ReturnName + "_Deal";
                    CopyxirrConfig.Type = "Deal";
                    CopyxirrConfig.Group1 = 0;
                    CopyxirrConfig.Group2 = 0;
                    CopyxirrConfig.isSystemGenerated = true;

                    if (string.IsNullOrEmpty(CopyxirrConfig.Comments))
                    {
                        CopyxirrConfig.Comments = "";
                    }

                    if (CopyxirrConfig.ReferencingDealLevelReturn == null)
                    {
                        CopyxirrConfig.ReferencingDealLevelReturn = 0;
                    }


                    int XIRRConfigIdDealCopy = tagXIRRLogic.InsertUpdateXIRRConfigs(CopyxirrConfig, headerUserID);

                    if (CopyxirrConfig.ListXirrConfig != null)
                    {
                        foreach (var item in CopyxirrConfig.ListXirrConfig)
                        {
                            item.XIRRConfigID = XIRRConfigIdDealCopy;
                        }
                    }

                    foreach (var item in CopyxirrConfig.ListXirrConfigFilter)
                    {
                        item.XIRRConfigID = XIRRConfigIdDealCopy;
                    }

                    tagXIRRLogic.InsertUpdateXIRRConfigDetail(CopyxirrConfig.ListXirrConfig, CopyxirrConfig.ListXirrConfigFilter, headerUserID);

                    tagXIRRLogic.UpdateReferencingDealLevelReturnforPortfolioType(XIRRConfigIdDealCopy, XIRRConfigIdPortfolio);

                    //call xirr for calc
                    XIRRConfigParamDataContract config = new XIRRConfigParamDataContract();
                    config.XIRRConfigIDs = XIRRConfigIdDealCopy.ToString();
                    tagXIRRLogic.InsertXIRRCalculationInput(config, headerUserID.ToString());

                }

                if (RefrencingDealLevel_XIRRConfigID != null)
                {
                    tagXIRRLogic.UpdateReferencingDealLevelReturnforPortfolioType(RefrencingDealLevel_XIRRConfigID, XIRRConfigIdPortfolio);
                }

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in SaveXIRRConfigs", "", "", ex.TargetSite.Name.ToString(), "", ex);
            }
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/XIRR/getLookupforXIRRFilters")]
        public IActionResult GetLookupforXIRRFilters(int XIRRConfigID)
        {
            List<XIRRFiltersLookupDataContract> XIRRFilters = new List<XIRRFiltersLookupDataContract>();

            List<XIRRConfigFilterDataContract> lstfilterNames = new List<XIRRConfigFilterDataContract>();

            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            GenericResult _authenticationResult = null;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            try
            {

                XIRRFilters = tagXIRRLogic.GetLookupforXIRRFilters(XIRRConfigID);
                lstfilterNames = tagXIRRLogic.GetXirrFilterSetupNames();
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    XIRRFiltersLookup = XIRRFilters,
                    ListXirrFilters = lstfilterNames
                };


            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in GetLookupforXIRRFilters", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/XIRR/getallxirrtransactiontypes")]
        public IActionResult GetAllXIRRTransactionTypes()
        {
            List<TransactionTypesDataContract> transactionTypes = new List<TransactionTypesDataContract>();
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            GenericResult _authenticationResult = null;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            List<XIRRConfigDataContract> ListXIRRArchives = new List<XIRRConfigDataContract>();
            try
            {

                transactionTypes = tagXIRRLogic.GetAllTransactionTypesforXIRRConfig();
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    TTList = transactionTypes
                };


            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in GetAllXIRRTransactionTypes", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/XIRR/xirrcalc")]
        public IActionResult InsertXIRRCalculationInput([FromBody] XIRRConfigParamDataContract XIRRConfigParam)
        {
            GenericResult _genericResult = null;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();

            try
            {
                tagXIRRLogic.InsertXIRRCalculationInput(XIRRConfigParam, headerUserID.ToString());

                //generate portfolio level input file
                Thread FirstThread = new Thread(() => GenerateXIRRInptFiles(XIRRConfigParam));
                FirstThread.Start();

                _genericResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Data archived successfully.",
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in InsertXIRRCalculationInput", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);

        }

        public IActionResult GenerateXIRRInptFiles(int xirrConfigID)
        {

            if (xirrConfigID == 0)
            {
                xirrConfigID = 9;
            }
            GenericResult _genericResult = null;
            XIRRConfigDataContract xirrDc = new XIRRConfigDataContract();
            string currDate = DateTime.Now.ToString("MMddyyyyHHmmss");
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            try
            {
                xirrDc = tagXIRRLogic.GetXIRRConfigByID(xirrConfigID, "");

                if (xirrDc != null && xirrDc.XIRRConfigID > 0)
                {
                    //tagXIRRLogic.ArchiveXIRROutput(XIRRConfigParam, headerUserID.ToString());
                    DataTable dt = new DataTable();

                    ExceluploadController exc = new ExceluploadController();

                    //upload xirr input file
                    ReportFileDataContract reportDC = new ReportFileDataContract();
                    reportDC.ReportFileName = "XIRR_Input";
                    reportDC.ReportFileFormat = "xlsx";
                    reportDC.SourceStorageLocation = "XIRRTemplates";
                    reportDC.SourceStorageTypeID = 392;
                    reportDC.ReportFileTemplate = "XIRR_Input_PortfolioLevel" + "." + reportDC.ReportFileFormat;
                    reportDC.DestinationStorageTypeID = 392;
                    reportDC.DestinationStorageLocation = "XIRRInput";

                    //reportDC.NewFileName = reportDC.ReportFileName + "_" + currDate + "." + reportDC.ReportFileFormat;
                    reportDC.NewFileName = "Input_"+ xirrDc.XIRRConfigID+"_"+xirrDc.ReturnName + "_" + xirrDc.Name + "_" + xirrDc.Type + "_" + currDate + "." + reportDC.ReportFileFormat;

                    try
                    {
                        //upload xirr input file    
                        DataTable dtXIRR = new DataTable();
                        dtXIRR = tagXIRRLogic.GetXIRRInputByConfigID(xirrConfigID, "");
                        var result = exc.UploadXIRRFiles(reportDC, dtXIRR, false,2);
                    }
                    catch (Exception ex)
                    {
                        //delete files from blob

                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in GenerateXIRRInptFiles", "", "", ex.TargetSite.Name.ToString(), "", ex);

                        _genericResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = ex.Message
                        };
                    }
                }
                _genericResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Data uploaded successfully.",
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in GenerateXIRRInptFiles", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_genericResult);

        }


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/XIRR/deleteXIRRByXIRRConfigID")]
        public IActionResult DeleteXIRRByXIRRConfigID([FromBody] int deletedXIRRConfigID)
        {
            GenericResult _genericResult = null;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();

            try
            {
                tagXIRRLogic.DeleteXIRRByXIRRConfigID(deletedXIRRConfigID);
                _genericResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Data Deleted successfully.",
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in InsertXIRRCalculationInput", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);

        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/XIRR/archivexirrinputoutput")]
        public IActionResult ArchiveXIRRInputOutput([FromBody] XIRRConfigParamDataContract XIRRConfigParam)
        {
            GenericResult _genericResult = null;

            try
            {
                var headerUserID = new Guid();
                if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                {
                    headerUserID = new Guid(Request.Headers["TokenUId"]);
                }

                Thread FirstThread = new Thread(() => ArchiveXIRROutput(XIRRConfigParam, headerUserID.ToString()));
                FirstThread.Start();

                _genericResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Data archived successfully.",
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in ArchiveXIRRInputOutput", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);

        }
        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/XIRR/archivexirroutput")]
        public IActionResult ArchiveXIRROutput(XIRRConfigParamDataContract XIRRConfigParam,string userID)
        {
            GenericResult _genericResult = null;
            
            XIRRConfigDataContract xirrDc = new XIRRConfigDataContract();
            var headerUserID = new Guid();
            string currTime = DateTime.Now.ToString("HHmmss");
            
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            DataTable dt = new DataTable();
            string FilenameInput = "";
            string FilenameOutput = "";

            ExceluploadController exc = new ExceluploadController();
            ReportFileDataContract reportDC = new ReportFileDataContract();
            reportDC.SourceStorageLocation = "XIRRTemplates";
            reportDC.ReportFileFormat = "xlsx";
            reportDC.SourceStorageTypeID = 392;
            reportDC.DestinationStorageTypeID = 392;


            foreach (string configid in XIRRConfigParam.XIRRConfigIDs.Split(","))
            {
                try
                {
                    int _configID = Convert.ToInt32(configid);
                    xirrDc = tagXIRRLogic.GetXIRRConfigByID(_configID, "");

                    try
                    {
                        //new code
                        //upload xirr input file    
                        reportDC.ReportFileTemplate = "XIRR_Input_PortfolioLevel_Archive" + "." + reportDC.ReportFileFormat;
                        reportDC.DestinationStorageLocation = "XIRRInputArchive";

                        reportDC.NewFileName = "Archive_Input_" + xirrDc.ReturnName + "_" + xirrDc.Name + "_" + xirrDc.Type + "_" + Convert.ToDateTime(XIRRConfigParam.ArchiveDate).ToString("MMddyyyy") + currTime + "." + reportDC.ReportFileFormat;
                        FilenameInput = reportDC.NewFileName;
                        DataTable dtXIRR = new DataTable();
                        dtXIRR = tagXIRRLogic.GetXIRRInputByConfigID(_configID, "");
                        var result = exc.UploadXIRRFiles(reportDC, dtXIRR, false,2);
                        tagXIRRLogic.UpdateXIRRInputOutputArchiveFiles(_configID, FilenameInput, "", Convert.ToDateTime(XIRRConfigParam.ArchiveDate), XIRRConfigParam.Comments, userID);
                        tagXIRRLogic.InsertXIRRDeleteBlobFiles(FilenameInput, reportDC.DestinationStorageLocation, "");
                    }
                    catch (Exception ex)
                    { 

                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred generating archive input file.Method-ArchiveXIRROutput", "", "", ex.TargetSite.Name.ToString(), "", ex);

                        _genericResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = ex.Message
                        };
                    }

                    try
                    {
                        //upload xirr out file
                        reportDC.DestinationStorageLocation = "XIRROutputArchive";
                        reportDC.NewFileName = "Archive_Output_" + xirrDc.ReturnName + "_" + xirrDc.Name + "_" + xirrDc.Type + "_" + Convert.ToDateTime(XIRRConfigParam.ArchiveDate).ToString("MMddyyyy") + currTime + "." + reportDC.ReportFileFormat;
                        FilenameOutput = reportDC.NewFileName;
                        if (xirrDc.Type == "Portfolio")
                        {
                            reportDC.ReportFileTemplate = "XIRR_Output_PortfolioLevel_Archive" + "." + reportDC.ReportFileFormat;
                            //reportDC.ReportFileTemplate = "Test" + "." + reportDC.ReportFileFormat;

                            DataSet dsXIRROut = new DataSet();
                            
                            DataTable dtP = tagXIRRLogic.GetXIRROutputPortfolioLevel(_configID, "");

                            if (dtP.Columns.IndexOf("XIRRConfigID") != -1)
                            {
                                dtP.Columns.Remove("XIRRConfigID");
                            }
                            if (dtP.Columns.IndexOf("XIRRReturnGroupID") != -1)
                            {
                                dtP.Columns.Remove("XIRRReturnGroupID");
                            }
                            dsXIRROut.Tables.Add(dtP);

                            DataTable dtD = tagXIRRLogic.GetXIRROutputDealLevel(_configID, "");

                            if (dtD.Columns.IndexOf("XIRRConfigID") != -1)
                            {
                                dtD.Columns.Remove("XIRRConfigID");
                            }
                            if (dtD.Columns.IndexOf("XIRRReturnGroupID") != -1)
                            {
                                dtD.Columns.Remove("XIRRReturnGroupID");
                            }
                            dsXIRROut.Tables.Add(dtD);

                            var resultOut = exc.UploadXIRRFiles(reportDC, dsXIRROut, true);
                            tagXIRRLogic.UpdateXIRRInputOutputArchiveFiles(_configID, "", FilenameOutput, Convert.ToDateTime(XIRRConfigParam.ArchiveDate), XIRRConfigParam.Comments, userID);
                            tagXIRRLogic.InsertXIRRDeleteBlobFiles(FilenameOutput, reportDC.DestinationStorageLocation, "");
                        }
                        else
                        {
                            reportDC.ReportFileTemplate = "XIRR_Output_DealLevel_Archive" + "." + reportDC.ReportFileFormat;
                            DataTable dtXIRROut = new DataTable();
                            dtXIRROut = tagXIRRLogic.GetXIRROutputDealLevel(_configID, "");

                            DataTable dtD = tagXIRRLogic.GetXIRROutputDealLevel(_configID, "");

                            if (dtXIRROut.Columns.IndexOf("XIRRConfigID") != -1)
                            {
                                dtXIRROut.Columns.Remove("XIRRConfigID");
                            }
                            if (dtXIRROut.Columns.IndexOf("XIRRReturnGroupID") != -1)
                            {
                                dtXIRROut.Columns.Remove("XIRRReturnGroupID");
                            }
                            var resultOut = exc.UploadXIRRFiles(reportDC, dtXIRROut, true,1);
                            tagXIRRLogic.UpdateXIRRInputOutputArchiveFiles(_configID, "", FilenameOutput, Convert.ToDateTime(XIRRConfigParam.ArchiveDate), XIRRConfigParam.Comments, "");
                            tagXIRRLogic.InsertXIRRDeleteBlobFiles(FilenameOutput, reportDC.DestinationStorageLocation, "");
                        }
                    }
                    catch (Exception ex)
                    {

                        LoggerLogic Log = new LoggerLogic();
                        Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in generating archive output file.Method-ArchiveXIRROutput", "", "", ex.TargetSite.Name.ToString(), "", ex);

                        _genericResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = ex.Message
                        };
                    }
                    //

                    _genericResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Data archived successfully.",
                    };
                }
                catch (Exception ex)
                {
                    LoggerLogic Log = new LoggerLogic();
                    Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in ArchiveXIRROutput", "", "", ex.TargetSite.Name.ToString(), "", ex);

                    _genericResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = ex.Message
                    };
                }
            }
            
            return Ok(_genericResult);

        }


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/XIRR/generatexirrinptfiles")]
        public IActionResult GenerateXIRRInptFiles([FromBody] XIRRConfigParamDataContract XIRRConfigParam)
        {
            GenericResult _genericResult = null;
            XIRRConfigDataContract xirrDc = new XIRRConfigDataContract();
            string currDate = DateTime.Now.ToString("MMddyyyyHHmmss");
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            foreach (string configid in XIRRConfigParam.XIRRConfigIDs.Split(","))
            {
                try
                {
                    DataTable dt = new DataTable();
                    ExceluploadController exc = new ExceluploadController();
                    //upload xirr input file
                    ReportFileDataContract reportDC = new ReportFileDataContract();
                    reportDC.ReportFileName = "XIRR_Input";
                    reportDC.ReportFileFormat = "xlsx";
                    reportDC.SourceStorageLocation = "XIRRTemplates";
                    reportDC.SourceStorageTypeID = 392;
                    reportDC.ReportFileTemplate = "XIRR_Input_PortfolioLevel" + "." + reportDC.ReportFileFormat;

                    reportDC.DestinationStorageTypeID = 392;
                    reportDC.DestinationStorageLocation = "XIRRInput";


                    xirrDc = tagXIRRLogic.GetXIRRConfigByID(Convert.ToInt32(configid), "");
                    //reportDC.NewFileName = reportDC.ReportFileName + "_" + currDate + "." + reportDC.ReportFileFormat;
                    reportDC.NewFileName = "Input_"+ xirrDc.XIRRConfigID+"_" + xirrDc.ReturnName + "_" + xirrDc.Name + "_" + xirrDc.Type + "_" + currDate + "." + reportDC.ReportFileFormat;

                    DataTable dtXIRR = new DataTable();
                    dtXIRR = tagXIRRLogic.GetXIRRInputByConfigID(Convert.ToInt32(configid), "");
                    var result = exc.UploadXIRRFiles(reportDC, dtXIRR);
                    tagXIRRLogic.UpdateXIRRInputFiles(Convert.ToInt32(configid), reportDC.NewFileName, "");
                    tagXIRRLogic.InsertXIRRDeleteBlobFiles(reportDC.NewFileName, reportDC.DestinationStorageLocation, "");

                    _genericResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Data uploaded successfully.",
                    };
                }
                catch (Exception ex)
                {
                    LoggerLogic Log = new LoggerLogic();
                    Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in UploadXIRRInptOutputToBlob", "", "", ex.TargetSite.Name.ToString(), "", ex);

                    _genericResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = ex.Message
                    };
                }
            }
            return Ok(_genericResult);

        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Route("api/XIRR/getAllArchiveXIRROutput")]
        public IActionResult GetAllArchiveXIRROutput()
        {
            GenericResult _authenticationResult = null;
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            List<XIRRArchiveDataContract> ListXIRRArchives = new List<XIRRArchiveDataContract>();


            try
            {
                ListXIRRArchives = tagXIRRLogic.GetAllArchiveXIRROutput(headerUserID);
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    lstArchivesXIRR = ListXIRRArchives
                };


            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in GetAllArchiveXIRROutput", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/XIRR/getAllNoteTags")]
        public IActionResult GetAllNoteTags()
        {
            GenericResult _authenticationResult = null;
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            DataTable dt = tagXIRRLogic.GetAllNoteTagsXIRR(headerUserID);


            try
            {
                if (dt.Rows.Count > 0)
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
                Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in GetAllNoteTags", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/XIRR/saveNoteTags")]
        public IActionResult SaveNoteTags([FromBody] DataTable dt)
        {
            GenericResult _genericResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            tagXIRRLogic.InsertUpdateNoteTagsXIRR(dt, headerUserID);
            try
            {
                _genericResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Save/Updated Successfully!",
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in SaveNoteTags", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/XIRR/deleteNoteTags")]
        public IActionResult DeleteNoteTags([FromBody] int? TagMasterXIRRID)
        {
            GenericResult _genericResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            tagXIRRLogic.deleteNoteTagsXIRR(TagMasterXIRRID, headerUserID);
            try
            {
                _genericResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Deleted Successfully!",
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in DeleteNoteTags", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/XIRR/GetViewAttachedNotes")]
        public IActionResult GetViewAttachedNotes([FromBody] int? TagMasterXIRRID)
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;

            var headerUserID = string.Empty;
            TagXIRRLogic TagXIRRLogic = new TagXIRRLogic();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            DataTable dt = new DataTable();
            try
            {
                dt = TagXIRRLogic.GetViewAttachedNotes(TagMasterXIRRID);
                int RowCount = dt.Rows.Count;
                if (dt.Rows.Count > 0)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Succeeded",
                        dt = dt,
                        RowCount = RowCount
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "No Data."
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
        [Route("api/XIRR/GetXIRROutputPortfolioLevel")]
        public IActionResult GetXIRROutputPortfolioLevel([FromBody] int XIRRConfigID)
        {
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            GenericResult _authenticationResult = null;
            DataTable dt = new DataTable();
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            try
            {

                dt = tagXIRRLogic.GetXIRROutputPortfolioLevel(XIRRConfigID, headerUserID.ToString());
                // dt = CreateDataTable();
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    dt = dt,

                };

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in GetLookupforXIRRFilters", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }

        [HttpPost]
        [Route("api/XIRR/GetXIRROutputDealLevelFromXirrDashBoard")]
        public IActionResult GetXIRROutputDealLevelFromXirrDashBoard([FromBody] dynamic jsonparameters)
        {
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            GenericResult _authenticationResult = null;
            DataTable dt = new DataTable();
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            try
            {
                int XIRRConfigID = Convert.ToInt16(jsonparameters[0]["XIRRConfigID"]);
                string GValue1 = Convert.ToString(jsonparameters[0]["GValue1"]);
                string GValue2 = Convert.ToString(jsonparameters[0]["GValue2"]);
                string LoanStatus = Convert.ToString(jsonparameters[0]["LoanStatus"]);
                string Type = Convert.ToString(jsonparameters[0]["Type"]);
                if (Type == "Deal")
                {
                    dt = tagXIRRLogic.GetXIRROutputDealLevelFromXirrDashBoard(XIRRConfigID, null, null, null);
                }
                else
                {
                    dt = tagXIRRLogic.GetXIRROutputDealLevelFromXirrDashBoard(XIRRConfigID, GValue1, GValue2, LoanStatus);
                }
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    dt = dt,

                };

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in GetXIRROutputDealLevelFromXirrDashBoard", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }

        [HttpPost]
        [Route("api/XIRR/GetExportExcelXIRROutputDealLevelFromXirrDashBoard")]
        public IActionResult GetExportExcelXIRROutputDealLevelFromXirrDashBoard([FromBody] dynamic jsonparameters)
        {
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            GenericResult _authenticationResult = null;
            DataTable dt = new DataTable();
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            try
            {
                int XIRRConfigID = Convert.ToInt16(jsonparameters[0]["XIRRConfigID"]);
                string GValue1 = Convert.ToString(jsonparameters[0]["GValue1"]);
                string GValue2 = Convert.ToString(jsonparameters[0]["GValue2"]);
                string LoanStatus = Convert.ToString(jsonparameters[0]["LoanStatus"]);
                string Type = Convert.ToString(jsonparameters[0]["Type"]);
                if (Type == "Deal")
                    dt = tagXIRRLogic.GetXIRROutputDealLevelFromXirrDashBoard(XIRRConfigID, null, null, null);
                else
                    dt = tagXIRRLogic.GetXIRROutputDealLevelFromXirrDashBoard(XIRRConfigID, GValue1, GValue2, LoanStatus);
                List<string> columnsToRemove = new List<string> { "XIRRConfigID", "XIRRReturnGroupID" };

                foreach (string columnName in columnsToRemove)
                {
                    if (dt.Columns.Contains(columnName))
                    {
                        dt.Columns.Remove(columnName);
                    }
                }

                // Export to excel
                DataSet ds = new DataSet();
                dt.TableName = "Deal_XIRR";
                ds.Tables.Add(dt);


                Stream stream = new StreamReader(_env.WebRootPath + "/ExcelTemplate/" + "GetXIRROutputDealLevelTemplate.xlsx").BaseStream;
                MemoryStream ms = WriteDataToExcel(ds, stream);
                return File(ms, "application/octet-stream");

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRRDownload.ToString(), "Error occurred in cashflow download ", "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
                return Ok(_authenticationResult);
            }

        }


        [HttpPost]
        [Route("api/XIRR/getAssociatedNotesData")]
        public IActionResult GetAssociatedNotesData([FromBody] int XIRRConfigID)
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

                TagXIRRLogic _Logic = new TagXIRRLogic();
                lstEquityCapitalContributionExportData = _Logic.GetXIRRFilterExtractData(XIRRConfigID);

                // Export to excel
                DataSet ds = new DataSet();
                lstEquityCapitalContributionExportData.TableName = "Transactions";
                ds.Tables.Add(lstEquityCapitalContributionExportData);

                Stream stream = new StreamReader(_env.WebRootPath + "/ExcelTemplate/" + "XirrAssociatedNotesData.xlsx").BaseStream;
                MemoryStream ms = WriteDataToExcel(ds, stream);
                return File(ms, "application/octet-stream");

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRRDownload.ToString(), "Error occurred in Capital Contribution download ", "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
                return Ok(_authenticationResult);
            }

        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/XIRR/getXIRRConfigByXIRRConfigGUID")]
        public IActionResult GetXIRRConfigByXIRRConfigGUID([FromBody] string XIRRConfigGUID)
        {
            //UserPermissionLogic upl = new UserPermissionLogic();
            GenericResult _authenticationResult = null;
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            var headerUserID = new Guid();
            int? XIRRConfigID = 0;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            try
            {
                List<XIRRConfigDataContract> ListXIRRConfig = new List<XIRRConfigDataContract>();
                List<XIRRConfigFilterDataContract> lstFilters = new List<XIRRConfigFilterDataContract>();
                //List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "XIRRDetail");

                if (XIRRConfigGUID == "00000000-0000-0000-0000-000000000000")
                {
                    XIRRConfigDataContract config = new XIRRConfigDataContract();
                    config.XIRRConfigGUID = "00000000-0000-0000-0000-000000000000";
                    config.XIRRConfigID = 0;
                    config.Group1 = 0;
                    config.Group2 = 0;
                    config.ArchivalRequirement = 0;
                    config.UpdateXIRRLinkedDeal = 0;

                    TagMasterXIRRDataContract tagMasterXIRRDataContract = new TagMasterXIRRDataContract();
                    tagMasterXIRRDataContract.TagMasterXIRRID = 0;
                    List<TagMasterXIRRDataContract> tagList = new List<TagMasterXIRRDataContract>();
                    config.ListTagMasterXIRRData = tagList;

                    List<TransactionTypesDataContract> transList = new List<TransactionTypesDataContract>();
                    TransactionTypesDataContract trans = new TransactionTypesDataContract();
                    trans.TransactionTypesID = 0;

                    config.ListTransactionTypesData = transList;

                    ListXIRRConfig.Add(config);
                    XIRRConfigFilterDataContract filter = new XIRRConfigFilterDataContract();
                    filter.XIRRConfigID = 0;
                    lstFilters.Add(filter);
                }
                else
                {
                    DataTable dt = tagXIRRLogic.GetXIRRConfigByXIRRConfigGUID(XIRRConfigGUID);

                    foreach (DataRow dr in dt.Rows)
                    {
                        XIRRConfigID = CommonHelper.ToInt32(dr["XIRRConfigID"]);
                    }
                    lstFilters = tagXIRRLogic.GetXIRRFiltersByXIRRConfigID(XIRRConfigID);

                    var distinctXIRRConfigIDs = dt.AsEnumerable()
                   .Select(row => Convert.ToInt32(row["XIRRConfigID"]))
                   .Distinct()
                   .ToList();



                    foreach (int xirrConfigID in distinctXIRRConfigIDs)
                    {
                        XIRRConfigDataContract xirr = new XIRRConfigDataContract();

                        DataRow[] rows = dt.Select($"XIRRConfigID = {xirrConfigID}");

                        HashSet<int> distinctTagIDs = new HashSet<int>();
                        HashSet<int> distinctTransactionIDs = new HashSet<int>();

                        List<TagMasterXIRRDataContract> tagList = new List<TagMasterXIRRDataContract>();
                        List<TransactionTypesDataContract> transList = new List<TransactionTypesDataContract>();

                        foreach (DataRow row in rows)
                        {
                            xirr.RowNumber = CommonHelper.ToInt32_NotNullable(row["RowNumber"]);
                            xirr.XIRRConfigID = CommonHelper.ToInt32_NotNullable(row["XIRRConfigID"]);
                            xirr.ReturnName = row["ReturnName"].ToString();
                            xirr.AnalysisID = row["AnalysisID"].ToString();
                            //xirr.UpdatedBy = row["LastCalculatedBy"].ToString();
                            xirr.Status = row["Status"].ToString();
                            xirr.UpdatedDate = row["LastCalculated"].ToDateTime();
                            xirr.Comments = row["Comments"].ToString();
                            xirr.ErrorDetails = row["ErrorDetails"].ToString();
                            xirr.Group1 = CommonHelper.ToInt32_NotNullable(row["Group1"]);
                            xirr.Group2 = CommonHelper.ToInt32_NotNullable(row["Group2"]);
                            xirr.Type = row["Type"].ToString();
                            xirr.ArchivalRequirement = CommonHelper.ToInt32_NotNullable(row["ArchivalRequirement"]);
                            xirr.ReferencingDealLevelReturn = CommonHelper.ToInt32(row["ReferencingDealLevelReturn"]);
                            xirr.UpdateXIRRLinkedDeal = CommonHelper.ToInt32_NotNullable(row["UpdateXIRRLinkedDeal"]);
                            xirr.FileNameInput = Convert.ToString(row["FileNameInput"]);
                            xirr.FileNameOutput = Convert.ToString(row["FileNameOutput"]);
                            xirr.XIRRConfigGUID = Convert.ToString(row["XIRRConfigGUID"]);

                            int tagID = row["TagID"] != DBNull.Value ? Convert.ToInt32(row["TagID"]) : 0;
                            string tagName = row["TagName"] != DBNull.Value ? row["TagName"].ToString() : string.Empty;

                            int transactionID = row["TransactionID"] != DBNull.Value ? Convert.ToInt32(row["TransactionID"]) : 0;
                            string transactionName = row["TransactionName"] != DBNull.Value ? row["TransactionName"].ToString() : string.Empty;


                            if (!distinctTagIDs.Contains(tagID) && tagID != 0)
                            {
                                tagList.Add(new TagMasterXIRRDataContract
                                {
                                    TagMasterXIRRID = tagID,
                                    Name = tagName
                                });
                                distinctTagIDs.Add(tagID);
                            }

                            if (!distinctTransactionIDs.Contains(transactionID))
                            {
                                transList.Add(new TransactionTypesDataContract
                                {
                                    TransactionTypesID = transactionID,
                                    TransactionName = transactionName
                                });
                                distinctTransactionIDs.Add(transactionID);
                            }

                        }

                        xirr.ListTagMasterXIRRData = tagList;
                        xirr.ListTransactionTypesData = transList;

                        ListXIRRConfig.Add(xirr);
                    }
                }

                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    lstXirrConfig = ListXIRRConfig,
                    ListXirrFilters = lstFilters,

                };

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in GetAllXIRRConfigs", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }
        public DataTable CreateDataTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("ReturnName");
            dt.Columns.Add("XIRRConfigID");
            dt.Columns.Add("LoanStatus");
            dt.Columns.Add("Y_Axis");
            dt.Columns.Add("CA");
            dt.Columns.Add("NY");

            DataRow rmport = dt.NewRow();
            rmport["ReturnName"] = "Do_Not_Change_Portfolio";
            rmport["XIRRConfigID"] = 9;
            rmport["Y_Axis"] = "Delphi I";
            rmport["LoanStatus"] = "Realized";
            rmport["CA"] = 0.070631003;
            rmport["NY"] = 0.066184943073927;
            dt.Rows.Add(rmport);


            DataRow r1 = dt.NewRow();
            r1["ReturnName"] = "Do_Not_Change_Portfolio";
            r1["XIRRConfigID"] = 9;
            r1["Y_Axis"] = "Delphi I";
            r1["LoanStatus"] = "Unrealized";
            r1["CA"] = 0.070631003;
            r1["NY"] = 0.066184943073927;
            dt.Rows.Add(r1);


            DataRow r2 = dt.NewRow();
            r2["ReturnName"] = "Do_Not_Change_Portfolio";
            r2["XIRRConfigID"] = 9;
            r2["Y_Axis"] = "Delphi II";
            r2["LoanStatus"] = "Unrealized";
            r2["CA"] = 0.070631003;
            r2["NY"] = 0.066184943073927;
            dt.Rows.Add(r2);

            DataRow r3 = dt.NewRow();
            r3["ReturnName"] = "Do_Not_Change_Portfolio";
            r3["XIRRConfigID"] = 9;
            r3["Y_Axis"] = "Delphi II";
            r3["LoanStatus"] = "Realized";
            r3["CA"] = 0.070631003;
            r3["NY"] = 0.066184943073927;
            dt.Rows.Add(r3);

            return dt;
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
                Log.WriteLogException(CRESEnums.Module.XIRRDownload.ToString(), "Error detail(method: ReadAndUploadTemplateFile) :  ", "", "", ex.TargetSite.Name.ToString(), "", ex);

                throw ex;
            }
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/XIRR/downloadxirroutputfiles")]
        public async Task<IActionResult> DownloadXIRROutputFiles([FromBody] XIRRConfigParamDataContract XIRRConfigParam)
        {
            GenericResult _genericResult = null;
            XIRRConfigDataContract xirrDc = new XIRRConfigDataContract();
            string currDate = DateTime.Now.ToString("MMddyyyy");
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            foreach (int configid in XIRRConfigParam.XIRRConfigIDs)
            {
                try
                {
                    //tagXIRRLogic.ArchiveXIRROutput(XIRRConfigParam, headerUserID.ToString());
                    DataTable dt = new DataTable();

                    ExceluploadController exc = new ExceluploadController();

                    //upload xirr input file
                    ReportFileDataContract reportDC = new ReportFileDataContract();
                    reportDC.ReportFileName = "XIRR_Input";
                    reportDC.ReportFileFormat = "xlsx";
                    reportDC.SourceStorageLocation = "XIRRTemplates";
                    reportDC.SourceStorageTypeID = 392;
                    reportDC.ReportFileTemplate = "XIRR_Input" + "." + reportDC.ReportFileFormat;

                    reportDC.DestinationStorageTypeID = 392;
                    reportDC.DestinationStorageLocation = "XIRROutput";


                    xirrDc = tagXIRRLogic.GetXIRRConfigByID(configid, "");
                    //reportDC.NewFileName = reportDC.ReportFileName + "_" + currDate + "." + reportDC.ReportFileFormat;
                    reportDC.NewFileName = xirrDc.ReturnName + "_" + xirrDc.Name + "_" + xirrDc.Type + "_" + currDate + "." + reportDC.ReportFileFormat;

                    DataTable dtXIRR = new DataTable();
                    dtXIRR = tagXIRRLogic.GetXIRRInputByConfigID(configid, "");
                    var result = await exc.UploadXIRRFiles(reportDC, dtXIRR);


                    _genericResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Data uploaded successfully.",
                    };
                }
                catch (Exception ex)
                {
                    LoggerLogic Log = new LoggerLogic();
                    Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in UploadXIRRInptOutputToBlob", "", "", ex.TargetSite.Name.ToString(), "", ex);

                    _genericResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = ex.Message
                    };
                }
            }
            return Ok(_genericResult);

        }
        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/XIRR/GetFileNameForCashflow")]

        public IActionResult GetFileNameForCashflow([FromBody] XIRRReturnGroupDataContract retDC)
        {
            GenericResult _genericResult = null;
            var headerUserID = new Guid();
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            XIRRReturnGroupDataContract retDCOut = new XIRRReturnGroupDataContract();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            try
            {
                retDCOut = tagXIRRLogic.GetFileNameForCashflow(retDC, "");
                _genericResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Data uploaded successfully.",
                    XIRRReturnGroupDC = retDCOut
                };

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRR.ToString(), "Error occurred in GetFileNameForCashflow", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_genericResult);
        }



    }
}
