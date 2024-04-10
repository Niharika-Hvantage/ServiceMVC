using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using CRES.BusinessLogic;
using CRES.DataContract;
using CRES.Utilities;
using System;
using Microsoft.AspNetCore.Mvc;
using CRES.BusinessLogic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;

namespace CRES.Services.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class IndexTypeController : ControllerBase
    {
        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/account/GetIndexType")]
        public IActionResult GetIndexTypeByDate([FromBody]IndexTypeDataContract indextypeDc)
        {
            GenericResult _actionResult = null;
            DataTable IndexTypedatatable = new DataTable();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            UserPermissionLogic upl = new UserPermissionLogic();
            //to get user 
            List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "Deallist"); //Indexes 
            if (permissionlist != null && permissionlist.Count > 0)
            {
                IndexTypeLogic indexTypeLog = new IndexTypeLogic();
                IndexTypedatatable = indexTypeLog.GetIndexTypeByDate(indextypeDc);
            }

            try
            {
                if (IndexTypedatatable != null)
                {
                    _actionResult = new GenericResult()
                    {
                        dtIndexType = IndexTypedatatable,
                        TotalCount = IndexTypedatatable.Rows.Count,
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        UserPermissionList = permissionlist
                    };
                }
                else
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed",
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
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/account/AddUpdateIndexType")]
        public IActionResult AddUpdateIndexType([FromBody]DataTable dt)
        {
            GenericResult _actionResult = null;
            int res = 0;

            IEnumerable<string> headerValues;

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            IndexTypeLogic indexTypeLog = new IndexTypeLogic();
            if (dt != null)
            {
                res = indexTypeLog.AddUpdateIndexes(dt, headerUserID, headerUserID);
            }

            try
            {
                if (res != 0)
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                    };
                }
                else
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed",
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
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/indextype/addupdateindextypeList")]
        public IActionResult AddUpdateIndexTypeList([FromBody]DataTable dt)
        {
            GenericResult _actionResult = null;
            int res = 0;
            IEnumerable<string> headerValues;

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            IndexTypeLogic indexTypeLog = new IndexTypeLogic();
            if (dt != null)
            {
                if (dt.Rows.Count > 0)
                {
                    res = indexTypeLog.AddUpdateIndexList(dt, headerUserID, headerUserID);
                }
            }
              

            try
            {
                if (res != 0)
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                    };
                }
                else
                {

                    if (dt.Rows.Count == 0)
                    {
                        _actionResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "Authentication succeeded",
                        };

                    }
                    else
                    {
                        _actionResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = "Authentication failed",
                        };
                    }
                   
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
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/indextype/getindexesdetailbyindexesmaster")]
        public IActionResult GetIndexesDetailByIndexesMaster([FromBody] IndexesMasterDataContract IndexDC)
        {
            GenericResult _authenticationResult = null;
            IndexesMasterDataContract _indexDC = new IndexesMasterDataContract();
            IEnumerable<string> headerValues;

            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            UserPermissionLogic upl = new UserPermissionLogic();
            IndexTypeLogic indexLogic = new IndexTypeLogic();

            //to get user
            List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "IndexesPage", IndexDC.IndexesMasterGuid.ToString() == "00000000-0000-0000-0000-000000000000" ? IndexDC.IndexesMasterGuid.ToString() : IndexDC.IndexesMasterGuid.ToString(), 283); // IndexDetail
            if (permissionlist != null && permissionlist.Count > 0)
            {
                _indexDC = indexLogic.GetIndexesMasterDetailByIndexesMaster(IndexDC.IndexesMasterGuid.ToGuid(), headerUserID.ToString());
            }

            try
            {
                if (_indexDC != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        indexesMaster = _indexDC,
                        UserPermissionList = permissionlist,
                        StatusCode = 200
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Not Exists",
                        StatusCode = 404
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
        [Route("api/indextype/insertupdateindexesmasterdetail")]
        public IActionResult InsertUpdateIndexesMasterDetail([FromBody]IndexesMasterDataContract IndexDC)
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            IndexTypeLogic indexLogic = new IndexTypeLogic();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            try
            {
                IndexDC.CreatedBy = headerUserID;
                IndexDC.UpdatedBy = headerUserID;
                DealLogic dealLogic = new DealLogic();
                PayruleSetupLogic psl = new PayruleSetupLogic();
                string res = indexLogic.InsertUpdateIndexesMasterDetail(IndexDC);
              

                string message = "Changes were saved successfully.";

                if (headerUserID != null)
                {
                    if (res != "FALSE")
                    {
                       _authenticationResult = new GenericResult()
                        {
                            newIndexesMasterGuid = res,
                            Succeeded = true,
                            Message = message
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
                string msg = ex.Message;
                msg = msg.Replace("'", "''");
                Logger.Write("Error occurred  while saving IndexesMasterDetail : IndexesMasterGuid " + IndexDC.IndexesMasterGuid + " " + msg, MessageLevel.Error);
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
        [Route("api/indextype/GetIndexesFromIndexesMaster")]
        public IActionResult GetIndexesFromIndexesMaster()
        {
            GenericResult _actionResult = null;
            List<IndexesMasterDataContract> lstIndexesMaster = new List<IndexesMasterDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            UserPermissionLogic upl = new UserPermissionLogic();
            //to get user 
            List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "Deallist"); //Indexes
            if (permissionlist != null && permissionlist.Count > 0)
            {
                IndexTypeLogic indexTypeLog = new IndexTypeLogic();
                lstIndexesMaster = indexTypeLog.GetIndexesFromIndexesMaster();
            }

            try
            {
                if (lstIndexesMaster.Count > 0)
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstIndexesMaster = lstIndexesMaster
                    };
                }
                else
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed",
                        lstIndexesMaster = null
                    };
                }
            }
            catch (Exception ex)
            {
                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message,
                    lstIndexesMaster = null
                };
            }
            return Ok(_actionResult);
        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/indextype/getallindexesmaster")]
        public IActionResult GetAllIndexesMaster(int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            List<IndexesMasterDataContract> _lstIndexMaster = new List<IndexesMasterDataContract>();
            IndexTypeLogic indexTypeLog = new IndexTypeLogic();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            DealLogic dealLogic = new DealLogic();
            int? totalCount = 0;

            UserPermissionLogic upl = new UserPermissionLogic();
            List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "Indexes"); // Indexeslist

            if (permissionlist.Count > 0)
            {
                _lstIndexMaster = indexTypeLog.GetAllIndexesMaster(headerUserID, pageSize, pageIndex, out totalCount);
            }

            try
            {
                if (_lstIndexMaster != null)
                {
                    //Logger.Write(headerUserID.ToString(), "Index list loaded successfully", MessageLevel.Info);
                    Logger.Write("Index list loaded successfully", MessageLevel.Info, headerUserID.ToString());
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = CommonHelper.ToInt32(totalCount),
                        lstIndexesMaster = _lstIndexMaster,
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
                Logger.Write("Error in loading all Index detail", ex);

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
        [Route("api/indextype/checkduplicateindexesname")]
        public IActionResult CheckDuplicateIndexesName([FromBody] IndexesMasterDataContract indexesDC)
        {
            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            IndexTypeLogic indexTypeLog = new IndexTypeLogic();
            bool res = indexTypeLog.CheckDuplicateIndexesName(indexesDC.IndexesMasterGuid.ToString(), indexesDC.IndexesName);
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
        [Route("api/indextype/addupdateindextypefromscenario")]
        public IActionResult AddUpdateIndexTypeFromScenario([FromBody]DataTable dt)
        {
            GenericResult _actionResult = null;
            int res = 0;

            IEnumerable<string> headerValues;

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            IndexTypeLogic indexTypeLog = new IndexTypeLogic();
            if (dt != null)
            {
                if (dt.Rows.Count > 0)
                {
                    res = indexTypeLog.AddUpdateIndexes(dt, headerUserID, headerUserID);
                }
            }

            try
            {
                if (res != 0)
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                    };
                }
                else
                {

                    if (dt.Rows.Count == 0)
                    {
                        _actionResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "Authentication succeeded",
                        };

                    }
                    else
                    {
                        _actionResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = "Authentication failed",
                        };
                    }

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
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/indextype/getindexesexportdatabyindexesmasterid")]
        public IActionResult GetIndexesExportDataByIndexesMasterID([FromBody] IndexesMasterSearchDataContract _indexesDC)
        {
            GenericResult _authenticationResult = null;
            DataTable IndexTypedatatable = new DataTable();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            IndexTypeLogic indexTypeLog = new IndexTypeLogic();
            int? totalCount;
            IndexTypedatatable = indexTypeLog.GetIndexesExportDataByIndexesMasterID(_indexesDC, headerUserID, out totalCount);
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

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/indextype/getindexlistbyindexesMasterID")]
        public IActionResult GetIndexListByIndexesMasterID(string gid, int? pageIndx, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            DataTable IndexTypedatatable = new DataTable();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            IndexTypeLogic indexTypeLog = new IndexTypeLogic();
            int? totalCount;
            IndexTypedatatable = indexTypeLog.GetIndexListByIndexesMasterID(headerUserID, gid.ToString(), pageIndx, pageSize, out totalCount);
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
        [Route("api/indextype/getindexlistbydate")]
        public IActionResult GetIndexListByDates([FromBody]IndexesMasterSearchDataContract _indexesMasterSearchDC)
        {
            GenericResult _authenticationResult = null;
            DataTable IndexTypedatatable = new DataTable();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            IndexTypeLogic indexTypeLog = new IndexTypeLogic();
            int? totalCount;
            IndexTypedatatable = indexTypeLog.GetIndexListByDates(_indexesMasterSearchDC, headerUserID, out totalCount);
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
        [Route("api/indextype/importindexes")]
        public IActionResult ImportIndexes([FromBody] IndexesMasterDataContract _indexesDC)
        {
            GenericResult _actionResult = null;
            int res = 0;

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            IndexTypeLogic indexTypeLog = new IndexTypeLogic();
            if (_indexesDC != null)
            {
                try
                {
                    indexTypeLog.ImportIndexes(headerUserID, _indexesDC);
                    _actionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                    };
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

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/indextype/refreshLibors")]
        public IActionResult RefreshLibors(IndexTypeDataContract indexes)
        {

            GenericResult _actionResult = null;
            int res = 0;

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            //if (Request.Headers.TryGetValues("TokenUId", out headerValues))
            //{
            //    headerUserID = new Guid(headerValues.FirstOrDefault());
            //}

            var IndexName = "Default Index";
            //var json = File.ReadAllText(@"E:\Data\Code\CRES5\SampleJson\Libordata.json");
            //DataTable dtjsonresult = (DataTable)JsonConvert.DeserializeObject(json, (typeof(DataTable)));

            //IndexTypeLogic indexTypeLog = new IndexTypeLogic();
            //indexTypeLog.InsertIndexTypeOutputJsonInfo(IndexName, dtjsonresult, headerUserID);

            try
            {
                _actionResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                };
            }
            catch (Exception ex)
            {
                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            //}


            return Ok(_actionResult);
        }
    }
}