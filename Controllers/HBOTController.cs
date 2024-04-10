using CRES.BusinessLogic;
using CRES.DataContract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web;



namespace CRES.Services.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    //added authentication
    public class HBOTController : ControllerBase                     
    {
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

        
        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        //[Services.Controllers.DeflateCompression]
        [Route("api/HBOT/GetSingleEntityByIntent")]
        public IActionResult GetSingleEntityByIntent(string ObjectType, string ObjectNature, string ObjectValue, string Intent)
        {
            HBOTGenericResult _authenticationResult = new HBOTGenericResult();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            DateTime Starttime = DateTime.Now;
            HBOTLogic dySizerLogic = new HBOTLogic();
            String result = dySizerLogic.GetSingleEntityByIntent(ObjectType, ObjectNature, ObjectValue, Intent);
            if (Intent == "DownloadCashFlow")
            {
                GetConfigSetting();
                string BaseUrl = Sectionroot.GetSection("apiPath").Value;

                result = BaseUrl + "api/HBOT/downloadDealcashflow?DealID=" + result;
            }
            DateTime Endtime = DateTime.Now;

            //insert start and end time 
            Thread FirstThread = new Thread(()=> dySizerLogic.InsertAIApiStartandEndTime(headerUserID,Starttime,Endtime, Intent));
            FirstThread.Start();

            try
            {
                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    SingleResult = result
                };
            }
            catch (Exception ex)
            {

                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }


        [HttpGet]
        [Services.Controllers.IsAuthenticate]
       // [Services.Controllers.DeflateCompression]
        [Route("api/HBOT/GetSingleEntityByIntentGeneric")]
        public IActionResult GetSingleEntityByIntentGeneric(string ObjectType, string ObjectNature, string ObjectValue, string Intent)
        {
            HBOTGenericResult _authenticationResult = new HBOTGenericResult();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            DateTime Starttime = DateTime.Now;
            HBOTLogic dySizerLogic = new HBOTLogic();
            String result = dySizerLogic.GetSingleEntityByIntentGeneric(ObjectType, ObjectNature, ObjectValue, Intent);

            DateTime Endtime = DateTime.Now;
            //insert start and end time 
            Thread FirstThread = new Thread(() => dySizerLogic.InsertAIApiStartandEndTime(headerUserID, Starttime, Endtime, Intent));
            FirstThread.Start();

            try
            {
                    _authenticationResult = new HBOTGenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        SingleResult = result
                    };
            }
            catch (Exception ex)
            {

                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }



        [HttpGet]
       // [Services.Controllers.DeflateCompression]
        [Services.Controllers.IsAuthenticate]
        [Route("api/HBOT/getListEntityByIntent")]
        public IActionResult getListEntityByIntent(string ObjectType, string ObjectNature, string ObjectValue, string Intent)
        {
            HBOTGenericResult _authenticationResult = new HBOTGenericResult();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            DateTime Starttime = DateTime.Now;
            DataTable dt = new DataTable();
            HBOTLogic dySizerLogic = new HBOTLogic();
            dt = dySizerLogic.GetListEntityByIntent(ObjectType, ObjectNature, ObjectValue, Intent);

            DateTime Endtime = DateTime.Now;
            //insert start and end time 
            Thread FirstThread = new Thread(() => dySizerLogic.InsertAIApiStartandEndTime(headerUserID, Starttime, Endtime, Intent));
            FirstThread.Start();
            try
            {
                         _authenticationResult = new HBOTGenericResult()
                        {
                            Succeeded = true,
                            Message = "success",
                             Listdt = dt
                        };
                    
            }
            catch (Exception ex)
            {
                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }

        
        [HttpGet]
        [Services.Controllers.IsAuthenticate]
      //  [Services.Controllers.DeflateCompression]
        [Route("api/HBOT/GetSingleEntityByIntentForNoteAndDeal")]
        public IActionResult GetSingleEntityByIntentForNoteAndDeal(string NoteNature, string NoteValue, string DealNature, string DealValue, string Intent)
        {
            HBOTGenericResult _authenticationResult = new HBOTGenericResult();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            DateTime Starttime = DateTime.Now;

            HBOTLogic hbotLogic = new HBOTLogic();
            String result = hbotLogic.GetSingleEntityByIntentForNoteAndDeal(NoteNature, NoteValue, DealNature, DealValue, Intent);

            DateTime Endtime = DateTime.Now;
            //insert start and end time 
            Thread FirstThread = new Thread(() => hbotLogic.InsertAIApiStartandEndTime(headerUserID, Starttime, Endtime, Intent));
            FirstThread.Start();
            try
            {
                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    SingleResult = result
                };
                //if (result != "")
                //{

                //    _authenticationResult = new HBOTGenericResult()
                //    {
                //        Succeeded = true,
                //        Message = "Authentication succeeded",
                //        SingleResult = result
                //    };
                //}
                //else
                //{
                //    _authenticationResult = new HBOTGenericResult()
                //    {
                //        Succeeded = false,
                //        Message = "Authentication failed"
                //    };
                //}
            }
            catch (Exception ex)
            {

                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }

        [HttpGet]
      //  [Services.Controllers.DeflateCompression]
        [Services.Controllers.IsAuthenticate]
        [Route("api/HBOT/GetSingleEntityByIntentForNoteAndDealByDateRange")]
        public IActionResult GetSingleEntityByIntentForNoteAndDealByDateRange(string NoteNature, string NoteValue, string DealNature, string DealValue, string StartDate, string EndDate, string Intent)
        
        {
            HBOTGenericResult _authenticationResult = new HBOTGenericResult();
            DataTable dt = new DataTable();
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            DateTime Starttime = DateTime.Now;
            HBOTLogic hbotLogic = new HBOTLogic();
            string Enddt ="";
            string startdt = "";
            if (EndDate != "null") {
                string[] splittedenddate = EndDate.Split(new char[] { ' ','T' });
                var Enddate = Convert.ToDateTime(splittedenddate[0]);
                Enddt = Enddate.ToString("MM-dd-yyyy");
            }
            else
            {
                Enddt = DateTime.Now.ToString("MM-dd-yyyy");
            }
            if (StartDate != "null")
            {
                string[] splittedstartdate = StartDate.Split(new char[] { ' ', 'T' });
                var startdate = Convert.ToDateTime(splittedstartdate[0]);
                startdt = startdate.ToString("MM-dd-yyyy");
            }
            else
            {
                startdt = DateTime.Now.ToString("MM-dd-yyyy");
            }

            dt = hbotLogic.GetSingleEntityByIntentForNoteAndDealByDateRange(NoteNature, NoteValue, DealNature, DealValue, Convert.ToDateTime(startdt), Convert.ToDateTime(Enddt), Intent);
            DateTime Endtime = DateTime.Now;
            //insert start and end time 
            Thread FirstThread = new Thread(() => hbotLogic.InsertAIApiStartandEndTime(headerUserID, Starttime, Endtime, Intent));
            FirstThread.Start();

            try
            {
                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    Listdt = dt
                };
            }
            catch (Exception ex)
            {

                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }

        [HttpGet]
       // [Services.Controllers.DeflateCompression]
        [Services.Controllers.IsAuthenticate]
        [Route("api/HBOT/getListEntityByIntentForNoteAndDeal")]
        public IActionResult getListEntityByIntentForNoteAndDeal(string NoteNature, string NoteValue, string DealNature, string DealValue, string Intent)
        {
            HBOTGenericResult _authenticationResult = new HBOTGenericResult();
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            DataTable dt = new DataTable();
            DateTime Starttime = DateTime.Now;
            HBOTLogic dySizerLogic = new HBOTLogic();
            dt = dySizerLogic.GetListEntityByIntentForNoteAndDeal(NoteNature, NoteValue, DealNature, DealValue, Intent);
            DateTime Endtime = DateTime.Now;
            //insert start and end time 
            Thread FirstThread = new Thread(() => dySizerLogic.InsertAIApiStartandEndTime(headerUserID, Starttime, Endtime, Intent));
            FirstThread.Start();

            try
            {
                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = true,
                    Message = "success",
                    Listdt = dt
                };

            }
            catch (Exception ex)
            {
                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }

        [HttpGet]
       // [Services.Controllers.DeflateCompression]
       // [Services.Controllers.IsAuthenticate]  //(second api for download cashflow, auth not required)
        [Route("api/HBOT/downloadDealcashflow")]
        public IActionResult downloadDealcashflow(string DealID)
        {
            HBOTGenericResult _authenticationResult = new HBOTGenericResult();
            DataTable dtfilename = new DataTable();
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            DataTable lstNoteCashflowsExportData = new DataTable();
            var AnalysisId = new Guid("C10F3372-0FC2-4861-A9F5-148F1F80804F");
            HBOTLogic _hbotlogic = new HBOTLogic();
            NoteLogic _notelogic = new NoteLogic();
            lstNoteCashflowsExportData = _notelogic.GetNoteCashflowsExportData(new Guid(), new Guid(DealID), AnalysisId, "");
            dtfilename = _hbotlogic.GetCREdealIDByDealID(new Guid(DealID), headerUserID);
            var fileName = dtfilename.Rows[0]["CREDealID"] + "_Default_Cashflow_" + DateTime.Now.ToString("MM-dd-yyyy_hh-mm-ss tt");
            try
            {
                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = true,
                    Message = "Downloaded successfully.",
                    Listdt = lstNoteCashflowsExportData,
                    SingleResult = fileName
                };
            }
            catch (Exception ex)
            {

                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }



        [HttpGet]
      //  [Services.Controllers.DeflateCompression]
        [Route("api/HBOT/GetListOfEntity")]
        public IActionResult GetListOfEntity()
        {
            HBOTGenericResult _auctionResult = null;
            List<HBOTEntityDataContract> lstEntityResult = new List<HBOTEntityDataContract>();
           
            HBOTLogic _hbotLogic = new HBOTLogic();
            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            lstEntityResult = _hbotLogic.GetListOfEntity(headerUserID);

            //=== Create Json =====================
            var distinct_EntityList = lstEntityResult.Select(c => new HBOTEntityDataContract()
            {
                entity_type = c.entity_type,
                entity_names = c.entity_names
            }).AsEnumerable().Distinct();


            List<lstOfEntity> _Result = new List<lstOfEntity>();
            var listobj = distinct_EntityList.Select(x => x.entity_type).Distinct().ToList();
            foreach (var etype in listobj)
            {
                var myList = new Dictionary<string, ArrayList>();
                foreach (var P_item in distinct_EntityList.Where(x => x.entity_type == etype).ToList())
                {
                    ArrayList arr = new ArrayList();
                    var q = lstEntityResult.FindAll(x => x.entity_type == P_item.entity_type && x.entity_names == P_item.entity_names).Select(x => new { x.synonyms }).ToList();

                    foreach (var item in q)
                    {
                        arr.Add(item.synonyms);
                    }
                    myList.Add(P_item.entity_names, arr);
                }
                lstOfEntity _entity = new lstOfEntity();
                _entity.entity_type = etype;
                _entity.entity_names = myList;
                _Result.Add(_entity);
            }
            

            try
            {
                if (lstEntityResult != null)
                {
                    _auctionResult = new HBOTGenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstOFEntity = _Result
                    };
                }
                else
                {
                    _auctionResult = new HBOTGenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed"
                    };
                }
            }
            catch (Exception ex)
            {
                _auctionResult = new HBOTGenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_auctionResult);
        }

        

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
      //  [Services.Controllers.DeflateCompression]
        [Route("api/HBOT/getchatlogHistory")]
        public IActionResult GetChatLogHistory([FromBody] int pageindex, int pagesize)
        {
            HBOTGenericResult _authenticationResult = new HBOTGenericResult();
            DataTable dt = new DataTable();
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            HBOTLogic hbotLogic = new HBOTLogic();
           // headerUserID = 'b0e6697b-3534-4c09-be0a-04473401ab93';
            dt = hbotLogic.GetchatlogHistory(headerUserID, pageindex, pagesize);

            try
            {
                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = true,
                    Message = "fetch successfully.",
                    Listdt=dt
                };
            }
            catch (Exception ex)
            {

                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }

        [HttpPost]
       // [Services.Controllers.DeflateCompression]
        [Services.Controllers.IsAuthenticate]
        [Route("api/HBOT/insertchatlogfordashboard")]
        public IActionResult InsertHBOTChatLogorDashboard([FromBody] DataTable dt)
        {
            HBOTGenericResult _authenticationResult = new HBOTGenericResult();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            var Status = Convert.ToString(dt.Rows[0]["Status"]);
            var Question = Convert.ToString(dt.Rows[0]["Question"]);
            var intentName = Convert.ToString(dt.Rows[0]["IntentName"]);
            var sentby = Convert.ToString(dt.Rows[0]["SentBy"]);
            var sessionId = Convert.ToString(dt.Rows[0]["LoginSession"]);
            HBOTLogic hbotLogic = new HBOTLogic();
            Thread FirstThread = new Thread(() => hbotLogic.InsertHBOTChatLog(Status, Question, intentName, Convert.ToString(headerUserID), sentby, sessionId)); 
            FirstThread.Start();
            try
            {
                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = true,
                    Message = "Chat log added successfully.",
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.AI_Assistant.ToString(), "Error occurred  while saving question/answer: " + Question, sentby, headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }

        [HttpGet]
       // [Services.Controllers.DeflateCompression]
        [Services.Controllers.IsAuthenticate]
        [Route("api/HBOT/GetListEntityByIntentForNoteAndDealByIntegerValue")]
        public IActionResult GetListEntityByIntentForNoteAndDealByIntegerValue(string NoteNature, string NoteValue, string DealNature, string DealValue, decimal IntValue, string Intent)
        {
            HBOTGenericResult _authenticationResult = new HBOTGenericResult();
            DataTable dt = new DataTable();
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            DateTime Starttime = DateTime.Now;
            HBOTLogic hbotLogic = new HBOTLogic();
            dt = hbotLogic.GetListEntityByIntentForNoteAndDealByIntegerValue( NoteNature, NoteValue, DealNature, DealValue, IntValue, Intent);
            DateTime Endtime = DateTime.Now;
            //insert start and end time 
            Thread FirstThread = new Thread(() => hbotLogic.InsertAIApiStartandEndTime(headerUserID, Starttime, Endtime, Intent));
            FirstThread.Start();

            try
            {
                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    Listdt = dt
                };
            }
            catch (Exception ex)
            {

                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }




        [HttpGet]
       // [Services.Controllers.DeflateCompression]
        // [Services.Controllers.IsAuthenticate]
        [Route("api/HBOT/getautosuggestsearchdatabyKey")]
        public IActionResult GetAutosuggestSearchData(string search, int? pageIndex, int? pageSize)
        {
            HBOTGenericResult _auctionResult = null;
            List<SearchDataContract> lstSearchResult = new List<SearchDataContract>();
            SearchLogic _searchLogic = new SearchLogic();
            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            int? totalCount;
            lstSearchResult = _searchLogic.GetAutosuggestSearchData(headerUserID, pageIndex, pageSize, search, out totalCount);
            var TotalCount = Convert.ToInt32(totalCount);
            try
            {
                if (lstSearchResult != null)
                {
                    _auctionResult = new HBOTGenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstSearch = lstSearchResult
                    };
                }
                else
                {
                    _auctionResult = new HBOTGenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed"
                    };
                }
            }
            catch (Exception ex)
            {
                _auctionResult = new HBOTGenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_auctionResult);
        }

        [HttpGet]
       // [Services.Controllers.DeflateCompression]
        [Route("api/HBOT/insertchatlog")]
        public IActionResult InsertHBOTChatLog(string Status, string Question)
        {
            HBOTGenericResult _authenticationResult = new HBOTGenericResult();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            HBOTLogic hbotLogic = new HBOTLogic();
            Thread FirstThread = new Thread(() => hbotLogic.InsertHBOTChatLog(Status, Question, null, Convert.ToString(headerUserID), null,null));
            FirstThread.Start();

            try
            {
                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = true,
                    Message = "Chat log added successfully.",
                };
            }
            catch (Exception ex)
            {

                _authenticationResult = new HBOTGenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }
    }
}