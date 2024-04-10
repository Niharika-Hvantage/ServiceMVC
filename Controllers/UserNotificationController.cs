using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using CRES.BusinessLogic;
using CRES.DataContract;
using System;
using Microsoft.AspNetCore.Mvc;
using CRES.BusinessLogic;

namespace CRES.Services.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class UserNotificationController : ControllerBase
    {

        //[HttpPost]
        //[Services.Controllers.IsAuthenticate]
        //[Route("api/notification/getallnotifications")]
        //public IActionResult GetAllNotifications([FromBody] string dt)
        //{
        //    GenericResult _authenticationResult = null;
        //    int pageindex = 1;
        //    List<UserNotificationDataContract> _lstUserNotification = new List<UserNotificationDataContract>();

        //    UserNotificationLogic _userNotifilogic = new UserNotificationLogic();

        //    IEnumerable<string> headerValues;
        //    var headerUserID = new Guid();
        //    if (Request.Headers.TryGetValues("TokenUId", out headerValues))
        //    {
        //        headerUserID = new Guid(headerValues.FirstOrDefault());
        //    }
            
        //   _lstUserNotification = _userNotifilogic.GetUserNotification(headerUserID.ToString(),dt, pageindex);


        //   // var notificount = _lstUserNotification.Where(x => x.ViewedTime == null).Count();

        //    if (_lstUserNotification != null)
        //    {
        //        _authenticationResult = new GenericResult()
        //        {
        //            Succeeded = true,
        //            Message = "succeeded",
        //            lstUserNotification = _lstUserNotification
                   
        //        };
        //    }
        //    return Ok(_authenticationResult);
        //}
        

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/notification/clearnotifications")]
        public IActionResult ClearNotification([FromBody] Guid Notificationid)
        {
            GenericResult _authenticationResult = null;

            bool result;

            UserNotificationLogic _userNotifilogic = new UserNotificationLogic();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            result = _userNotifilogic.ClearNotification(Notificationid, headerUserID.ToString());

            if (result == true)
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "succeeded"
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
            return Ok(_authenticationResult);
        }


        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/notification/clearallnotifications")]
        public IActionResult ClearAllNotification()
        {
            GenericResult _authenticationResult = null;
            bool result;

            UserNotificationLogic _userNotifilogic = new UserNotificationLogic();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            result = _userNotifilogic.ClearAllNotification(headerUserID.ToString());

            if (result == true)
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "succeeded"
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
            return Ok(_authenticationResult);
        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/notification/clearallnotificationscount")]
        public IActionResult ClearAllNotificationCount()
        {
            GenericResult _authenticationResult = null;
            bool result;

            UserNotificationLogic _userNotifilogic = new UserNotificationLogic();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            result = _userNotifilogic.ClearAllNotificationCount(headerUserID.ToString());

            if (result == true)
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "succeeded"
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
            return Ok(_authenticationResult);
        }




        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/notification/getusernotificationscount")]
        public IActionResult GetUserNotificationCount()
        {
            GenericResult _authenticationResult = null;
           
            List<UserNotificationDataContract> _lstUserNotification = new List<UserNotificationDataContract>();

            UserNotificationDataContract UserNotifiDC = new UserNotificationDataContract();
            UserNotificationLogic _userNotifilogic = new UserNotificationLogic();
           // int result = 0;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }


            //  _lstUserNotification = _userNotifilogic.GetUserNotification(headerUserID.ToString());

            UserNotifiDC = _userNotifilogic.GetUserNotificationCount(headerUserID.ToString());
           // var notificount = _lstUserNotification.Where(x => x.ViewedTime == null).Count();
            if (_lstUserNotification != null)
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    TotalCount= UserNotifiDC.TotalCount,
                    exceptioncount = UserNotifiDC.CurrentCount
                };
            }
            
            return Ok(_authenticationResult);
        }

        [HttpGet]
        //  [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/notification/notificationsubscription")]
        public IActionResult GetNotificationSubscriptionListByUserId(int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            List<NotificationSubscriptionDataContract> lstSubscription = new List<NotificationSubscriptionDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            UserNotificationLogic notificationLogic = new UserNotificationLogic();
            int? totalCount;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                lstSubscription = notificationLogic.GetNotificationSubscriptionListByUserId(headerUserID, pageIndex, pageSize, out totalCount);
            }
            try
            {
                if (lstSubscription != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstSubscription = lstSubscription,

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
        [Route("api/notification/addupdatenotificationsubscription")]
        public IActionResult AddUpdateNotificationSubscription([FromBody] List<NotificationSubscriptionDataContract> _notificationSubscriptionDC)
        {
            GenericResult _actionResult = null;
            List<NotificationSubscriptionDataContract> _lstNotificationSubscription = new List<NotificationSubscriptionDataContract>();


            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }


            UserNotificationLogic _notificationLogic = new UserNotificationLogic();
            bool result = _notificationLogic.AddUpdateNotificationSubscription(new Guid(headerUserID), _notificationSubscriptionDC, headerUserID, headerUserID);

            try
            {
                if (result)
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Subscription updated successfully",
                    };
                }
                else
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Updation failed",
                    };
                }
            }
            catch (Exception ex)
            {
                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_actionResult);
        }

        
        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/notification/allnotification")]
        public IActionResult GetAllNotification(int pageIndx, int pageSize, string gId)
      //  public IActionResult GetAllNotification(string dt)
        {
            GenericResult _authenticationResult = null;
          //  int pageIndx = 1, pageSize = 30;
            List<UserNotificationDataContract> _lstUserNotification = new List<UserNotificationDataContract>();
            IEnumerable<string> headerValues;
            int? totalCount = 0;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            UserNotificationLogic notificationLogic = new UserNotificationLogic();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                _lstUserNotification = notificationLogic.GetAllUserNotification(headerUserID.ToString(), gId, pageIndx, pageSize,out totalCount);
            }
            try
            {
                if (_lstUserNotification != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstUserNotification = _lstUserNotification,
                        TotalCount = Convert.ToInt32(totalCount)

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