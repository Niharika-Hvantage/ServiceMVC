using System;
using System.Collections.Generic;
using CRES.DataContract;
using CRES.BusinessLogic;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using CRES.Utilities;
using System.Data;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using CRES.DAL.Repository;
using CRES.Services;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Graph;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using static CRES.DataContract.V1CalcDataContract;
using System.Security.Policy;

namespace CRES.ServiceMVC.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class ChathamFinancialController : ControllerBase
    {
        public IEmailNotification _iEmailNotification;
        private IHostingEnvironment _env;
        public ChathamFinancialController(IEmailNotification iemailNotification, IHostingEnvironment env)
        {
            _iEmailNotification = iemailNotification;
            _env = env;
        }
        private string useridforSys_Scheduler = "3D6DB33D-2B3A-4415-991D-A3DA5CEB8B50";

        [HttpGet]
        [Route("api/ChathamFinancial/GetChathamFinancialDailyRate")]
        public IActionResult GetChathamFinancialDailyRate()
        {
            decimal? Currentrate = 0;
            DateTime? CurrentDate = DateTime.MinValue;
            string Outputresponse = "";
            string ApiConstantUrl = "";

            string error = "";
            string returnMessage = "";
            string copieddatamsg = "";
            v1GenericResult _authenticationResult = null;
            LoggerLogic Log = new LoggerLogic();

            DataTable dtimport = new DataTable();
            dtimport.Columns.Add("Date");
            dtimport.Columns.Add("Value");
            dtimport.Columns.Add("IndexType");

            DataTable dtcopy = new DataTable();

            try
            {
                Log.WriteLogInfo(CRESEnums.Module.DailyRatePull.ToString(), "Chatham Financial Daily Rate Api called ", "", useridforSys_Scheduler);


                ChathamFinancialLogic ChathamLogic = new ChathamFinancialLogic();
                DataTable dtconfig = ChathamLogic.GetChathamConfig("DailyRate");
                foreach (DataRow dr in dtconfig.Rows)
                {
                    ApiConstantUrl = dr["URL"].ToString() + dr["RatesCode"].ToString();
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
                                var CalcResponse = JObject.Parse(Outputresponse);

                                Currentrate = CommonHelper.StringToDecimal(CalcResponse["Rate"]);
                                CurrentDate = CommonHelper.ToDateTime(CalcResponse["CurrentDate"]);
                            }
                        }
                        catch (Exception e)
                        {
                            error = "Chatham Financial Error";
                            Outputresponse = "Chatham Financial Error :" + e.Message;
                        }
                    }

                    if (error == "")
                    {
                        DataTable dtindexes = new DataTable();
                        dtindexes.Columns.Add("Date");
                        dtindexes.Columns.Add("Name");
                        dtindexes.Columns.Add("Value");
                        dtindexes.Columns.Add("IndexesMasterGuid");

                        DataRow data1 = dtindexes.NewRow();
                        data1["Date"] = CurrentDate;

                        data1["Name"] = dr["IndexType"].ToString();

                        data1["Value"] = Currentrate / 100;
                        data1["IndexesMasterGuid"] = dr["IndexesMasterGuid"].ToString();
                        dtindexes.Rows.Add(data1);

                        //add data in import datatable for email
                        DataRow rmport = dtimport.NewRow();
                        rmport["Date"] = CurrentDate;

                        if (dr["IndexType"].ToString() == "SOFR")
                        {
                            rmport["IndexType"] = "Daily SOFR";
                        }
                        else
                        {
                            rmport["IndexType"] = dr["IndexType"].ToString();
                        }
                        rmport["Value"] = Currentrate / 100;
                        dtimport.Rows.Add(rmport);

                        //save data for index in table
                        IndexTypeRepository indexTypeRepository = new IndexTypeRepository();
                        indexTypeRepository.AddUpdateIndexList(dtindexes, useridforSys_Scheduler, useridforSys_Scheduler);

                        //Check Missing Data for sofr
                        dtcopy = indexTypeRepository.InsertUpdateMissingIndexList(dr["IndexesMasterGuid"].ToString(), Convert.ToInt16(dr["IndexTypeID"]), useridforSys_Scheduler);

                    }
                    else
                    {
                        Log.WriteLogExceptionMessage(CRESEnums.Module.DailyRatePull.ToString(), "Chatham Financial Daily Rate:" + Outputresponse, "", useridforSys_Scheduler, "GetChathamFinancialDailyRate", Outputresponse);
                        _authenticationResult = new v1GenericResult()
                        {
                            Status = Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError,
                            Succeeded = false,
                            Message = returnMessage,
                            ErrorDetails = Outputresponse
                        };

                        //_iEmailNotification.SendChathamFinancialDailyRateNotification("An error occurred in the daily SOFR pull from Chatham Financial. M61 systems team is looking for solution.", "Daily", "Fail");
                    }
                }

                //send email
                _iEmailNotification.SendChathamFinancialDailyRateNotificationSucces(dtimport, dtcopy, "Chatham Financial");
                _authenticationResult = new v1GenericResult()
                {
                    Status = Microsoft.AspNetCore.Http.StatusCodes.Status200OK,
                    Succeeded = true,
                    Message = returnMessage,
                    ErrorDetails = ""
                };
            }
            catch (Exception ex)
            {
                _authenticationResult = new v1GenericResult()
                {
                    Status = Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError,
                    Succeeded = false,
                    Message = "Error",
                    ErrorDetails = ex.Message
                };

                Log.WriteLogException(CRESEnums.Module.DailyRatePull.ToString(), "Error occured in GetChathamFinancialDailyRate ", "", useridforSys_Scheduler, ex.TargetSite.Name.ToString(), "", ex);
                //_iEmailNotification.SendChathamFinancialDailyRateNotification("An error occurred in the daily SOFR pull from Chatham Financial. M61 systems team is looking for solution.", "Daily", "Fail");
            }
            return Ok(_authenticationResult);
        }

        [HttpGet]
        [Route("api/ChathamFinancial/GetChathamFinancialForwardRateQuarterly")]
        public IActionResult GetChathamFinancialForwardRateQuarterly()
        {
            v1GenericResult _authenticationResult = null;
            LoggerLogic Log = new LoggerLogic();
            string Outputresponse = "";
            string ApiConstantUrl = "";
            string fwrateGuid = "";
            string error = "";
            try
            {
                Log.WriteLogInfo(CRESEnums.Module.DailyRatePull.ToString(), "Chatham Financial Quarterly Rate Api called ", "", useridforSys_Scheduler);
                //AppConfigLogic applogic = new AppConfigLogic();
                //List<AppConfigDataContract> configlist = applogic.GetAppConfigByKey(new Guid(useridforSys_Scheduler), "ChathamFinancialForwardRateQuarterly");
                
                ChathamFinancialLogic ChathamLogic = new ChathamFinancialLogic();

                DataTable dtconfig = ChathamLogic.GetChathamConfig("QuarterlyForwardRate");

                foreach (DataRow dr in dtconfig.Rows)
                {
                    ApiConstantUrl = dr["URL"].ToString() + dr["RatesCode"].ToString();

                    fwrateGuid = dr["IndexesMasterGuid"].ToString();
                    System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                    dynamic CalcResponse = null;
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
                                CalcResponse = JObject.Parse(Outputresponse);
                            }
                        }
                        catch (Exception e)
                        {
                            error = "Chatham Financial Error";
                            Outputresponse = "Chatham Financial Error :" + e.Message;
                        }
                    }
                    if (error == "")
                    {
                        DataTable dtexcel = new DataTable();
                        dtexcel.Columns.Add("Date", typeof(DateTime));
                        dtexcel.Columns.Add("1 Month Term SOFR Forward Curve", typeof(decimal));

                        DataTable dtindexes = new DataTable();
                        dtindexes.Columns.Add("Date");
                        dtindexes.Columns.Add("Name");
                        dtindexes.Columns.Add("Value");
                        dtindexes.Columns.Add("IndexesMasterGuid");

                        var ListRates = CalcResponse["Rates"];
                        if (ListRates != null)
                        {
                            for (var j = 0; j < ListRates.Count; j++)
                            {
                                DataRow frwrate = dtindexes.NewRow();
                                frwrate["Date"] = CommonHelper.ToDateTime(ListRates[j].Date);
                                frwrate["Value"] = CommonHelper.StringToDecimal(ListRates[j].Rate);
                                frwrate["Name"] = "1M Term SOFR";
                                frwrate["IndexesMasterGuid"] = fwrateGuid;
                                dtindexes.Rows.Add(frwrate);

                                DataRow frwrexcelate = dtexcel.NewRow();
                                frwrexcelate["Date"] = CommonHelper.ToDateTime(ListRates[j].Date);
                                frwrexcelate["1 Month Term SOFR Forward Curve"] = CommonHelper.StringToDecimal(ListRates[j].Rate);
                                dtexcel.Rows.Add(frwrexcelate);

                            }
                        }
                        IndexTypeRepository indexTypeRepository = new IndexTypeRepository();

                        indexTypeRepository.AddUpdateIndexList(dtindexes, useridforSys_Scheduler, useridforSys_Scheduler);
                        // update master 

                        IndexesMasterDataContract _indexesMasterDC = indexTypeRepository.GetIndexesMasterDetailByIndexesMaster(new Guid(fwrateGuid), useridforSys_Scheduler);
                        _indexesMasterDC.Description = "Forward Rate " + DateTime.Now.ToString("MM.dd.yyyy");
                        _indexesMasterDC.UpdatedBy = useridforSys_Scheduler;
                        _indexesMasterDC.UpdatedDate = DateTime.Now;

                        indexTypeRepository.InsertUpdateIndexesMasterDetail(_indexesMasterDC);

                        Log.WriteLogInfo(CRESEnums.Module.DailyRatePull.ToString(), "Chatham Financial Quarterly Rate Api data saved for Forward Rate ", "", useridforSys_Scheduler);

                        Thread FirstThread = new Thread(() => UploadChathamFinancialJson(Outputresponse));
                        FirstThread.Start();

                        string returnMessage = " Quarterly SOFR forward curve rates has been pulled successfully from Chatham Financial.";

                        MemoryStream ms = GetStreamfromDatatable(dtexcel);
                        string randomstring = DateTime.Now.ToString("MM_dd_yyyy");
                        _iEmailNotification.SendChathamFinancialQuarterlyForwardRateNotification(returnMessage, ms, "Chatham_One_Month_Term_SOFR_Forward_Curve_" + randomstring + ".xlsx");

                        _authenticationResult = new v1GenericResult()
                        {
                            Status = Microsoft.AspNetCore.Http.StatusCodes.Status200OK,
                            Succeeded = true,
                            Message = "Chatham Financial Quarterly Rate Api data saved for Forward Rate",
                            ErrorDetails = ""
                        };
                    }
                    else
                    {
                        Log.WriteLogExceptionMessage(CRESEnums.Module.DailyRatePull.ToString(), "Chatham Financial Quarterly Rate Api:" + Outputresponse, "", useridforSys_Scheduler, "GetChathamFinancialForwardRateQuarterly", Outputresponse);
                        _authenticationResult = new v1GenericResult()
                        {
                            Status = Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError,
                            Succeeded = false,
                            Message = "Error",
                            ErrorDetails = Outputresponse
                        };
                        _iEmailNotification.SendChathamFinancialDailyRateNotification("An error occurred in the Quarterly SOFR pull from Chatham Financial. M61 systems team is looking for solution.", "Quarterly", "Fail");
                    }
                }
               


            }
            catch (Exception ex)
            {

                Log.WriteLogException(CRESEnums.Module.DailyRatePull.ToString(), "Error occured in GetChathamFinancialForwardRateQuarterly ", "", useridforSys_Scheduler, ex.TargetSite.Name.ToString(), "", ex);
                _authenticationResult = new v1GenericResult()
                {
                    Status = Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError,
                    Succeeded = false,
                    Message = "Error",
                    ErrorDetails = ex.Message
                };
                _iEmailNotification.SendChathamFinancialDailyRateNotification("An error occurred in the Quarterly SOFR pull from Chatham Financial. M61 systems team is looking for solution.", "Quarterly", "Fail");
            }
            return Ok(_authenticationResult);

        }
        public void UploadChathamFinancialJson(string jsonStr)
        {
            LoggerLogic Log = new LoggerLogic();
            try
            {
                string filename = "Chatham_USForwardCurves_" + DateTime.Now.ToString("MM_dd_yyyy_HH_mm_ss") + ".json";
                var isJSONSaved = AzureStorageReadFile.UploadChathamFinancialJsonFileToAzureBlob(jsonStr, filename);
                if (isJSONSaved == false)
                {

                    Log.WriteLogExceptionMessage(CRESEnums.Module.DailyRatePull.ToString(), "Error occured in UploadChathamFinancialJson", "", "Error occured in UploadChathamFinancialJson", "", "");
                }
            }
            catch (Exception ex)
            {

                Log.WriteLogException(CRESEnums.Module.DailyRatePull.ToString(), "Error occured in Upload DealFunding Json for ", "", "", ex.TargetSite.Name.ToString(), "", ex);
            }

        }

        public MemoryStream GetStreamfromDatatable(DataTable dt)
        {
            dt.TableName = "Chatham_USForwardCurves";
            Stream ms = new MemoryStream();
            DataSet ds = new DataSet();
            ds.Tables.Add(dt);
            Stream stream = new StreamReader(_env.WebRootPath + "/ExcelTemplate/" + "Chatham_USForwardCurves.xlsx").BaseStream;
            ms = WriteDataToExcel.DataSetToExcel(ds, stream);
            return (MemoryStream)ms;
        }

        [HttpGet]
        [Route("api/CMEGroup/getCMEGroupSofrRate")]
        public IActionResult GetCMEGroupSofrRate()
        {
            string ApiConstantUrl = "https://www.cmegroup.com/services/sofr-strip-rates/?isProtected&_t=1698335626025";
            decimal? CurrentPrice = 0;
            JObject resultsStrip = new JObject();

            decimal? Overnight = 0;
            DateTime? CurrentDate = DateTime.MinValue;
            string CREIndexType = "";
            string Outputresponse = "";

            string error = "";
            string returnMessage = "";
            
            v1GenericResult _authenticationResult = null;
            LoggerLogic Log = new LoggerLogic();

            DataTable dtimport = new DataTable();
            dtimport.Columns.Add("Date");
            dtimport.Columns.Add("Value");
            dtimport.Columns.Add("IndexType");
            DataTable dtcopy = new DataTable();


            try
            {
                Log.WriteLogInfo(CRESEnums.Module.DailyRatePull.ToString(), "CME Group Daily Rate Api called ", "", useridforSys_Scheduler);


                ChathamFinancialLogic ChathamLogic = new ChathamFinancialLogic();
                DataTable dtconfig = ChathamLogic.GetChathamConfig("DailyRate");

                if (dtconfig != null && dtconfig.Rows.Count > 0)
                {
                    if (!string.IsNullOrEmpty(ApiConstantUrl))
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ApiConstantUrl);
                        request.Method = "GET";
                        request.ContentType = "application/json";
                        request.Headers.Add("Accept", "*/*");
                        request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
                        request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
                        request.Headers.Add("Connection", "keep-alive");
                        request.Headers.Add("Upgrade-Insecure-Requests", "keep-alive");
                        request.Headers.Add("Host", "www.cmegroup.com");
                        request.Headers.Add("Cache-Control", "max-age=0");
                        request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/119.0");
                        request.AutomaticDecompression = DecompressionMethods.GZip;

                        WebResponse response = request.GetResponse();
                        try
                        {
                            using (var reader = new StreamReader(response.GetResponseStream()))
                            {
                                Outputresponse = reader.ReadToEnd();
                                var CalcResponse = JObject.Parse(Outputresponse);
                                resultsStrip = CalcResponse["resultsStrip"][0] as JObject;
                                var sofrRatesFixingArray = CalcResponse["resultsStrip"][0]["rates"]["sofrRatesFixing"];
                               
                                if (error == "")
                                {
                                    foreach (DataRow dr in dtconfig.Rows)
                                    {
                                        string dbindextype = dr["IndexType"].ToString();
                                        if (dbindextype == "1M Term SOFR")
                                        {
                                            CREIndexType = "1M";
                                        }
                                        else if (dbindextype == "3M Term SOFR")
                                        {
                                            CREIndexType = "3M";
                                        } else if (dbindextype== "SOFR") 
                                        {
                                            CREIndexType = "SOFR";
                                        }
                                        if (CREIndexType == "SOFR")
                                        {
                                            //to read from lastest date 
                                            //CurrentDate = CommonHelper.ToDateTime(resultsStrip["date"]);                                          
                                            //CurrentPrice = resultsStrip["overnight"].ToString() == "-" ? 0 : CommonHelper.StringToDecimal(resultsStrip["overnight"]);
                                  
                                            var sofrRates= CalcResponse["resultsStrip"][1];
                                            CurrentDate = CommonHelper.ToDateTime(sofrRates["date"]);
                                            CurrentPrice = sofrRates["overnight"].ToString() == "-" ? 0 : CommonHelper.StringToDecimal(sofrRates["overnight"]);
                                        }
                                        else
                                        {
                                            foreach (JObject rate in sofrRatesFixingArray)
                                            {
                                                var CurrentTerm = rate["term"].ToString();
                                                if (CurrentTerm == CREIndexType)
                                                {
                                                    CurrentPrice = CommonHelper.StringToDecimal(rate["price"]);
                                                    CurrentDate = CommonHelper.ToDateTime(rate["timestamp"]);
                                                    break;
                                                }
                                            }
                                        }
                                       
                                        //dtindexes is used in data saving
                                        DataTable dtindexes = new DataTable();
                                        dtindexes.Columns.Add("Date");
                                        dtindexes.Columns.Add("Name");
                                        dtindexes.Columns.Add("Value");
                                        dtindexes.Columns.Add("IndexesMasterGuid");

                                        DataRow data1 = dtindexes.NewRow();
                                        data1["Date"] = CurrentDate;
                                        data1["Name"] = dr["IndexType"].ToString();
                                        data1["Value"] = CurrentPrice / 100;
                                        data1["IndexesMasterGuid"] = dr["IndexesMasterGuid"].ToString();
                                        dtindexes.Rows.Add(data1);

                                        //add data in import datatable for email
                                        DataRow rmport = dtimport.NewRow();
                                        rmport["Date"] = CurrentDate;
                                        rmport["Value"] = CurrentPrice;

                                        if (dbindextype == "SOFR")
                                        {
                                            rmport["IndexType"] = "Daily SOFR";
                                        }
                                        else
                                        {
                                            rmport["IndexType"] = dbindextype;
                                        }
                                         
                                        dtimport.Rows.Add(rmport);

                                        //save data for index in table
                                        IndexTypeRepository indexTypeRepository = new IndexTypeRepository();
                                        indexTypeRepository.AddUpdateIndexList(dtindexes, useridforSys_Scheduler, useridforSys_Scheduler);

                                        //Check Missing Data for sofr
                                        dtcopy = indexTypeRepository.InsertUpdateMissingIndexList(dr["IndexesMasterGuid"].ToString(), Convert.ToInt16(dr["IndexTypeID"]), useridforSys_Scheduler);


                                    }
                                }
                                else
                                {
                                    Log.WriteLogExceptionMessage(CRESEnums.Module.DailyRatePull.ToString(), "CME Group Daily Rate:" + Outputresponse, "", useridforSys_Scheduler, "GetCMEGroupDailyRate", Outputresponse);
                                    _authenticationResult = new v1GenericResult()
                                    {
                                        Status = Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError,
                                        Succeeded = false,
                                        Message = returnMessage,
                                        ErrorDetails = Outputresponse
                                    };

                                    //_iEmailNotification.SendChathamFinancialDailyRateNotification("An error occurred in the daily SOFR pull from Chatham Financial. M61 systems team is looking for solution.", "Daily", "Fail");
                                }

                            }
                        }
                        catch (Exception ex)
                        {

                            error = "CME Data Pull";
                            Outputresponse = "CME Data Pull Error :" + ex.Message;
                        }
                    }
                }
                //send email
                _iEmailNotification.SendChathamFinancialDailyRateNotificationSucces(dtimport, dtcopy, "CME Group");
                _authenticationResult = new v1GenericResult()
                {
                    Status = Microsoft.AspNetCore.Http.StatusCodes.Status200OK,
                    Succeeded = true,
                    Message = returnMessage,
                    ErrorDetails = ""
                };
            }
            catch (Exception ex)
            {
                _authenticationResult = new v1GenericResult()
                {
                    Status = Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError,
                    Succeeded = false,
                    Message = "Error",
                    ErrorDetails = ex.Message
                };

                Log.WriteLogException(CRESEnums.Module.DailyRatePull.ToString(), "Error occured in GetCMEGroupSofrRate ", "", useridforSys_Scheduler, ex.TargetSite.Name.ToString(), "", ex);
                _iEmailNotification.SendChathamFinancialDailyRateNotification("An error occurred in the daily CME Group rate pull.M61 systems team is looking for the solution.", "Daily", "Fail");
            }
            return Ok(_authenticationResult);
        }

    }
}
