using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Controllers;
using System.Diagnostics;
using System.Net;
using CRES.DataContract;
using CRES.ServicesNew.Controllers;
using CRES.BusinessLogic;
using CRES.Utilities;
using CRES.BusinessLogic;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CRES.Services.Controllers
{
    public class IsAuthenticate : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            GenericResultResponce _authenticationResult = null;

            IEnumerable<string> headerValues;
            var headerToken = string.Empty;
            var headerUserID = string.Empty;
            var delegateUserID = string.Empty;

            //for AI  API authentication
            if (Convert.ToString(context.HttpContext.Request.Headers["TokenUId"]).Contains("|"))
            {
                String[] splittedtoken = Convert.ToString(context.HttpContext.Request.Headers["TokenUId"]).Split("|");
                context.HttpContext.Request.Headers["TokenUId"] = splittedtoken[0];
                context.HttpContext.Request.Headers["Token"] = splittedtoken[1];

                // From AI not receiving delegate userid property
                context.HttpContext.Request.Headers["DelegatedUser"] = "00000000-0000-0000-0000-000000000000";
            }
            //end

            if (context.HttpContext.Request.Headers["TokenUId"] != DBNull.Value)
            {
                headerUserID = Convert.ToString(context.HttpContext.Request.Headers["TokenUId"]);
            }
            
            if (context.HttpContext.Request.Headers["DelegatedUser"] != DBNull.Value)
            {
                delegateUserID = Convert.ToString(context.HttpContext.Request.Headers["DelegatedUser"]);
            }

            

            if (delegateUserID == "")
            {
                delegateUserID = "00000000-0000-0000-0000-000000000000";
            }
            string DBToken = string.Empty;


            if (!string.IsNullOrEmpty(headerUserID))
            {
                DBToken = GetUserCredentialByUserID(new Guid(headerUserID), new Guid(delegateUserID));
                if (DBToken == "")
                {
                    //new code
                    _authenticationResult = new GenericResultResponce()
                    {
                        Succeeded = false,
                        Message = "Authentication failed"
                    };

                    //actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized, _authenticationResult);
                    context.Result = new JsonResult(_authenticationResult)
                    {
                        StatusCode = (int)HttpStatusCode.Unauthorized
                    };
                    return;
                }
            }

            
             if (context.HttpContext.Request.Headers["Token"]!= DBNull.Value)
              {
                headerToken = Convert.ToString(context.HttpContext.Request.Headers["Token"]);
                if (headerToken == DBToken)
                {
                    //actionContext.Request.Properties["AuthResult"] = "Authorized";
                }
                else
                {
                    //actionContext.Request.Properties["AuthResult"] = "Unauthorized";
                    //actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.BadRequest, modelState);
                    //ChallengeAuthRequest(actionContext);
                    //return;

                    _authenticationResult = new GenericResultResponce()
                    {
                        Succeeded = false,
                        Message = "Authentication failed"
                    };

                    context.Result = new JsonResult(_authenticationResult)
                    {
                        StatusCode = (int)HttpStatusCode.Unauthorized
                    };
                    //{
                    //    Content = _authenticationResult,
                    //    StatusCode = (int)HttpStatusCode.Unauthorized
                    //};

                    //Ok(_authenticationResult);
                    // context.HttpContext.Response = context.HttpContext.Request.CreateResponse(HttpStatusCode.Unauthorized, _authenticationResult);
                    return;
                }
            }
            else
            {
                _authenticationResult = new GenericResultResponce()
                {
                    Succeeded = false,
                    Message = "Authentication failed"
                };

                context.Result = new JsonResult(_authenticationResult)
                {
                    StatusCode = (int)HttpStatusCode.Unauthorized
                };
                return;
            }

            //base.OnActionExecuting(actionContext);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {

            //var objectContent = actionExecutedContext.Response.Content as ObjectContent;
            //if (objectContent != null)
            //{
            //    var type = objectContent.ObjectType; //type of the returned object
            //    var value = objectContent.Value; //holding the returned value
            //}
            //Debug.WriteLine("ACTION 1 DEBUG  OnActionExecuted Response " + actionExecutedContext.Response.StatusCode.ToString());
        }


        private static void ChallengeAuthRequest(ActionExecutedContext filterContext)
        {
            //var dnsHost = filterContext.Request.RequestUri.DnsSafeHost;
            //filterContext.Response = filterContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
            filterContext.Result = new UnauthorizedResult();
            return;
        }


        public string GetUserCredentialByUserID(Guid userid, Guid? delegatedUserID)
        {
            string _ret_token = "";
            string ServerName = "";
            UserDataContract _userdatacontract = new UserDataContract();
            UserLogic userlogic = new UserLogic();
            _userdatacontract = userlogic.GetUserCredentialByUserID(userid, delegatedUserID);

            if (_userdatacontract != null)
            {
                IConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
                var root = builder.Build();
                ServerName = root.GetSection("Application").GetSection("ServerName").Value;
                _ret_token = Encryptor.MD5Hash(ServerName +_userdatacontract.UserID + _userdatacontract.Password + _userdatacontract.UserToken);
            }
            return _ret_token;
        }

    }
}