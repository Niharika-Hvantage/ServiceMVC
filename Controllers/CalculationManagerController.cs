using CRES.BusinessLogic;

using CRES.DataContract;
using CRES.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;


namespace CRES.Services.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class CalculationManagerController : ControllerBase
    {
        private IHostingEnvironment _env;

        public CalculationManagerController(IHostingEnvironment env)
        {
            _env = env;
        }

        IConfigurationSection Sectionroot = null;
        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/calculation/loadcalculationstatus")]
        public IActionResult LoadCalculationStatus([FromBody] CalculationManagerDataContract DCcalc)
        {

            GenericResult _authenticationResult = null;
            List<CalculationManagerDataContract> lstcalculationstatus = new List<CalculationManagerDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            UserPermissionLogic upl = new UserPermissionLogic();
            //to get user 
            List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "CalculationManager");
            if (permissionlist != null && permissionlist.Count > 0)
            {

                CalculationManagerLogic calculationlogic = new CalculationManagerLogic();
                lstcalculationstatus = calculationlogic.RefreshcalculationStatus(DCcalc, headerUserID);
            }
            try
            {
                if (lstcalculationstatus != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstCalculationManger = lstcalculationstatus,
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
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.CalculationManager.ToString(), "Error occurred in LoadCalculationStatus on calculation manager", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/calculation/insertcalculateonserverrequest")]
        public IActionResult InsertCalculateOnServerRequest([FromBody] List<CalculationManagerDataContract> nlist)
        {
            GetConfigSetting();
            GenericResult _authenticationResult = null;

            List<CalculationManagerDataContract> lstcalculationstatus = new List<CalculationManagerDataContract>();
            bool status = true;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            string envname = Sectionroot.GetSection("ApplicationName").Value;
            string PriorityText = "";
            if (nlist != null)
            {
                if (nlist.Count > 10)
                {
                    PriorityText = "Batch";
                }
                else
                { PriorityText = "Real Time"; }
            }
            foreach (CalculationManagerDataContract cdc in nlist)
            {
                cdc.StatusText = "Processing";
                cdc.UserName = headerUserID.ToString();
                cdc.ApplicationText = envname;
                cdc.PriorityText = PriorityText;
                cdc.CalcType = 775;
            }
            try
            {
                CalculationManagerLogic calculationlogic = new CalculationManagerLogic();
                status = calculationlogic.QueueNotesForCalculation(nlist, headerUserID.ToString());

                //not required will remove after next release 3.8 on 5/26/2023
                //CalculateNoteUsingM61V1Redesign(nlist);
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Request submitted successfully",
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.CalculationManager.ToString(), "Error occurred in LoadCalculationStatus on calculation manager", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }


        private void CalculateNoteUsingM61V1Redesign(List<CalculationManagerDataContract> notelist)
        {
            GetConfigSetting();
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            DealLogic dealLogic = new DealLogic();
            // var Enablem61Calculation = Sectionroot.GetSection("Enablem61Calculation").Value;
            var Enablem61Calculation = getEnablem61CalculationValue(headerUserID.ToString());
            if (Enablem61Calculation == "true")
            {
                V1CalcLogic v1logic = new V1CalcLogic();
                foreach (var item in notelist)
                {
                    v1logic.SubmitCalcRequest(item.NoteId, 182, item.AnalysisID.ToString(), 775, false, "");
                }
            }
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/calculation/getcalculationstatus")]
        public IActionResult GetCalculationStatus([FromBody] string lstcalcmgrDC)
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            CalculationManagerLogic _calclogic = new CalculationManagerLogic();
            int result = _calclogic.GetCalculationStatus();
            try
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    TotalCount = result
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.CalculationManager.ToString(), "Error occurred in GetCalculationStatus on calculation manager", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/calculation/getallcalcstatus")]
        public IActionResult GetAllcalcstatus()
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();


            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            CalculationManagerLogic _calclogic = new CalculationManagerLogic();
            int result = _calclogic.GetCalculationStatus();
            try
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    TotalCount = result
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.CalculationManager.ToString(), "Error occurred in GetAllcalcstatus on calculation manager", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/calculation/getallexceptions")]
        public IActionResult GetAllExceptions([FromBody] string name, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            //   List<CalculationManagerDataContract> lstcalculationstatus = new List<CalculationManagerDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            ExceptionsLogic el = new ExceptionsLogic();
            int? totalCount;
            List<ExceptionDataContract> listexceptions = el.GetAllExceptionsList(name, pageSize, pageIndex, out totalCount);

            try
            {
                if (listexceptions != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        Allexceptionslist = listexceptions,
                        TotalCount = totalCount//listexceptions.Count
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication Failed",
                        TotalCount = 0
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.CalculationManager.ToString(), "Error occurred in GetAllcalcstatus on calculation manager", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/calculation/getbatchcalculationLog")]
        public IActionResult GetBatchCalculationLog([FromBody] CalculationManagerDataContract DCcalc)
        {
            GenericResult _authenticationResult = null;
            List<BatchCalculationMasterDataContract> lstBatchlog = new List<BatchCalculationMasterDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            CalculationManagerLogic calculationlogic = new CalculationManagerLogic();
            DCcalc.UserName = headerUserID.ToString();
            lstBatchlog = calculationlogic.GetBatchCalculationLog(DCcalc);

            try
            {
                if (lstBatchlog != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstBatchCalculationMaster = lstBatchlog,
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
                Log.WriteLogException(CRESEnums.Module.CalculationManager.ToString(), "Error occurred in GetAllcalcstatus on calculation manager", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_authenticationResult);
        }


        //[Services.Controllers.DeflateCompression]
        //[Services.Controllers.IsAuthenticate]
        [HttpPost]
        [Route("api/calculation/downloadfilecalcoutput")]
        public IActionResult DownloadFileCalcOutput([FromBody] CalculationManagerDataContract _Calclist)
        {
            GenericResult _authenticationResult = null;
            CalculationManagerLogic calcMgr = new CalculationManagerLogic();
            CalculatorOutputJsonInfoDataContract _objCalcOutput = new CalculatorOutputJsonInfoDataContract();

            DataTable dtNotePeriodicOutputsDataContract = new DataTable();
            DataTable dtBalanceTab = new DataTable();
            DataTable dtCouponTab = new DataTable();
            DataTable dtPIKInterestTab = new DataTable();
            DataTable dtFinancingTab = new DataTable();
            DataTable dtRateTab = new DataTable();
            DataTable dtFeesTab = new DataTable();
            DataTable dtDatesTab = new DataTable();
            DataTable dtGAAPBasisTab = new DataTable();
            DataTable dtFeeOutputDataContract = new DataTable();
            DataTable dtFutureFundingScheduleTab = new DataTable();
            DataTable dtMaturityList = new DataTable();

            try
            {
                IEnumerable<string> headerValues;
                var headerUserID = new Guid();
                if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                {
                    headerUserID = new Guid(Request.Headers["TokenUId"]);
                }
                string JSON_Filename = _Calclist.FileName;

                if (_Calclist.CalcEngineType == 798)
                {
                   
                    dtNotePeriodicOutputsDataContract = GetJsonFile_V1(JSON_Filename);

                    DataSet ds = new DataSet();
                    dtNotePeriodicOutputsDataContract.TableName = "Output";
                    ds.Tables.Add(dtNotePeriodicOutputsDataContract);

                    Stream stream = new StreamReader(_env.WebRootPath + "/ExcelTemplate/" + "DebugData_V1_download.xlsx").BaseStream;
                    MemoryStream ms = WriteDataToExcel(ds, stream);
                    return File(ms, "application/octet-stream");
                }
                else
                {
                    CalculatorDebugData _calculatorDebugData = new CalculatorDebugData();
                    _calculatorDebugData = GetJsonFile(JSON_Filename);
                    DownloadFileCalcOutputHelperLogic _DownloadFileCalcOutputHelperLogic = new DownloadFileCalcOutputHelperLogic();

                    dtNotePeriodicOutputsDataContract = ObjToDataTable.ToDataTable(_calculatorDebugData.ListNotePeriodicOutput);
                    DataTable dtPeriodicOutput = _DownloadFileCalcOutputHelperLogic.FormatPeriodicOutputData(dtNotePeriodicOutputsDataContract);

                    dtRateTab = ObjToDataTable.ToDataTable(_calculatorDebugData.ListRateTab);
                    DataTable dtRatesTab = _DownloadFileCalcOutputHelperLogic.FormatRatesData(dtRateTab);

                    dtBalanceTab = ObjToDataTable.ToDataTable(_calculatorDebugData.ListBalanceTab);
                    DataTable dtBalance = _DownloadFileCalcOutputHelperLogic.FormatBalanceData(dtBalanceTab);


                    dtDatesTab = ObjToDataTable.ToDataTable(_calculatorDebugData.ListDatesTab);
                    DataTable dtDates = _DownloadFileCalcOutputHelperLogic.FormatDatesData(dtDatesTab);

                    dtFeesTab = ObjToDataTable.ToDataTable(_calculatorDebugData.ListFeesTab);
                    DataTable dtFees = _DownloadFileCalcOutputHelperLogic.FormatFeesData(dtFeesTab);

                    dtFeeOutputDataContract = ObjToDataTable.ToDataTable(_calculatorDebugData.ListFeeOutput);
                    DataTable dtFeeOutputData = _DownloadFileCalcOutputHelperLogic.FormatFeeOutputData(dtFeeOutputDataContract);


                    dtCouponTab = ObjToDataTable.ToDataTable(_calculatorDebugData.ListCouponTab);
                    DataTable dtCouponData = _DownloadFileCalcOutputHelperLogic.FormatCouponData(dtCouponTab);


                    dtGAAPBasisTab = ObjToDataTable.ToDataTable(_calculatorDebugData.ListGAAPBasisTab);
                    DataTable dtGAAPBasisData = _DownloadFileCalcOutputHelperLogic.FormatGaapData(dtGAAPBasisTab);


                    dtPIKInterestTab = ObjToDataTable.ToDataTable(_calculatorDebugData.ListPIKInterestTab);
                    DataTable dtPIKData = _DownloadFileCalcOutputHelperLogic.FormatPIkData(dtPIKInterestTab);



                    dtFutureFundingScheduleTab = ObjToDataTable.ToDataTable(_calculatorDebugData.ListFutureFundingScheduleTab);
                    DataTable dtFundingData = _DownloadFileCalcOutputHelperLogic.FormatFutureFundingScheduleData(dtFutureFundingScheduleTab);

                    //dtMaturityList = ObjToDataTable.ToDataTable(_calculatorDebugData.MaturityScenariosList);
                    //dtFinancingTab = ObjToDataTable.ToDataTable(_calculatorDebugData.ListFinancingTab);

                    //export to excel
                    DataSet ds = new DataSet();
                    dtPeriodicOutput.TableName = "PeriodicOutput";
                    ds.Tables.Add(dtPeriodicOutput);

                    dtRatesTab.TableName = "Rates";
                    ds.Tables.Add(dtRatesTab);

                    dtBalance.TableName = "Balance";
                    ds.Tables.Add(dtBalance);

                    dtDates.TableName = "Dates";
                    ds.Tables.Add(dtDates);

                    dtFees.TableName = "Fees";
                    ds.Tables.Add(dtFees);

                    dtFeeOutputData.TableName = "ListFeeOutput";
                    ds.Tables.Add(dtFeeOutputData);

                    dtCouponData.TableName = "Coupon";
                    ds.Tables.Add(dtCouponData);

                    dtGAAPBasisData.TableName = "GAAPBasis";
                    ds.Tables.Add(dtGAAPBasisData);

                    //PIK
                    dtPIKData.TableName = "PIK";
                    ds.Tables.Add(dtPIKData);

                    dtFundingData.TableName = "Fundings";
                    ds.Tables.Add(dtFundingData);




                    Stream stream = new StreamReader(_env.WebRootPath + "/ExcelTemplate/" + "DebugData_download.xlsx").BaseStream;
                    MemoryStream ms = WriteDataToExcel(ds, stream);
                    return File(ms, "application/octet-stream");
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.CalculationManager.ToString(), "Error occurred in DownloadFileCalcOutput on calculation manager", "", "", ex.TargetSite.Name.ToString(), "", ex);
                
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError, "InternalServerError");
               
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
                                //for (var k = 1; k <= worksheet.Dimension.End.Column; k++)
                                //{
                                //    worksheet.Column(k).AutoFit();

                                //}
                                //if (i == 0)
                                //{
                                //    worksheet.Column(3).Style.Numberformat.Format = "mm-dd-yyyy";

                                //    worksheet.Column(8).Style.Numberformat.Format = "mm-dd-yyyy";
                                //    worksheet.Column(9).Style.Numberformat.Format = "mm-dd-yyyy";
                                //    worksheet.Column(10).Style.Numberformat.Format = "mm-dd-yyyy";
                                //}
                                //if (i == 1)
                                //{
                                //    worksheet.Column(6).Style.Numberformat.Format = "mm-dd-yyyy";
                                //}
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

        public CalculatorDebugData GetJsonFile(string FileName)
        {
            GetConfigSetting();
            string json = "";
            CalculatorDebugData JFile = new CalculatorDebugData();
            try
            {
                MemoryStream memStreamDownloaded = new MemoryStream();
                var Container = Sectionroot.GetSection("storage:container:name").Value;
                CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);
                CloudBlobDirectory blobDirectory = container.GetDirectoryReference("CalcDebug");
                CloudBlockBlob blockBlob = blobDirectory.GetBlockBlobReference(FileName);
                blockBlob.DownloadToStream(memStreamDownloaded);

                MemoryStream memStream = new MemoryStream(memStreamDownloaded.ToArray());

                using (StreamReader r = new StreamReader(memStream))
                {
                    json = r.ReadToEnd();
                    JFile = JsonConvert.DeserializeObject<CalculatorDebugData>(json);
                }

                //string filepath = @"C:\Users\vishal.balapure\Desktop\JSON\resultjson.json";//AppDomain.CurrentDomain.BaseDirectory + @"\JSONFile\" + ServicerName + "_" + fileext.ToUpper() + ".json";
                //using (StreamReader r = new StreamReader(filepath))
                //{
                //    json = r.ReadToEnd();
                //    JFile = JsonConvert.DeserializeObject<CalculatorDebugData>(json);
                //}
            }
            catch (Exception ex)
            {
                string exc = ex.ToString();
            }
            return JFile;

        }

        public DataTable GetJsonFile_V1(string FileName)
        {
            GetConfigSetting();
            DataTable dt = new DataTable();
            try
            {
                MemoryStream memStreamDownloaded = new MemoryStream();
                var Container = Sectionroot.GetSection("storage:container:name").Value;
                CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);
                CloudBlobDirectory blobDirectory = container.GetDirectoryReference("CalcDebug");
                CloudBlockBlob blockBlob = blobDirectory.GetBlockBlobReference(FileName);
                blockBlob.DownloadToStream(memStreamDownloaded);

                MemoryStream memStream = new MemoryStream(memStreamDownloaded.ToArray());

                using (StreamReader r = new StreamReader(memStream))
                {
                    var periodoutput = JsonConvert.DeserializeObject<Jsonperiodicresponse>(r.ReadToEnd());
                    dt = ObjToDataTable.ConvertStringToDataTable(periodoutput.data);
                }
            }
            catch (Exception ex)
            {
                string exc = ex.ToString();
            }
            return dt;

        }
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

        [Services.Controllers.DeflateCompression]
        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/calculation/deletebatchcalculationrequestbyanalysisid")]
        public IActionResult DeleteBatchCalculationRequestByAnalysisID([FromBody] string analysisid)
        {
            string result;
            GenericResult _authenticationResult = null;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            try
            {
                CalculationManagerLogic _calclogic = new CalculationManagerLogic();
                GetConfigSetting();
                // var Enablem61Calculation = Sectionroot.GetSection("Enablem61Calculation").Value;
                var Enablem61Calculation = getEnablem61CalculationValue(headerUserID.ToString());
                if (Enablem61Calculation == "true")
                {

                    DataTable dt = _calclogic.CancelBatchRequestByAnalysisID(analysisid);
                    V1CalcLogic vc = new V1CalcLogic();
                    vc.CallBatchCancelAPI(dt);
                }
                else
                {
                    result = _calclogic.DeleteBatchCalculationRequestByAnalysisID(analysisid);
                }

                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded"
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.CalculationManager.ToString(), "Error occurred in DeleteBatchCalculationRequestByAnalysisID on calculation manager", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            //delete Notes inside 'CancelBatchRequestByAnalysisID' script 
            /*
            result = _calclogic.DeleteBatchCalculationRequestByAnalysisID(analysisid);
            try
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = result
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.CalculationManager.ToString(), "Error occurred in DeleteBatchCalculationRequestByAnalysisID on calculation manager", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            */
            return Ok(_authenticationResult);

        }

        [Services.Controllers.DeflateCompression]
        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Route("api/calculation/gettimecurrentoffset")]
        public IActionResult GetTimeZoneCurrentOffset()
        {
            GenericResult _authenticationResult = null;
            List<BatchCalculationMasterDataContract> lstBatchlog = new List<BatchCalculationMasterDataContract>();

            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            DataTable dt = new DataTable();
            CalculationManagerLogic calculationlogic = new CalculationManagerLogic();
            dt = calculationlogic.GetCurrentoffsetbyuserID(headerUserID);
            var currentoffset = Convert.ToString(dt.Rows[0]["currentoffset"]);

            try
            {
                if (currentoffset != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        currentoffset = currentoffset
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
        [Route("api/calculation/getTransactionCategory")]
        public IActionResult GetTransactionCategory()
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            DataTable dtCategory = new DataTable();
            DataTable dtGroup = new DataTable();
            CalculationManagerLogic calculationlogic = new CalculationManagerLogic();
            dtCategory = calculationlogic.GetTransactionCategory(headerUserID);
            dtGroup = calculationlogic.GetTransactionGroup(headerUserID);
            try
            {
                if (dtCategory.Rows.Count > 0 || dtGroup.Rows.Count > 0)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        dt = dtCategory,
                        dtGroup = dtGroup
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
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in GetTransactionCategory on calculation manager", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_authenticationResult);
        }

        public string getEnablem61CalculationValue(string headerUserID)
        {
            AppConfigLogic appl = new AppConfigLogic();
            //to get user 
            List<AppConfigDataContract> SettingKeyslist;
            string Enablem61Calculation = "";
            if (headerUserID == "")
                SettingKeyslist = appl.GetAppConfigByKey(null, "EnableM61Calculator");
            else
                SettingKeyslist = appl.GetAppConfigByKey(new Guid(headerUserID), "EnableM61Calculator");

            if (SettingKeyslist != null)
            {
                var Value = SettingKeyslist.FirstOrDefault().Value;
                if (Value == "1")
                {
                    Enablem61Calculation = "true";
                }
                else
                {
                    Enablem61Calculation = "false";
                }
            }
            return Enablem61Calculation;
        }

    }

}