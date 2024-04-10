using CRES.BusinessLogic;
using CRES.BusinessLogic;
using CRES.DataContract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.IO;


namespace CRES.Services.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class ScenarioController : ControllerBase
    {
        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/scenarios/getallscenario")]
        public IActionResult GetAllScenario()
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            int pageSize = 100;
            int pageIndex = 1;

            List<ScenarioParameterDataContract> listscenario = new List<ScenarioParameterDataContract>();
            UserPermissionLogic upl = new UserPermissionLogic();
            ScenarioLogic scenarioLogic = new ScenarioLogic();

            int? totalCount;
            List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "ScenarioManagementPage");
            if (permissionlist != null && permissionlist.Count > 0)
            {
                listscenario = scenarioLogic.GetAllScenario(headerUserID.ToString(), pageIndex, pageSize, out totalCount);
            }

            try
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    lstScenario = listscenario,
                    UserPermissionList = permissionlist
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

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/scenarios/getscenarioparameterbyscenarioid")]
        public IActionResult GetScenarioParameterByScenarioID([FromBody] string scenarioid)
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            ScenarioLogic scenarioLogic = new ScenarioLogic();
            UserPermissionLogic upl = new UserPermissionLogic();
            List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "ScenariosList");
            ScenarioParameterDataContract spdc = new ScenarioParameterDataContract();
            if (permissionlist != null && permissionlist.Count > 0)
            {
                spdc = scenarioLogic.GetScenarioParameterByScenarioID(scenarioid);
            }

            try
            {
                if (spdc != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        ScenarioParameters = spdc,
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
        [Route("api/scenarios/getindexbyscenarioid")]
        public IActionResult GetIndexByScenarioID(int? pageIndx, int? pageSize, string gid)
        {
            GenericResult _authenticationResult = null;
            DataTable IndexTypedatatable = new DataTable();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            ScenarioLogic scenarioLogic = new ScenarioLogic();
            int? totalCount;
            IndexTypedatatable = scenarioLogic.GetIndexByScenarioID(headerUserID, gid.ToString(), pageIndx, pageSize, out totalCount);
            try
            {
                if (IndexTypedatatable != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        dtIndexType = IndexTypedatatable,
                        TotalCount = totalCount,//IndexTypedatatable.Rows.Count,
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        //ScenarioParameters = spdc
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
        [Route("api/scenarios/getindexesfromdate")]
        public IActionResult GetIndexesFromDate([FromBody]ScenariosearchDataContract _ScenariosearchDc)
        {
            GenericResult _authenticationResult = null;
            DataTable IndexTypedatatable = new DataTable();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            ScenarioLogic scenarioLogic = new ScenarioLogic();
            int? totalCount;
            IndexTypedatatable = scenarioLogic.GetIndexesFromDate(_ScenariosearchDc, headerUserID, out totalCount);
            try
            {
                if (IndexTypedatatable != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        dtIndexType = IndexTypedatatable,
                        TotalCount = totalCount,//IndexTypedatatable.Rows.Count,
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        //ScenarioParameters = spdc
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
        [Route("api/scenarios/getindexesexportdata")]
        public IActionResult GetIndexesExportData([FromBody]ScenariosearchDataContract _ScenariosearchDc)
        {
            GenericResult _authenticationResult = null;
            DataTable IndexTypedatatable = new DataTable();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            ScenarioLogic scenarioLogic = new ScenarioLogic();
            int? totalCount;
            IndexTypedatatable = scenarioLogic.GetIndexesExportData(_ScenariosearchDc, headerUserID, out totalCount);
            try
            {
                if (IndexTypedatatable != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        dtIndexType = IndexTypedatatable,
                        TotalCount = totalCount,//IndexTypedatatable.Rows.Count,
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        //ScenarioParameters = spdc
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
        [Route("api/scenarios/insertupdatescenario")]
        public IActionResult InsertUpdateScenario([FromBody]ScenarioParameterDataContract scenarioDC)
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            string scenariomsg = "";
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            scenarioDC.CreatedBy = headerUserID;
            scenarioDC.UpdatedBy = headerUserID;

            ScenarioLogic scenarioLogic = new ScenarioLogic();
            string res = scenarioLogic.InsertUpdateScenario(scenarioDC);

            if (scenarioDC.ActionStatus == "CalcAndSave")
            {
                scenariomsg = "Scenario Details saved successfully. All loans are being added for recalculation.";
                Thread FirstThread = new Thread(() => AddNoteInCalculationRequestsByScenarioID(scenarioDC.AnalysisID, headerUserID));
                FirstThread.Start();
                
            }
            else
            {

                scenariomsg = "Scenario Details saved successfully.";
            }
            try
            {
                if (headerUserID != null)
                {
                    if (res != "FALSE")
                    {
                        _authenticationResult = new GenericResult()
                        {
                            newDeailID = res,
                            Succeeded = true,
                            Message = "Changes were saved successfully.",
                            ScenarioMsg = scenariomsg,
                            newscenarioid = scenarioDC.AnalysisID
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

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/scenarios/getallLookup")]
        public IActionResult GetAllLookup()
        {
            string getAllLookup = "52,79,2,98,133,134";
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
                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }

        public void SendRequestToRecalAlllNotes(CalculationManagerDataContract DCcalc)
        { 
            GetConfigSetting();
            CalculationManagerLogic calculationlogic = new CalculationManagerLogic();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
           // var Enablem61Calculation = Sectionroot.GetSection("Enablem61Calculation").Value;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            AppConfigLogic appl = new AppConfigLogic();
            //to get user 
            List<CalculationManagerDataContract> lstcalculationstatus = calculationlogic.RefreshcalculationStatus(DCcalc, new Guid(headerUserID));

            //string envname = System.Configuration.ConfigurationManager.AppSettings["ApplicationName"].ToString();
            string envname = Sectionroot.GetSection("ApplicationName").Value;

            foreach (CalculationManagerDataContract cdc in lstcalculationstatus)
            {
                cdc.StatusText = "Processing";
                cdc.UserName = DCcalc.UserName;
                cdc.ApplicationText = envname;
                cdc.PriorityText = "Batch";
            }
            calculationlogic.QueueNotesForCalculation(lstcalculationstatus, DCcalc.UserName);
        }

        public void AddNoteInCalculationRequestsByScenarioID(string AnalysisID,string username)
        {
            GetConfigSetting();
            CalculationManagerLogic calculationlogic = new CalculationManagerLogic();         
            //string envname = System.Configuration.ConfigurationManager.AppSettings["ApplicationName"].ToString();
            string envname = Sectionroot.GetSection("ApplicationName").Value;
            calculationlogic.AddNoteInCalculationRequestsByScenarioID(AnalysisID, username, envname);
        }
        

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/scenarios/checkduplicatescenario")]
        public IActionResult CheckDuplicateScenarioName([FromBody] ScenarioParameterDataContract scenarioDC)
        {
            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            ScenarioLogic scenarioLogic = new ScenarioLogic();
            bool res = scenarioLogic.CheckDuplicateScenarioName(scenarioDC.AnalysisID, scenarioDC.ScenarioName);
            try
            {
                if (res == true)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Duplicate",
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "NoteDuplicate"
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
        [Route("api/scenarios/InsertUpdateScenarioUserMap")]
        public IActionResult InsertUpdateScenarioUserMap([FromBody]ScenarioUserMapDataContract scenarioDC)
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            string scenariomsg = "";
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            scenarioDC.CreatedBy = headerUserID;
            scenarioDC.UpdatedBy = headerUserID;

            ScenarioLogic scenarioLogic = new ScenarioLogic();
            string res = scenarioLogic.InsertUpdateScenarioUserMap(scenarioDC);

            try
            {
                if (headerUserID != null)
                {
                    if (res != "FALSE")
                    {
                        _authenticationResult = new GenericResult()
                        {

                            Succeeded = true,
                            Message = "Changes were saved successfully."

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
            //var javaScriptSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            //string jsonString = javaScriptSerializer.Serialize(_authenticationResult);
            //var json = new JavaScriptSerializer().Serialize(_authenticationResult);
            //Request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            return Ok(_authenticationResult);
            //    return Ok(_authenticationResult);
        }


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/scenarios/GetScenarioUserMapByUserID")]
        public IActionResult GetScenarioUserMapByUserID([FromBody]ScenarioUserMapDataContract _ScenarioDc)
        {
            GenericResult _authenticationResult = null;
            ScenarioUserMapDataContract retScenarioDC = new ScenarioUserMapDataContract();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            ScenarioLogic scenarioLogic = new ScenarioLogic();

            retScenarioDC = scenarioLogic.GetScenarioUserMapByUserID(_ScenarioDc.UserID);
            try
            {
                if (retScenarioDC != null)
                {
                    _authenticationResult = new GenericResult()
                    {

                        ScenarioUserMap = retScenarioDC,
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        //ScenarioParameters = spdc
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
        [Route("api/scenarios/GetAllScenarioDistinct")]
        public IActionResult GetAllScenarioDistinct([FromBody]ScenarioParameterDataContract _ScenarioDc)
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }


            List<ScenarioUserMapDataContract> listscenario = new List<ScenarioUserMapDataContract>();
            UserPermissionLogic upl = new UserPermissionLogic();
            ScenarioLogic scenarioLogic = new ScenarioLogic();


            listscenario = scenarioLogic.GetAllScenarioDistinct(_ScenarioDc.UserID.ToString());

            try
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    lstScenarioUserMap = listscenario
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



        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/scenarios/GetScenarioDownload")]
        public IActionResult GetScenarioDownload([FromBody]ScenariosearchDataContract _ScenariosearchDc)
        {
            GenericResult _authenticationResult = null;
            //DataTable IndexTypedatatable = new DataTable();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            ScenarioLogic scenarioLogic = new ScenarioLogic();
            //int? totalCount;
            
            NoteLogic _noteLogic = new NoteLogic();
            DataTable lstNoteCashflowsExportData = new DataTable();
            DealLogic deallogic = new DealLogic();
            DataTable exceptiondt = new DataTable();
            lstNoteCashflowsExportData = _noteLogic.GetNoteCashflowsExportData(new Guid("00000000-0000-0000-0000-000000000000"), new Guid("00000000-0000-0000-0000-000000000000"), new Guid(_ScenariosearchDc.AnalysisID), "");
            exceptiondt = deallogic.GetAllExceptionsByDealID(new Guid(), "Scenario", "", new Guid(),new Guid());
            

            try
            {
                if (lstNoteCashflowsExportData != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        dtIndexType = lstNoteCashflowsExportData,
                        TotalCount = 0,
                        dt = exceptiondt,
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        //ScenarioParameters = spdc
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
                //Logger.Write("CashFlow Download", "Error in cashFlow Download: ", MessageLevel.Error, headerUserID.ToString(), "");
                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
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

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/scenarios/GetAllRuleType")]
        public IActionResult GetAllRuleType()
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            List<ScenarioruletypeDataContract> srlist = new List<ScenarioruletypeDataContract>();
            ScenarioLogic scenarioLogic = new ScenarioLogic();

            srlist = scenarioLogic.GetAllRuleType(headerUserID);

            try
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    lstScenariorule = srlist
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
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/scenarios/GetAllRuleTypeDetail")]
        public IActionResult GetAllRuleTypeDetail()
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            List<ScenarioruletypeDataContract> srlist = new List<ScenarioruletypeDataContract>();
            ScenarioLogic scenarioLogic = new ScenarioLogic();

            srlist = scenarioLogic.GetAllRuleTypeDetail(headerUserID);

            try
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    lstScenarioRuleDetail = srlist
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

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/scenarios/GetRuleTypeSetupbyObjectId")]
        public IActionResult GetRuleTypeSetupbyObjectId([FromBody] string Id)
        {
            GenericResult _authenticationResult = null;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            List<ScenarioruletypeDataContract> listscenario = new List<ScenarioruletypeDataContract>();
            ScenarioLogic scenarioLogic = new ScenarioLogic();
            listscenario = scenarioLogic.GetRuleTypeSetupbyObjectId(Id);

            try
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    lstScenariorule = listscenario
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

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/scenarios/AddUpdateAnalysisRuleTypeSetup")]
        public IActionResult AddUpdateAnalysisRuleTypeSetup([FromBody] List<ScenarioruletypeDataContract> scenarioDC)
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            string scenariomsg = "";
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            ScenarioLogic scenarioLogic = new ScenarioLogic();
            string res = scenarioLogic.AddUpdateAnalysisRuleTypeSetup(scenarioDC, headerUserID);

            try
            {
                if (res != null)
                {
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
        [Route("api/scenarios/deleteScenariobyAnalysisID")]
        public IActionResult deleteScenariobyAnalysisID([FromBody] string AnalysisID)
        {
            GenericResult _authenticationResult = null;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            ScenarioLogic scenarioLogic = new ScenarioLogic();
            var res = scenarioLogic.deleteScenariobyAnalysisID(AnalysisID, headerUserID.ToString());

            try
            {
                if (res == "TRUE")
                {
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