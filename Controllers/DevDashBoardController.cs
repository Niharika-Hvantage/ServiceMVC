
using CRES.BusinessLogic;
//using CRES.DAL;
using CRES.DataContract;
using CRES.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using OfficeOpenXml;
using System.IO;
using iTextSharp.tool.xml.html.table;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace CRES.Services.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class DevDashBoardController : ControllerBase
    {
        private IHostingEnvironment _env;

        public DevDashBoardController(IHostingEnvironment env)
        {
            _env = env;
        }

        private DevDashBoardLogic _DevDashBoardLogic = new DevDashBoardLogic();


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/devdash/GetcalculationStatus")]
        public IActionResult GetcalculationStatus([FromBody] string scenrioID)
        {
            GenericResult _authenticationResult = null;
            List<DevDashBoardDataContract> lstCalculationStatus = new List<DevDashBoardDataContract>();
            List<DevDashBoardDataContract> listuserrequstcount = new List<DevDashBoardDataContract>();
            List<DevDashBoardDataContract> listFastestandSlowest = new List<DevDashBoardDataContract>();

            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            DevDashBoardLogic devDlogic = new DevDashBoardLogic();
            lstCalculationStatus = devDlogic.GetCalculationStaus(new Guid(scenrioID));
            listuserrequstcount = devDlogic.UserRequestCount(new Guid(scenrioID));

            listFastestandSlowest = listFastestandSlowest.OrderByDescending(x => x.value).ToList();
            try
            {
                if (lstCalculationStatus != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        CalculationStatus = lstCalculationStatus,
                        UserRequestCount = listuserrequstcount,
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
        [Route("api/devdash/GetCalculationStatusForValuationDashBoard")]
        public IActionResult GetCalculationStatusForValuationDashBoard()
        {
            GenericResult _authenticationResult = null;
            List<DevDashBoardDataContract> lstCalculationStatus = new List<DevDashBoardDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            DevDashBoardLogic devDlogic = new DevDashBoardLogic();
            lstCalculationStatus = devDlogic.GetCalculationStatusForValuationDashBoard();
            try
            {
                if (lstCalculationStatus != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        CalculationStatus = lstCalculationStatus,
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
        [Route("api/devdash/GetFailedNotes")]
        public IActionResult GetFailedNotes([FromBody] string logtype)
        {
            GenericResult _authenticationResult = null;
            List<DevDashBoardDataContract> lstCalculationStatus = new List<DevDashBoardDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            DevDashBoardLogic devDlogic = new DevDashBoardLogic();
            if (logtype == "ServiceFailure")
            {
                logtype = "";
            }
            lstCalculationStatus = devDlogic.GetFailedNotes(logtype, new Guid(headerUserID));

            try
            {
                if (lstCalculationStatus != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Succeeded",
                        CalculationStatus = lstCalculationStatus
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
                string message = ExceptionHelper.GetFullMessage(ex);
                Logger.Write("Dash Board", "Error in loading: " + "" + " Exception : " + message, MessageLevel.Error, headerUserID, "");
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
        [Route("api/devdash/GetCalcJson")]
        public IActionResult GetCalcJson([FromBody] DevDashBoardDataContract devDashBoard)
        {
            NoteDataContract _noteCalculatorDC = new NoteDataContract();

            string noteid = "";
            string analysisID = "";
            GenericResult _authenticationResult = null;
            List<DevDashBoardDataContract> lstCalculationStatus = new List<DevDashBoardDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            DevDashBoardLogic devDlogic = new DevDashBoardLogic();

            DataTable dt = devDlogic.GetNoteIDAndAnalysisID(devDashBoard.NoteID);

            analysisID = devDashBoard.ScenarioID;
            if (dt.Rows.Count > 0)
            {
                noteid = dt.Rows[0]["NoteID"].ToString();
                NoteLogic nl = new NoteLogic();
                _noteCalculatorDC = nl.GetNoteAllDataForCalculatorByNoteId(noteid, new Guid(headerUserID), new Guid(analysisID), null, null);
            }

            try
            {
                if (_noteCalculatorDC != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        NoteData = _noteCalculatorDC
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
                Logger.Write("Dash Board", "Error in loading: " + "" + " Exception : " + message, MessageLevel.Error, headerUserID, "");
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
        [Route("api/devdash/GetCalcJsonV1")]
        public IActionResult GetCalcJsonV1([FromBody] DevDashBoardDataContract devDashBoard)
        {
            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            V1CalcLogic v1logic = new V1CalcLogic();

            dynamic objJsonResult = v1logic.GetDealCalcRequestData(devDashBoard.NoteID, 182, devDashBoard.ScenarioID, 775, false, true);

            try
            {
                if (objJsonResult != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        v1json = objJsonResult
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
                Logger.Write("Dash Board", "Error in loading: " + "" + " Exception : " + message, MessageLevel.Error, headerUserID, "");
                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }


        [HttpPost]
        //[Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/devdash/refreshdatawarehouse")]
        public IActionResult RefreshDataWarehouse([FromBody] string currenttime)
        {
            List<DevDashBoardDataContract> _lstAppTimeZone = new List<DevDashBoardDataContract>();
            IEnumerable<string> headerValues;

            NoteLogic _noteLogic = new NoteLogic();
            GenericResult _authenticationResult = null;
            DevDashBoardLogic devDlogic = new DevDashBoardLogic();
            string Message = "";
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            DataTable dt = devDlogic.GetDatabaseStatus(currenttime, new Guid(headerUserID));
            if (dt.Rows[0]["Status2"].ToString() != "Process Running")
            {
                Message = "Request to refresh data warehouse submitted successfully.";
                Thread FirstThread = new Thread(() => _noteLogic.importsourcetodw());
                FirstThread.Start();
            }
            else { Message = "Data warehouse process is already running."; }

            _authenticationResult = new GenericResult()
            {
                Succeeded = true,
                Message = Message
            };

            return Ok(_authenticationResult);
        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/devdash/geterrorlogcount")]
        public IActionResult GetErrorLogCount()
        {

            IEnumerable<string> headerValues;
            GenericResult _authenticationResult = null;
            DevDashBoardLogic devDlogic = new DevDashBoardLogic();
            List<DevDashBoardDataContract> lst = new List<DevDashBoardDataContract>();
            List<DevDashBoardDataContract> UserSummary = new List<DevDashBoardDataContract>();
            string Message = "";
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            DataTable dt = devDlogic.GetErrorLogs();
            foreach (DataRow row in dt.Rows)
            {
                DevDashBoardDataContract dbc = new DevDashBoardDataContract();
                dbc.Name = row["Category"].ToString();
                dbc.ProcessType = row["Category"].ToString();
                if (dbc.Name == "")
                {
                    dbc.Name = "ServiceFailure";

                }
                else if (dbc.Name.Contains("ReadAccountingReportTemplate"))
                {

                    dbc.Name = "Reporting";
                }
                dbc.value = Convert.ToInt32(row["CategoryCount"]);
                lst.Add(dbc);
            }
            DataTable dtuser = devDlogic.ShowUserSummary();
            foreach (DataRow row in dtuser.Rows)
            {
                DevDashBoardDataContract res = UserSummary.Find(x => x.Name == row["UserName"].ToString());
                if (res != null)
                {
                    foreach (var User in UserSummary)
                    {
                        if (User.Name == row["UserName"].ToString())
                        {
                            if (row["Type"].ToString() == "Deal")
                            {
                                User.DealCount = Convert.ToInt32(row["Count"]);
                            }
                            else if (row["Type"].ToString() == "Note")
                            {
                                User.NoteCount = Convert.ToInt32(row["Count"]);

                            }
                        }
                    }
                }
                else
                {
                    DevDashBoardDataContract dbc = new DevDashBoardDataContract();
                    dbc.Name = row["UserName"].ToString();
                    if (row["Type"].ToString() == "Deal")
                    {
                        dbc.DealCount = Convert.ToInt32(row["Count"]);
                        dbc.NoteCount = 0;
                    }
                    else if (row["Type"].ToString() == "Note")
                    {
                        dbc.NoteCount = Convert.ToInt32(row["Count"]);
                        dbc.DealCount = 0;
                    }
                    UserSummary.Add(dbc);
                }

            }
            DataTable calcSummary = devDlogic.GetCalculationSummary();
            //calcSummary = calcSummary.AsEnumerable()
            //    .Where(row =>
            //    {
            //        int running = Convert.ToInt32(row["Running"]);
            //        int processing = Convert.ToInt32(row["Processing"]);
            //        int failed = Convert.ToInt32(row["Failed"]);
            //        int completed = Convert.ToInt32(row["Completed"]);

            //        int total = running + processing + failed + completed;
            //        return total != 0;
            //    })
            //    .CopyToDataTable();

            _authenticationResult = new GenericResult()
            {
                Succeeded = true,
                Message = Message,
                UserRequestCount = lst,
                ResultList = UserSummary,
                CalcSummary = calcSummary,
            };

            return Ok(_authenticationResult);
        }


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/devdash/calculatemultiplenotes")]
        public IActionResult CalculateMultipleNotes([FromBody] DevDashBoardDataContract devDashBoard)
        {
            NoteLogic _noteLogic = new NoteLogic();
            GenericResult _authenticationResult = null;
            DevDashBoardLogic devDlogic = new DevDashBoardLogic();
            string Message = "";

            string noteids = devDashBoard.NoteID.Replace("\n", "");
            noteids = noteids.Replace("\t", "");
            devDlogic.CalculateMultipleNotes(noteids, devDashBoard.ScenarioID);

            _authenticationResult = new GenericResult()
            {
                Succeeded = true,
                Message = Message
            };

            return Ok(_authenticationResult);
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/devdash/calcallnotes")]
        public IActionResult CalcAllNotes([FromBody] string scenrioID)
        {
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            GenericResult _authenticationResult = null;
            DevDashBoardLogic devDlogic = new DevDashBoardLogic();
            devDlogic.CalcAllNotes(scenrioID, headerUserID.ToString());
            _authenticationResult = new GenericResult()
            {
                Succeeded = true
            };

            return Ok(_authenticationResult);
        }



        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/devdash/GetAIDashBoardData")]
        public IActionResult GetAIDashBoardData()
        {
            GenericResult _authenticationResult = null;
            List<DevDashBoardDataContract> lstAIDashboard = new List<DevDashBoardDataContract>();
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            DevDashBoardLogic devDlogic = new DevDashBoardLogic();
            lstAIDashboard = devDlogic.GetAIDashBoardData();
            try
            {
                if (lstAIDashboard != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstAIDashboard = lstAIDashboard

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
                Log.WriteLogException(CRESEnums.Module.AI_Assistant.ToString(), "Error occurred in GetAIDashBoardData ", "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/devdash/GetAIUserData")]
        public IActionResult GetAIUserData([FromBody] string username)
        {

            GenericResult _authenticationResult = null;
            List<DevDashBoardDataContract> lstAIDashboard = new List<DevDashBoardDataContract>();
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            DevDashBoardLogic devDlogic = new DevDashBoardLogic();
            lstAIDashboard = devDlogic.GetAIUserData(username);
            try
            {
                if (lstAIDashboard != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstAIDashboard = lstAIDashboard

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
                Log.WriteLogException(CRESEnums.Module.AI_Assistant.ToString(), "Error occurred in GetAIDashBoardData ", "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }

        [HttpGet]
        [Services.Controllers.DeflateCompression]
        [Route("api/devdash/importStagingdata")]
        public IActionResult ImportStagingData()
        {

            GenericResult _authenticationResult = null;
            DevDashBoardLogic devDlogic = new DevDashBoardLogic();
            string Message = "";


            AppConfigLogic appLogic = new AppConfigLogic();
            List<AppConfigDataContract> lstAppConfig = new List<AppConfigDataContract>();
            lstAppConfig = appLogic.GetAppConfigByKey(Guid.NewGuid(), "ImportStagingDataIntoIntegration");
            int IsImportEnable = Convert.ToInt32(lstAppConfig.FirstOrDefault().Value);


            string Status = devDlogic.GetImportStagingDataStatus();
            if (Status == "Not Completed")
            {
                Message = "Import staging data already in process.";
            }
            else
            {
                if (IsImportEnable == 0)
                {
                    Message = "The import flag is OFF, You have to enable that flag to import the staging data.";
                }
                else
                {

                    Message = "Request to import staging data submitted successfully.";

                    Thread thrd_ImportStagingData = new Thread(() => devDlogic.ImportStagingData());
                    thrd_ImportStagingData.Start();
                }
            }


            _authenticationResult = new GenericResult()
            {
                Succeeded = true,
                Message = Message
            };
            return Ok(_authenticationResult);
        }


        [HttpGet]
        [Services.Controllers.DeflateCompression]
        [Route("api/devdash/getstagingdataintointegrationstatus")]
        public IActionResult GetStagingDataIntoIntegrationStatus()
        {

            GenericResult _authenticationResult = null;
            DevDashBoardLogic devDlogic = new DevDashBoardLogic();

            DataTable ImportDataStatus = devDlogic.GetStagingDataIntoIntegrationStatus();

            _authenticationResult = new GenericResult()
            {
                Succeeded = true,
                ImportDataStatus = ImportDataStatus
            };
            return Ok(_authenticationResult);
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
        [Route("api/devdash/getLogsDownloadExcel")]
        public IActionResult DownloadLogsExcel([FromBody] string objectID)
        {
            try
            {
                DevDashBoardLogic devDlogic = new DevDashBoardLogic();

                DataTable dt = devDlogic.GetLogsForDownloadExcel(objectID);
                DataSet ds = new DataSet();
                dt.TableName = "Output";
                ds.Tables.Add(dt);

                Stream stream = new StreamReader(_env.WebRootPath + "/ExcelTemplate/" + "ErrorLogs.xlsx").BaseStream;
                MemoryStream ms = WriteDataToExcel(ds, stream);
                return File(ms, "application/octet-stream");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }


        [HttpGet]
        [Route("api/devdash/getfirsthalfdiscrepancysummarydevdash")]
        public IActionResult GetFirstHalfDiscrepancySummaryDevDash()
        {
            GenericResult _authenticationResult = null;
            DealLogic _dealLogic = new DealLogic();

            try
            {
                List<(string TableName, DataTable Table, int RowCount)> tables = new List<(string, DataTable, int)>();


                DataTable fundingTable = new DataTable();
                fundingTable = _dealLogic.GetDealNoteFundingDiscrepancy();
                DataTable exitTable = new DataTable();
                exitTable = _dealLogic.GetDiscrepancyForExitAndExtentionStripReceiveable();
                DataTable eligibleDealsTable = new DataTable();
                eligibleDealsTable = _dealLogic.GetDiscrepancyListOfDealForEnableAutoSpread();
                DataTable missingFinancingTable = new DataTable();
                missingFinancingTable = _dealLogic.GetDiscrepancyForFinancingSource();
                DataTable invoicesTable = new DataTable();
                invoicesTable = _dealLogic.GetInvoiceDiscrepancy();
                DataTable recordTable = new DataTable();
                recordTable = _dealLogic.GetDiscrepancyForWireConfirmed();
                DataTable backshopTable = new DataTable();
                backshopTable = _dealLogic.GetDiscrepancyForDuplicatePIK_InBackshop();

                tables.Add(("Funding", fundingTable, fundingTable.Rows.Count));
                tables.Add(("Exit / Extension Fee Stripping/ Receivable", exitTable, exitTable.Rows.Count));
                tables.Add(("Eligible Deals to Autospread", eligibleDealsTable, eligibleDealsTable.Rows.Count));
                tables.Add(("Notes with Missing Financing Source", missingFinancingTable, missingFinancingTable.Rows.Count));
                tables.Add(("Invoices Pending More Than 3 BDs", invoicesTable, invoicesTable.Rows.Count));
                tables.Add(("Record less than current date and not wire confirmed", recordTable, recordTable.Rows.Count));
                tables.Add(("Backshop duplicate PIKNC and PIKPP", backshopTable, backshopTable.Rows.Count));

                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    DataTablesList = tables
                };
            }
            catch
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = false
                };
            }

            return Ok(_authenticationResult);
        }


        [HttpGet]
        [Route("api/devdash/getsecondhalfdiscrepancysummarydevdash")]
        public IActionResult GetSecondHalfDiscrepancySummaryDevDash()
        {
            GenericResult _authenticationResult = null;
            DealLogic _dealLogic = new DealLogic();

            try
            {
                List<(string TableName, DataTable Table, int RowCount)> tables = new List<(string, DataTable, int)>();

                DataTable balanceTable = new DataTable();
                balanceTable = _dealLogic.GetDiscrepancyForNetIOTransaction();
                tables.Add(("Balance Not Zeroed Out", balanceTable, balanceTable.Rows.Count));

                DataTable exportTable = new DataTable();
                exportTable = _dealLogic.GetDiscrepancyForExportPaydown();
                tables.Add(("Export Paydown Between M61 and Backshop", exportTable, exportTable.Rows.Count));

                DataTable balanceMismatchTable = new DataTable();
                balanceMismatchTable = _dealLogic.GetDiscrepancyForBalanceM61VsBackshop();
                tables.Add(("Balance not matching between M61 and backshop", balanceMismatchTable, balanceMismatchTable.Rows.Count));

                DataTable adjCommitmentTable = new DataTable();
                adjCommitmentTable = _dealLogic.GetDiscrepancyForAdjCommitmentM61VsBackshop();
                tables.Add(("Adjusted Commitment not matching between M61 and backshop", adjCommitmentTable, adjCommitmentTable.Rows.Count));

                DataTable futureFundingTable = new DataTable();
                futureFundingTable = _dealLogic.GetDiscrepancyForTotalFFVsUnfundedCommitment();
                tables.Add(("Future funding not matching with Unfunded Commitment", futureFundingTable, futureFundingTable.Rows.Count));

                DataTable ffMismatchTable = new DataTable();
                ffMismatchTable = _dealLogic.GetDiscrepancyForFFBetweenM61andBackshop();
                tables.Add(("Future Funding Mismatch Between M61 and Backshop", ffMismatchTable, ffMismatchTable.Rows.Count));

                DataTable commitmentTable = new DataTable();
                commitmentTable = _dealLogic.GetDiscrepancyForCommitmentData();
                tables.Add(("Commitment", commitmentTable, commitmentTable.Rows.Count));

                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    DataTablesList2 = tables
                };
            }
            catch
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = false
                };
            }

            return Ok(_authenticationResult);
        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/devdash/GetXIRRSummaryfordevdash")]
        public IActionResult GetXIRRSummaryfordevdash()
        {
            GenericResult _authenticationResult = null;
            List<DevDashBoardDataContract> lstxirrStatus = new List<DevDashBoardDataContract>();

            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            DevDashBoardLogic devDlogic = new DevDashBoardLogic();
            lstxirrStatus = devDlogic.GetXIRRStatusSummaryfordevdash();
            try
            {
                if (lstxirrStatus != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        XIRRStatusSummary = lstxirrStatus
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
        [Services.Controllers.DeflateCompression]
        [Route("api/devdash/GetAzureVMStatus")]
        public IActionResult GetAzureVMStatus()
        {
            GenericResult _authenticationResult = null;
            string status = "";
            try
            {
                string ApiConstantUrl = "https://vm-valuation-powerfunction.azurewebsites.net/api/HttpTrigger1?code=Sp7EuZqNsJjp1dMIrQoxYuJiHyLURiw31Dhlm5WqVqo7AzFuCxsu_g==&resourcegroup=NONPROD-ACOREVALUATION&vm=NonProdExcelServer01&action=get";

                string Outputresponse = "";
                System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(10);
                    var res = client.GetAsync(ApiConstantUrl);
                    try
                    {
                        HttpResponseMessage response1 = res.Result.EnsureSuccessStatusCode();
                        if (response1.IsSuccessStatusCode)
                        {
                            Outputresponse = response1.Content.ReadAsStringAsync().Result;
                        }
                        JArray CalcResponse = JArray.Parse(Outputresponse);
                        DataTable dataTable = JsonConvert.DeserializeObject<DataTable>(JsonConvert.SerializeObject(CalcResponse));

                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "Authentication succeeded",
                            dt = dataTable
                        };
                    }


                    catch (Exception e)
                    {

                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = e.Message
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

            return Ok(_authenticationResult);
        }


    }
}