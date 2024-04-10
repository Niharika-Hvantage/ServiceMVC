using CRES.BusinessLogic;
using CRES.BusinessLogic;
using CRES.DataContract;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

namespace CRES.Services.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class TaskManagementController : ControllerBase
    {
        // GET: TaskManagement


        private readonly IEmailNotification _iEmailNotification;
        public TaskManagementController(IEmailNotification iemailNotification)
        {
            _iEmailNotification = iemailNotification;
        }

        private TaskManagementLogic _taskManagementLogic = new TaskManagementLogic();

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/taskmanagement/getallLookup")]
        public IActionResult GetAllLookup()
        {
            string getAllLookup = "57,58,59,60";
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

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/taskmanagement/insertupdatetask")]
        public IActionResult InsertUpdateTask([FromBody]TaskManagementDataContract TaskDc)
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            string updatedbytext = "";

            updatedbytext = TaskDc.UpdatedBy;

            TaskDc.CreatedBy = headerUserID;
            TaskDc.UpdatedBy = headerUserID;

            if (TaskDc.AssignedTo == "")
            {
                TaskDc.AssignedTo = null;
            }
            TaskActivityHelper tah = new TaskActivityHelper();
            List<TaskManagementDataContract> activitylist = tah.CaptureActivity(TaskDc);

            TaskManagementLogic taskManagementLogic = new TaskManagementLogic();
            string res = taskManagementLogic.InsertUpdateTask(TaskDc, headerUserID);

            if (res != "")
            {
                TaskDc.TaskID = res;

                if (activitylist != null && activitylist.Count > 0)
                {
                    taskManagementLogic.InsertTaskActivity(activitylist, headerUserID, res);
                    var msg = activitylist.Find(x => x.ActivityType == 386);

                    if (msg != null)
                    {
                        Thread FirstThread = new Thread(() => SendEmailForTaskAssigned(TaskDc, updatedbytext));
                        FirstThread.Start();
                    }

                    var newtask = activitylist.Find(x => x.ActivityType == 381);
                    if (newtask != null)
                    {
                        Thread FirstThread = new Thread(() => SendEmailForNewuser(TaskDc));
                        FirstThread.Start();
                    }
                }


            }
            try
            {
                if (headerUserID != null)
                {
                    if (res != "FALSE")
                    {
                        _authenticationResult = new GenericResult()
                        {
                            newtaskID = res,
                            Succeeded = true,
                            Message = "Changes were saved successfully.",
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
        [Route("api/taskmanagement/getalltask")]
        public IActionResult GetAllTask([FromBody] TaskManagementDataContract _task, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            List<TaskManagementDataContract> _lsttask = new List<TaskManagementDataContract>();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            ///   pageIndex=
            //378	59	Closed

            TaskManagementLogic tasklogic = new TaskManagementLogic();
            int? totalCount = 0;
            _lsttask = tasklogic.GetAllTask(Convert.ToInt32(_task.Status), headerUserID, pageSize, pageIndex, out totalCount);

            try
            {
                if (_lsttask != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        lstTask = _lsttask
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
        [Route("api/taskmanagement/taskbytaskid")]
        public IActionResult TaskByTaskID([FromBody] string TaskID)
        {
            GenericResult _authenticationResult = null;
            TaskManagementDataContract taskdc = new TaskManagementDataContract();
            IEnumerable<string> headerValues;

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            TaskManagementLogic tasklogic = new TaskManagementLogic();
            taskdc = tasklogic.GetTaskBYTaskID(TaskID);

            try
            {
                if (taskdc != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TaskData = taskdc
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
        [Route("api/taskmanagement/insertupdatetaskcomment")]
        public IActionResult InsertUpdateTaskComment([FromBody] TaskCommentDataContract commentsDC)
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            string usernametext = "";
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            usernametext = commentsDC.CreatedBy;
            commentsDC.CreatedBy = headerUserID;
            commentsDC.UpdatedBy = headerUserID;
            TaskManagementLogic taskManagementLogic = new TaskManagementLogic();
            string res = taskManagementLogic.InsertUpdateTaskComment(commentsDC);

            if (res == "TRUE")
            {
                List<UserDataContract> user = taskManagementLogic.GetSubscribedUserEmailIDsByTaskID(commentsDC.TaskID.ToString(), headerUserID);
                Thread FirstThread = new Thread(() => SendCommentNotificationtouser(user, commentsDC.TaskID.ToString(), commentsDC.TaskSummary, commentsDC.Comments, usernametext));
                FirstThread.Start();
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
        [Route("api/taskmanagement/commentsbytaskid")]
        public IActionResult GetTaskCommentsByTaskId([FromBody]TaskCommentDataContract commentsDC)
        {
            GenericResult _authenticationResult = null;
            List<TaskCommentDataContract> lsttaskcommentdc = new List<TaskCommentDataContract>();
            IEnumerable<string> headerValues;
            //  string currentTime = "2017-08-09";
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            commentsDC.CommentType = "All";
            TaskManagementLogic tasklogic = new TaskManagementLogic();
            lsttaskcommentdc = tasklogic.GetTaskCommentsByTaskId(new Guid(headerUserID),commentsDC.TaskID.ToString(), commentsDC.Currentdate, commentsDC.CommentType);

            try
            {
                if (lsttaskcommentdc != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstTaskComment = lsttaskcommentdc
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
        [Route("api/taskmanagement/getsubscribeduserbytaskid")]
        public IActionResult GetSubscribedUserByTaskID([FromBody] string TaskID)
        {
            GenericResult _authenticationResult = null;
            List<TaskSubscriptionDataContract> lstSubscribeduser = new List<TaskSubscriptionDataContract>();
            IEnumerable<string> headerValues;

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            TaskManagementLogic tasklogic = new TaskManagementLogic();
            lstSubscribeduser = tasklogic.GetSubscribedUserByTaskID(TaskID);

            try
            {
                if (lstSubscribeduser != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstSubscribeduser = lstSubscribeduser
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
        [Route("api/taskmanagement/insertsubscriptiondata")]
        public IActionResult InsertSubscriptionData([FromBody]List<TaskSubscriptionDataContract> lstsubdata)
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            lstsubdata = lstsubdata.Where(x => x.SubscriptionStatus == true).ToList();
            TaskManagementLogic taskManagementLogic = new TaskManagementLogic();
            string res = taskManagementLogic.InsertSubscriptionData(lstsubdata, headerUserID);
            try
            {
                if (headerUserID != null)
                {
                    if (res != "FALSE")
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "Changes were saved successfully.",
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

        public void SendEmailForNewuser(TaskManagementDataContract TaskDc)
        {
            string userid = "";
            if (TaskDc.AssignedTo == null || TaskDc.AssignedTo == "")
            {
                TaskManagementLogic taskManagementLogic = new TaskManagementLogic();
                userid = taskManagementLogic.GetTaskDefaultConfigByTaskType(TaskDc.TaskType);
            }
            else
            {
                userid = TaskDc.AssignedTo;
            }

            if (userid != null && userid != "")
            {

                UserLogic ul = new UserLogic();
                UserDataContract udc = ul.GetUserCredentialByUserID(new Guid(userid), new Guid());
                //   EmailNotification en = new EmailNotification();
                _iEmailNotification.SendNewTaskNotification(udc, TaskDc);
            }
        }

        public void SendCommentNotificationtouser(List<UserDataContract> userlist, string taskid, string tasksummary, string comment, string username)
        {
            foreach (UserDataContract udc in userlist)
            {
                if (udc.Email != null)
                {
                    //  EmailNotification en = new EmailNotification();
                    _iEmailNotification.SendCommentNotificationtouser(udc, tasksummary, taskid, comment, username);
                }
            }
        }

        public void SendEmailForTaskAssigned(TaskManagementDataContract TaskDc, string username)
        {
            string userid = "";
            if (TaskDc.AssignedTo != null && TaskDc.AssignedTo != "")
            {
                userid = TaskDc.AssignedTo;
                UserLogic ul = new UserLogic();
                UserDataContract udc = ul.GetUserCredentialByUserID(new Guid(userid), new Guid());
                // EmailNotification en = new EmailNotification();
                _iEmailNotification.SendTaskAssignedNotification(udc, TaskDc, username);
            }
        }


        [HttpPost]
        //[Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/taskmanagement/managetask")]
        public IActionResult ManageTask([FromBody]WorkflowDataContract WorkDc)
        {
            GenericResult _authenticationResult = null;

           

            try
            {
                _authenticationResult = new GenericResult()
                {
                    //TotalCount = WorkDc.FirstTaskTime + WorkDc.SecondTaskTime,
                    Succeeded = true,
                    Message = "Task updateded successfully.",
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
            //var javaScriptSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            //string jsonString = javaScriptSerializer.Serialize(_authenticationResult);
            //var json = new JavaScriptSerializer().Serialize(_authenticationResult);
            //Request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            return Ok(_authenticationResult);
            //    return Ok(_authenticationResult);
        }


    }
}