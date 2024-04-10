using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using CRES.DataContract;
using CRES.BusinessLogic;
using CRES.Utilities;
using Microsoft.AspNetCore.Mvc;
using CRES.BusinessLogic;

namespace CRES.Services.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class FeeConfigurationController : ControllerBase
    {

        
        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/feeconfiguration/addupdatefeefunctionsconfig")]
        public IActionResult AddupdateFeeFunctionsConfig([FromBody] List<FeeFunctionsConfigDataContract> _feefunctiondc)
        {
            GenericResult _actionResult = null;
            IEnumerable<string> headerValues;
            string Validationmessage = string.Empty;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            try
            {

                FeeConfigurationLogic _feeLogic = new FeeConfigurationLogic();
                _feeLogic.SaveFeeFunctionsConfig(new Guid(headerUserID), _feefunctiondc);
                _actionResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Fee function config saved successfully",
                };
                
            }
            catch (Exception ex)
            {
                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ExceptionHelper.GetFullMessage(ex)
                };
            }
           
            return Ok(_actionResult);
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/feeconfiguration/addupdatefeeschedulesconfig")]
        public IActionResult AddupdateFeeSchedulesConfig([FromBody] List<FeeSchedulesConfigDataContract> _feescheduledc)
        {
            GenericResult _actionResult = null;
            IEnumerable<string> headerValues;
            string Validationmessage = string.Empty;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            try
            {

                FeeConfigurationLogic _feeLogic = new FeeConfigurationLogic();
                _feeLogic.SaveFeeSchedulesConfig(new Guid(headerUserID), _feescheduledc);
                _actionResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Fee schedule config saved successfully",
                };

            }
            catch (Exception ex)
            {
                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ExceptionHelper.GetFullMessage(ex)
                };
            }

            return Ok(_actionResult);
        }


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/feeconfiguration/addupdatefeeconfig")]
        public IActionResult AddupdateFeeConfig([FromBody] FeeConfigDataContract feeConfigDC)
        {
            GenericResult _actionResult = null;
            IEnumerable<string> headerValues;
            string Validationmessage = string.Empty;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            try
            {

                FeeConfigurationLogic _feeLogic = new FeeConfigurationLogic();
                if (feeConfigDC != null && feeConfigDC.lstFeeFunctionsConfig!=null && feeConfigDC .lstFeeFunctionsConfig.Count()> 0)
                {
                    _feeLogic.SaveFeeFunctionsConfig(new Guid(headerUserID), feeConfigDC.lstFeeFunctionsConfig);
                }
                if (feeConfigDC != null && feeConfigDC.lstFeeSchedulesConfig != null && feeConfigDC.lstFeeSchedulesConfig.Count() > 0)
                {
                    _feeLogic.SaveFeeSchedulesConfig(new Guid(headerUserID), feeConfigDC.lstFeeSchedulesConfig);
                }
                _actionResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Fee config saved successfully",
                };

            }
            catch (Exception ex)
            {
                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ExceptionHelper.GetFullMessage(ex)
                };
            }

            return Ok(_actionResult);
        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/feeconfiguration/getallfeefunctionsconfig")]
        public IActionResult GetAllFeefunctionsConfig()
        {
            GenericResult _authenticationResult = null;
            List<FeeFunctionsConfigDataContract> _lstFeeFunctions = new List<FeeFunctionsConfigDataContract>();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            FeeConfigurationLogic feeLogic = new FeeConfigurationLogic();
            UserPermissionLogic upl = new UserPermissionLogic();
            //List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "Deallist");

            //if (permissionlist.Count > 0)
            //{
                _lstFeeFunctions = feeLogic.GetFeeFunctionsConfig(headerUserID);
            //}

            try
            {
                if (_lstFeeFunctions != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstFeeFunctionsConfig = _lstFeeFunctions
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
        [Route("api/feeconfiguration/getallfeeschedulesconfig")]
        public IActionResult GetAllFeeSchedulesConfig()
        {
            GenericResult _authenticationResult = null;
            List<FeeSchedulesConfigDataContract> _lstFeeSchedules = new List<FeeSchedulesConfigDataContract>();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            FeeConfigurationLogic feeLogic = new FeeConfigurationLogic();
            UserPermissionLogic upl = new UserPermissionLogic();
            //List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "Deallist");

            //if (permissionlist.Count > 0)
            //{
            _lstFeeSchedules = feeLogic.GetFeeSchedulesConfig(headerUserID);
            //}

            try
            {
                if (_lstFeeSchedules != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstFeeSchedulesConfig= _lstFeeSchedules
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
        [Route("api/feeconfiguration/deletefeeschedulesconfig")]
        public IActionResult DeleteFeeSchedulesConfigByID([FromBody] Guid FeeTypeGuID)
        {
            GenericResult _actionResult = null;
            IEnumerable<string> headerValues;
            string Validationmessage = string.Empty;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            try
            {
                FeeConfigurationLogic _feeLogic = new FeeConfigurationLogic();
                 _feeLogic.DeleteFeeSchedulesConfigByID(new Guid(headerUserID), FeeTypeGuID);
                _actionResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Fee schedule config deleted successfully",
                };

            }
            catch (Exception ex)
            {
                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ExceptionHelper.GetFullMessage(ex)
                };
            }

            return Ok(_actionResult);
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/feeconfiguration/deletefeefunctionsconfig")]
        public IActionResult DeleteFeeFunctionsConfigByID([FromBody] Guid FunctionGuID)
        {
            GenericResult _actionResult = null;
            IEnumerable<string> headerValues;
            string Validationmessage = string.Empty;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            try
            {
                FeeConfigurationLogic _feeLogic = new FeeConfigurationLogic();
                _feeLogic.DeleteFeeFunctionsConfigByID(new Guid(headerUserID), FunctionGuID);
                _actionResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Fee function config deleted successfully",
                };

            }
            catch (Exception ex)
            {
                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ExceptionHelper.GetFullMessage(ex)
                };
            }

            return Ok(_actionResult);
        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/feeconfiguration/getpayruledropdownfeeschedules")]
        public IActionResult GetPayRuleDropDownFeeSchedules()
        {
            GenericResult _authenticationResult = null;
            List<FeeSchedulesConfigDataContract> _lstFeeSchedules = new List<FeeSchedulesConfigDataContract>();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            FeeConfigurationLogic feeLogic = new FeeConfigurationLogic();
            UserPermissionLogic upl = new UserPermissionLogic();
            
            _lstFeeSchedules = feeLogic.GetPayRuleDropDownFeeSchedules(headerUserID);
            

            try
            {
                if (_lstFeeSchedules != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstFeeSchedulesConfig = _lstFeeSchedules
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
