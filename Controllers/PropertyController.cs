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


namespace CRES.Services.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class PropertyController : ControllerBase
    {
        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/property/getallproperty")]
        public IActionResult GetAllProperty([FromBody] PropertyDataContract _propertyDC, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            List<PropertyDataContract> _lstproperty = new List<PropertyDataContract>();
            PropertyLogic _propertyLogic = new PropertyLogic();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            int? totalCount = 0;

            UserPermissionLogic upl = new UserPermissionLogic();
            //to get user
            List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "Properties");
            if (permissionlist != null && permissionlist.Count > 0)
            {
                _lstproperty = _propertyLogic.getAllProperty(_propertyDC.Deal_DealID, headerUserID, pageSize, pageIndex, out totalCount);
            }

            try
            {
                if (_lstproperty != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        lstProperty = _lstproperty,
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

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/property/updateProperty")]
        public IActionResult UpdateProperty([FromBody] List<PropertyDataContract> _propertyDC)
        {
            GenericResult _actionResult = null;
            List<PropertyDataContract> _lstproperty = new List<PropertyDataContract>();

            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            PropertyLogic _propertyLogic = new PropertyLogic();
            _propertyDC = _propertyDC.FindAll(y => y.PropertyName != null && y.PropertyName.Trim()!="" ).ToList();             

            bool result = _propertyLogic.UpdateProperty(new Guid(headerUserID), _propertyDC, headerUserID, headerUserID);
             
            try
            {
                if (result)
                {
                    

                    _actionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Property Updated successfully",                        
                    };
                }
                else
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Updation failed",
                        // Token = Encryptor.MD5Hash(_userDataContract.Login + EncruptOldPassword)
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

            //var javaScriptSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            //string jsonString = javaScriptSerializer.Serialize(_actionResult);

            //var json = new JavaScriptSerializer().Serialize(_actionResult);
            ////JavaScriptSerializer js = new JavaScriptSerializer();
            ////string returnstring = js.Serialize(_authenticationResult);

            //Request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            return Ok(_actionResult);
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/property/getpropertybypropertyid")]
        public IActionResult GetPropertyByPropertyId([FromBody]PropertyDataContract _propertyDC)
        {
            GenericResult _authenticationResult = null;
            PropertyDataContract _property = new PropertyDataContract();
            PropertyLogic _propertyLogic = new PropertyLogic();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            _property = _propertyLogic.GetPropertyById(_propertyDC.PropertyID.ToString());

            try
            {
                if (_property != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        propertyDataContract = _property
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