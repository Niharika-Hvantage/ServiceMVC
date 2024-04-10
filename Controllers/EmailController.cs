using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using CRES.Utilities;
using CRES.DataContract;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using CRES.BusinessLogic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace CRES.Services.Controllers
{
    public class EmailController : ControllerBase
    {

        private readonly IEmailNotification _iEmailNotification;
        public EmailController(IEmailNotification iemailNotification)
        {
            _iEmailNotification = iemailNotification;
        }
        [Route("api/Email/SendEmailNotification")]
        [HttpGet]
        public IActionResult SendEmailNotification()
        {

            GenericResult _authenticationResult = null;
            try
            {
                //  EmailNotification emailNotification = new EmailNotification();
                _iEmailNotification.SendCalculationFailureNotification();
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Email sent succeeded",
                    Token = ""
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
            return Ok(_authenticationResult); ;

        }
        [Route("api/Email/GenericSchedular")]
        [HttpGet]
        public IActionResult GenericSchedular()
        {
            GetConfigSetting();
            string apiBaseURL = Sectionroot.GetSection("apiPath").Value;
            GenericResult _authenticationResult = null;
            List<SchedulerConfigDataContract> lstSchedulerConfig = null;
            SchedulerLogDataContract schedulerDC = null;
            SchedulerConfigDataContract schedulerConfigDC = null;
            try
            {
                UserPermissionLogic _userPermission = new UserPermissionLogic();

                //get all scheduler config
                SchedulerParamDataContract paramDC = new SchedulerParamDataContract();
                //get query string param
                string GroupID = HttpContext.Request.Query["GroupID"].ToString();
                if (!string.IsNullOrEmpty(GroupID))
                {
                    paramDC.GroupID = Convert.ToInt32(GroupID);
                }

                //

                paramDC.Status = 1;
                paramDC.ConfigFor = "Scheduler";
                lstSchedulerConfig = _userPermission.GetALLSchedulerConfig("", paramDC);
                //get all APIs which needs to be executed
                //lstSchedulerTobeExecute = lstSchedulerConfig.Where(i => i.NextexecutionTime <= DateTime.Now).ToList();
                foreach (SchedulerConfigDataContract obj in lstSchedulerConfig)
                {

                    System.Threading.Tasks.Task.Factory.StartNew(() =>
                    {
                        using (var client = new HttpClient())
                        {
                            int SchedulerLogID = 0;
                            try
                            {
                                //make an entry in  schedular log with start time
                                schedulerDC = new SchedulerLogDataContract { SchedulerConfigID = obj.SchedulerConfigID, StartTime = DateTime.Now, GeneratedBy = "System" };
                                SchedulerLogID = _userPermission.InsertUpdateSchedulerLog("", schedulerDC);

                                //set job status as Running
                                schedulerConfigDC = new SchedulerConfigDataContract { SchedulerConfigID = obj.SchedulerConfigID, JobStatus = "Running" };
                                _userPermission.UpdateSchedulerConfig("", schedulerConfigDC);

                                client.BaseAddress = new Uri(apiBaseURL);
                                //Call API
                                string APIName = obj.APIname;
                                if (obj.ObjectID != 0 && obj.ObjectID != null)
                                {
                                    APIName = APIName + "?ObjectID=" + obj.ObjectID;
                                }
                                var responseTask = client.GetAsync(APIName);
                                responseTask.Wait();

                                var result = responseTask.Result;
                                //update scheduler log End time
                                schedulerDC = new SchedulerLogDataContract { SchedulerLogID = SchedulerLogID, EndTime = DateTime.Now, GeneratedBy = "System" };
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
                                        schedulerConfigDC = new SchedulerConfigDataContract { SchedulerConfigID = obj.SchedulerConfigID, JobStatus = "Success" };
                                        _userPermission.UpdateSchedulerConfig("", schedulerConfigDC);
                                    }
                                    else
                                    {
                                        //update next execution time
                                        schedulerConfigDC = new SchedulerConfigDataContract { SchedulerConfigID = obj.SchedulerConfigID, JobStatus = "Failed" };
                                        _userPermission.UpdateSchedulerConfig("", schedulerConfigDC);
                                    }
                                }

                                else//error
                                {
                                    //update next execution time
                                    schedulerConfigDC = new SchedulerConfigDataContract { SchedulerConfigID = obj.SchedulerConfigID, JobStatus = "Failed" };
                                    _userPermission.UpdateSchedulerConfig("", schedulerConfigDC);
                                }
                            }
                            catch (Exception ex)
                            {
                                //update scheduler log End time
                                schedulerDC = new SchedulerLogDataContract { SchedulerLogID = SchedulerLogID, EndTime = DateTime.Now, GeneratedBy = "System" };
                                SchedulerLogID = _userPermission.InsertUpdateSchedulerLog("", schedulerDC);

                                //update next execution time
                                schedulerConfigDC = new SchedulerConfigDataContract { SchedulerConfigID = obj.SchedulerConfigID, JobStatus = "Failed" };
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

                }
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Scheduler executed successfully",
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.GenericScheduler.ToString(), "Error occurred  while running Generic Schedular by system.", "", "", ex.TargetSite.Name.ToString(), "", ex);

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

        //test method need to comment
        [Route("api/Email/TestGenericSchedulerException")]
        [HttpGet]
        public IActionResult TestGenericSchedulerException()
        {

            GenericResult _authenticationResult = null;
            try
            {
                int t = Convert.ToInt32("");
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Email sent succeeded",
                    Token = ""
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
            return Ok(_authenticationResult); ;

        }

        [Route("api/Email/TestGenericScheduler")]
        [HttpGet]
        public IActionResult TestGenericScheduler()
        {

            GenericResult _authenticationResult = null;
            try
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Email sent succeeded",
                    Token = ""
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
            return Ok(_authenticationResult); ;

        }

        [Route("api/Email/TestGenericSchedulerWithObjectID")]
        [HttpGet]
        public IActionResult TestGenericSchedulerWithObjectID(int ObjectID)
        {
            int paramObjectID = ObjectID;
            GenericResult _authenticationResult = null;
            try
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Email sent succeeded",
                    Token = ""
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
            return Ok(_authenticationResult); ;

        }

    }
}