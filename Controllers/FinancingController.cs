using CRES.BusinessLogic;
using CRES.BusinessLogic;
using CRES.DataContract;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace CRES.Services.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class FinancingController : ControllerBase
    {
        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/account/GetFinancingWarehouse")]
        public IActionResult GetFinancingWarehouse(int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            List<FinancingWarehouseDataContract> _lstFinancingWarehouseDataContract = new List<FinancingWarehouseDataContract>();

            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            int? totalCount=0;

            UserPermissionLogic upl = new UserPermissionLogic();
            //to get user
            List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "Financing");
            if (permissionlist != null && permissionlist.Count > 0)
            {
                FinancingLogic financingLogic = new FinancingLogic();

                _lstFinancingWarehouseDataContract = financingLogic.GetFinancingWarehouse(new Guid(headerUserID), pageSize, pageIndex, out totalCount);
            }

            try
            {
                if (_lstFinancingWarehouseDataContract != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        lstFinancingWarehouseDataContract = _lstFinancingWarehouseDataContract,
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
        [Services.Controllers.DeflateCompression]
        [Services.Controllers.IsAuthenticate]
        [Route("api/note/addUpdateFinancingWarehouse")]
        public IActionResult AddUpdateFinancingWarehouse([FromBody]FinancingWarehouseDataContract FinancingWarehousedc)
        {
            GenericResult _actionResult = null;

            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            FinancingWarehousedc.CreatedBy = headerUserID;
            FinancingWarehousedc.UpdatedBy = headerUserID;
            FinancingLogic financingLogic = new FinancingLogic();

            string result = financingLogic.AddUpdateFinancingWarehouse(FinancingWarehousedc).ToString();
            try
            {
                if (result != "")
                {
                    _actionResult = new GenericResult()
                    {
                        newFinancingWarehouseid = result,
                        Succeeded = true,
                        Message = "Changes were saved successfully.",
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

        [HttpPost]
        [Services.Controllers.DeflateCompression]
        [Services.Controllers.IsAuthenticate]
        [Route("api/note/addUpdateFinancingWarehouseDetails")]
        public IActionResult AddUpdateFinancingWarehouseDetails([FromBody]List<FinancingWarehouseDetailDataContract> warehouseDetailDataContracts)
        {
            GenericResult _actionResult = null;

            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            FinancingLogic financingLogic = new FinancingLogic();

            string result = financingLogic.AddUpdateFinancingWarehouseDetails(warehouseDetailDataContracts, headerUserID).ToString();
            try
            {
                if (result != "")
                {
                    _actionResult = new GenericResult()
                    {
                        newFinancingWarehouseid = result,
                        Succeeded = true,
                        Message = "Changes were saved successfully.",
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

        // public FinancingWarehouseDataContract GetFinancingWarehouseByid(string financingWarehouseID)
        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/getFinancingWarehouseById")]
        public IActionResult GetFinancingWarehouseByid([FromBody]FinancingWarehouseDataContract FinancingWarehousedc)
        {
            GenericResult _actionResult = null;
            FinancingWarehouseDataContract FinancingWarehousedatacontract = new FinancingWarehouseDataContract();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            FinancingLogic financingLogic = new FinancingLogic();
            if (FinancingWarehousedc.FinancingWarehouseID != null)
            {
                FinancingWarehousedatacontract = financingLogic.GetFinancingWarehouseByid(FinancingWarehousedc.FinancingWarehouseID.ToString());
                try
                {
                    if (FinancingWarehousedc != null)
                    {
                        _actionResult = new GenericResult()
                        {
                            FinancingWarehouseDataContract = FinancingWarehousedatacontract,
                            Succeeded = true,
                            Message = "Changes were saved successfully.",
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
            }
            return Ok(_actionResult);
        }
    }
}