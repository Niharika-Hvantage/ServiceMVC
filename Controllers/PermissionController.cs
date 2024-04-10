using Amazon.SimpleSystemsManagement;
using CRES.BusinessLogic;
using CRES.BusinessLogic;
using CRES.DataContract;
using CRES.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using CRES.Services;


namespace CRES.Services.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class PermissionController : ControllerBase
    {


        private readonly IEmailNotification _iEmailNotification;
        public PermissionController(IEmailNotification iemailNotification)
        {
            _iEmailNotification = iemailNotification;
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/permission/getuserpermissionbypagename")]
        public IActionResult GetUserPermissionByPageName([FromBody] string pagename)
        {
            GenericResult _authenticationResult = null;
            List<LookupDataContract> lstlookupDC = new List<LookupDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            UserPermissionLogic upl = new UserPermissionLogic();

            List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), pagename);

            try
            {
                if (lstlookupDC != null)
                {
                    Logger.Write("Lookup loaded successfully", MessageLevel.Info);
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
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
        [Route("api/permission/getrole")]
        public IActionResult GetRole()
        {
            GenericResult _authenticationResult = null;

            UserPermissionLogic upl = new UserPermissionLogic();

            List<RoleDataContract> rolelist = upl.GetRole();

            try
            {
                if (rolelist.Count > 0)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        RoleList = rolelist
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
        [Route("api/permission/getmoduletabmaster")]
        public IActionResult GetModuleTabMaster()
        {
            GenericResult _authenticationResult = null;

            UserPermissionLogic upl = new UserPermissionLogic();

            List<ModuleTabMasterDataContract> mtmlist = upl.GetModuleTabMaster();

            try
            {
                if (mtmlist.Count > 0)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "succeeded",
                        ModuleTabMasterList = mtmlist
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "failed"
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
        [Route("api/permission/getpermissionbyroleid")]
        public IActionResult GetPermissionByRoleId([FromBody] string RoleId)
        {
            GenericResult _authenticationResult = null;

            UserPermissionLogic upl = new UserPermissionLogic();

            List<ModuleTabMasterDataContract> mtmlist = upl.GetPermissionByRoleId(RoleId);

            try
            {
                if (mtmlist.Count > 0)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "succeeded",
                        ModuleTabMasterList = mtmlist
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "failed"
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
        [Route("api/permission/InsertUpdateUserPermissionByRoleID")]
        public IActionResult InsertUpdateUserPermissionByRoleID([FromBody] List<ModuleTabMasterDataContract> lstModuleTabMaster)
        {
            GenericResult _authenticationResult = null;

            UserPermissionLogic upl = new UserPermissionLogic();

            upl.InsertUpdateUserPermissionByRoleID(lstModuleTabMaster);

            try
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "succeeded"
                };

                //if (mtmlist.Count > 0)
                //{

                //    _authenticationResult = new GenericResult()
                //    {
                //        Succeeded = true,
                //        Message = "succeeded",
                //        ModuleTabMasterList = mtmlist
                //    };
                //}
                //else
                //{
                //    _authenticationResult = new GenericResult()
                //    {
                //        Succeeded = false,
                //        Message = "failed"
                //    };
                //}
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
        [Route("api/permission/AddUpdateRole")]
        public IActionResult AddUpdateRole([FromBody]RoleDataContract roledc)
        {
            GenericResult _authenticationResult = null;

            UserPermissionLogic upl = new UserPermissionLogic();

            upl.AddUpdateRole(roledc);

            try
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "succeeded"
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
        [Route("api/permission/ResetPassword")]
        public IActionResult Resetuserpassword([FromBody]UserDataContract userdc)
        {
            GenericResult _authenticationResult = null;

            // EmailNotification emailnf = new EmailNotification();
            UserLogic userlogic = new UserLogic();


            try
            {
                string newPassword = Encryptor.GenerateStrongPassword();
                userdc.Password = Encryptor.MD5Hash(newPassword);
                int res = userlogic.ResetUserPassword(userdc);
                _iEmailNotification.SendEmailResetPasswordNotification(userdc.FirstName, userdc.SuperAdminName, userdc.Login, userdc.Email, newPassword);
                if (res == 1)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "succeeded"
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
        [Route("api/permission/resetpasswordandsendactivationlink")]
        public IActionResult ResetuserpasswordAndSendActivationLink([FromBody]UserDataContract userdc)
        {
            GenericResult _authenticationResult = null;

            // EmailNotification emailnf = new EmailNotification();
            UserLogic userlogic = new UserLogic();


            try
            {
                string newPassword = Encryptor.GenerateStrongPassword();
                userdc.Password = Encryptor.MD5Hash(newPassword);
                int res = userlogic.ResetUserPassword(userdc);
                _iEmailNotification.SendEmailResetPasswordActivationLinkNotification(userdc.FirstName, userdc.SuperAdminName, userdc.Login, userdc.Email, userdc.Password);
                if (res == 1)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "succeeded"
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
        [Services.Controllers.DeflateCompression]
        [Route("api/permission/forgotpasswordandsendactivationlink")]
        public IActionResult ForgotPasswordandsendactivationlink([FromBody]UserDataContract userdc)
        {
            GenericResult _authenticationResult = null;

            // EmailNotification emailnf = new EmailNotification();
            UserLogic userlogic = new UserLogic();
            UserDataContract _userdc = new UserDataContract();

            try
            {
                string newAuthenticationKey = Encryptor.GenerateStrongPassword();
                userdc.AuthenticationKey = Encryptor.MD5Hash(newAuthenticationKey);
                _userdc = userlogic.UserForgotPassword(userdc).FirstOrDefault();
                if (_userdc.Email != "")
                {
                    _iEmailNotification.SendEmailForgotPasswordActivationLinkNotification(_userdc.FirstName, _userdc.Email, userdc.AuthenticationKey);

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "succeeded"
                    };

                }
            }
            catch (Exception ex)
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = false, // return true because we are not showing error message in case user not exists.
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }
        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/permission/getallschedulerconfig")]
        public IActionResult GetALLSchedulerConfig([FromBody] SchedulerParamDataContract paramDC)
        {
            GenericResult _authenticationResult = null;
            UserPermissionLogic upl = new UserPermissionLogic();
            List<SchedulerConfigDataContract> lstSchedulerConfig = new List<SchedulerConfigDataContract>();
            try
            {
                //get all scheduler config
                lstSchedulerConfig = upl.GetALLSchedulerConfig("", paramDC);
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    ListSchedulerConfig = lstSchedulerConfig
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
        [Route("api/permission/addupdateschedulerconfig")]
        public IActionResult AddUpdateSchedulerConfig([FromBody] List<SchedulerConfigDataContract> _lstSchedulerConfig)
        {
            GenericResult _authenticationResult = null;

            // EmailNotification emailnotify = new EmailNotification();
            UserPermissionLogic upl = new UserPermissionLogic();
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            try
            {
                _lstSchedulerConfig.Where(i => i.Status == 0).ToList().ForEach(c => c.Status = 2);
                upl.AddUpdateSchedulerConfig(headerUserID, _lstSchedulerConfig);
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Changes were saved successfully."

                };

            }
            catch (Exception ex)
            {
                //LoggerLogic Log = new LoggerLogic();
                //Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in AddUpdateSchedulerConfig", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_authenticationResult); ;

        }
        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/permission/runscheduler")]
        public IActionResult RunScheduler([FromBody] SchedulerConfigDataContract _schedulerConfig)
        {
            GetConfigSetting();
            string apiBaseURL = Sectionroot.GetSection("apiPath").Value;
            GenericResult _authenticationResult = null;
            SchedulerConfigDataContract objSchedulerConfig = null;
            SchedulerLogDataContract schedulerDC = null;
            SchedulerConfigDataContract schedulerConfigDC = null;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            try
            {

                UserPermissionLogic _userPermission = new UserPermissionLogic();

                //get  scheduler to be run
                objSchedulerConfig = _userPermission.GetSchedulerConfigByID(headerUserID, _schedulerConfig);

                //run asynchronously
                System.Threading.Tasks.Task.Factory.StartNew(() =>
                    {
                        using (var client = new HttpClient())
                        {
                            int SchedulerLogID = 0;
                            try
                            {
                                //make an entry in  schedular log with start time
                                schedulerDC = new SchedulerLogDataContract { SchedulerConfigID = objSchedulerConfig.SchedulerConfigID, StartTime = DateTime.Now, GeneratedBy = _schedulerConfig.GeneratedBy };
                                SchedulerLogID = _userPermission.InsertUpdateSchedulerLog("", schedulerDC);

                                //set job status as Running
                                schedulerConfigDC = new SchedulerConfigDataContract { SchedulerConfigID = objSchedulerConfig.SchedulerConfigID, JobStatus = "Running" };
                                _userPermission.UpdateSchedulerConfig("", schedulerConfigDC);

                                client.BaseAddress = new Uri(apiBaseURL);
                                //Call API
                                string APIName = objSchedulerConfig.APIname;
                                if (objSchedulerConfig.ObjectID != 0 && objSchedulerConfig.ObjectID != null)
                                {
                                    APIName = APIName + "?ObjectID=" + objSchedulerConfig.ObjectID;
                                }
                                var responseTask = client.GetAsync(APIName);
                                responseTask.Wait();

                                var result = responseTask.Result;
                                //update scheduler log End time
                                schedulerDC = new SchedulerLogDataContract { SchedulerLogID = SchedulerLogID, EndTime = DateTime.Now, GeneratedBy = _schedulerConfig.GeneratedBy };
                                SchedulerLogID = _userPermission.InsertUpdateSchedulerLog("", schedulerDC);

                                if (result.IsSuccessStatusCode)
                                {
                                    System.Threading.Tasks.Task<string> responseJsonString = result.Content.ReadAsStringAsync();
                                    var genericResult = Newtonsoft.Json.JsonConvert.DeserializeObject<GenericResult>(responseJsonString.Result);
                                    if (genericResult.Succeeded)
                                    {
                                        ////update scheduler log End time
                                        //schedulerDC = new SchedulerLogDataContract { SchedulerLogID = SchedulerLogID, EndTime = DateTime.Now, GeneratedBy = "System" };
                                        //SchedulerLogID = _userPermission.InsertUpdateSchedulerLog("", schedulerDC);

                                        //update next execution time
                                        schedulerConfigDC = new SchedulerConfigDataContract { SchedulerConfigID = objSchedulerConfig.SchedulerConfigID, JobStatus = "Success" };
                                        _userPermission.UpdateSchedulerConfig("", schedulerConfigDC);
                                    }
                                    else
                                    {
                                        //update next execution time
                                        schedulerConfigDC = new SchedulerConfigDataContract { SchedulerConfigID = objSchedulerConfig.SchedulerConfigID, JobStatus = "Failed" };
                                        _userPermission.UpdateSchedulerConfig("", schedulerConfigDC);
                                    }
                                }

                                else//error
                                {
                                    //update next execution time
                                    schedulerConfigDC = new SchedulerConfigDataContract { SchedulerConfigID = objSchedulerConfig.SchedulerConfigID, JobStatus = "Failed" };
                                    _userPermission.UpdateSchedulerConfig("", schedulerConfigDC);
                                }
                            }
                            catch (Exception ex)
                            {
                                //update scheduler log End time
                                schedulerDC = new SchedulerLogDataContract { SchedulerLogID = SchedulerLogID, EndTime = DateTime.Now, GeneratedBy = _schedulerConfig.GeneratedBy };
                                SchedulerLogID = _userPermission.InsertUpdateSchedulerLog("", schedulerDC);

                                //update next execution time
                                schedulerConfigDC = new SchedulerConfigDataContract { SchedulerConfigID = objSchedulerConfig.SchedulerConfigID, JobStatus = "Failed" };
                                _userPermission.UpdateSchedulerConfig("", schedulerConfigDC);

                                //add exception in logger
                                _authenticationResult = new GenericResult()
                                {
                                    Succeeded = false,
                                    Message = ex.Message
                                };

                            }
                        }
                    });

                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Scheduler executed successfully",
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.GenericScheduler.ToString(), "Error occurred  while running Generic Schedular by manually.", headerUserID.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

            }
            return Ok(_authenticationResult); ;

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
        [Route("api/permission/getholidaycalendar")]
        public IActionResult GetHolidayCalendar()
        {
            GenericResult _authenticationResult = null;
            DataTable dt = new DataTable();
            DataTable dtholidaymaster = new DataTable();
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            UserPermissionLogic upl = new UserPermissionLogic();

            dt = upl.GetHolidayCalendar(new Guid(headerUserID));
            dtholidaymaster = upl.GetHolidayMaster(new Guid(headerUserID));
            try
            {
                if (dt.Rows.Count > 0 || dtholidaymaster.Rows.Count > 0)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "succeeded",
                        dt = dt,
                        dtholidaymaster = dtholidaymaster

                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "failed"
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
        [Route("api/permission/addholidaycalendarname")]
        public IActionResult AddCalendarName([FromBody] string CalendarName)
        {
            GenericResult _authenticationResult = null;
            List<LookupDataContract> lstlookupDC = new List<LookupDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            UserPermissionLogic upl = new UserPermissionLogic();

            upl.AddHolidayCalendarName(new Guid(headerUserID), CalendarName);

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
        [Route("api/permission/addholidaydates")]
        public IActionResult AddHolidayDates([FromBody] DataTable dtholidaysdate)
        {
            GenericResult _authenticationResult = null;
            List<LookupDataContract> lstlookupDC = new List<LookupDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            UserPermissionLogic upl = new UserPermissionLogic();

            // insertholidays 
            upl.InsertHolidayDates(dtholidaysdate, new Guid(headerUserID));

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
                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }

        [HttpGet]
        [Route("api/permission/getallrules")]
        public IActionResult GetAllRules()
        {
            GenericResult _authenticationResult = null;
            DataTable dtjsontemplate = new DataTable();

            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            UserPermissionLogic upl = new UserPermissionLogic();
            var jsontemplatedata = upl.GetAllRules();
            try
            {
                if (jsontemplatedata != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "succeeded",
                        AllRulesList = jsontemplatedata,
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "failed"
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetAllJsonTemplate", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/permission/getContentByRuleTypeDetailID")]
        public IActionResult GetContentByRuleTypeDetailID([FromBody] string RuleTypeDetailID)
        {
            GenericResult _authenticationResult = null;
            DataTable dtjsontemplate = new DataTable();

            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            // public string GetContentByRuleTypeDetailID(int RuleTypeDetailID)
            UserPermissionLogic upl = new UserPermissionLogic();
            //  var jsontemplatedata = upl.GetContentByRuleTypeDetailID(Convert.ToInt32(RuleTypeDetailID));           
            //get data into blob 
            AzureStorageRead azstorage = new AzureStorageRead();
            var jsontemplatedata = azstorage.GetRuleJSONFileToAzureBlob(RuleTypeDetailID);
          //  var jsontemplatedata = Services.AzureStorageReadFile.GetRuleJSONFileToAzureBlob(RuleTypeDetailID);


            try
            {
                if (jsontemplatedata != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "succeeded",
                        RuleContent = jsontemplatedata,
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "failed"
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetAllJsonTemplate", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/permission/getjsontemplatebykey")]
        public IActionResult GetJsonTemplateByKey([FromBody] int KeyName)
        {
            GenericResult _authenticationResult = null;
            var headerUserID = string.Empty;
            UserPermissionLogic upl = new UserPermissionLogic();
            //string[] KeyNameList = KeyName.Split(" - ");
            //string key = KeyNameList[0];
            // string type = KeyNameList[1];

            JsonTemplate jtlist = upl.GetJsonTemplateByKey(KeyName);

            try
            {
                if (jtlist != null)
                {
                    string newtype = jtlist.Type;
                    // Value = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(newtype), Formatting.None);
                    // Value = Decompress(newtype);
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "succeeded",
                        JsonTemplateItem = newtype,
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "failed"
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
        [Route("api/permission/updatejsontemplate")]
        public IActionResult UpdateJsonTemplate([FromBody] DataTable data)
        {
            GenericResult _authenticationResult = null;
            List<RuleTypeDataContract> jsontemplatedata = new List<RuleTypeDataContract>();
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            UserPermissionLogic upl = new UserPermissionLogic();
            V1RulesLogic rl = new V1RulesLogic();
            if (data.Rows.Count > 0)
            {

                //Save JSON file into blob
                foreach (DataRow row in data.Rows)
                {
                    string FileName = row["DBFileName"].ToString();
                    string NewFileName = FileName.Replace(".json", "") + DateTime.Now.ToString("MM_dd_yyyy_HH_mm_ss") + ".json";
                    string strFileName = row["RuleTypeName"].ToString() + "_" + NewFileName;
                    string Content = row["Content"].ToString();
                    row["DBFileName"] = strFileName;
                    Services.AzureStorageReadFile.UploadRuleJSONFileToAzureBlob(Content, row["DBFileName"].ToString());
                }
                rl.InsertUpdateRuleTypeData(data, headerUserID);
                jsontemplatedata = upl.GetAllRules();
            }

            try
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "succeeded",
                    AllRulesList = jsontemplatedata,
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
        [Route("api/permission/getPowerBIPassword")]
        public IActionResult getPowerBIPassword()
        {
            GenericResult _authenticationResult = null;
            DataTable dtjsontemplate = new DataTable();

            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            AppConfigLogic app = new AppConfigLogic();
            List<AppConfigDataContract> appconfiglist = app.GetAppConfigByKey(new Guid(headerUserID), "PowerBIPassword");

            try
            {
                if (appconfiglist != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "succeeded",
                        appconfigpowerBI = appconfiglist.FirstOrDefault().Value,
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "failed"
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in getPowerBIPassword", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/permission/UpdatePowerBIPassword")]
        public IActionResult UpdatePowerBIPassword([FromBody] AppConfigDataContract _appconfigdatacontract)
        {
            GenericResult _authenticationResult = null;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            AppConfigLogic appconfiglogic = new AppConfigLogic();

            int result = appconfiglogic.UpdateAppConfigByKey(new Guid(headerUserID), _appconfigdatacontract);

            try
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "succeeded"
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


    }
}