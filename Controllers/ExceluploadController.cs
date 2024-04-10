
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using CRES.DataContract;
using CRES.BusinessLogic;
using System.Threading.Tasks;
using System.Collections;
using CRES.Utilities;
using System.Net.Http.Headers;
using System.Web;
using System.Drawing;
using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using CRES.Services;
using System.IO;
using System.Data;
using CRES.Services.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.StaticFiles;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Newtonsoft.Json;
using System.Threading;

namespace CRES.ServicesNew.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class ExceluploadController : ControllerBase
    {
        //[Services.Controllers.IsAuthenticate]
        //[Services.Controllers.DeflateCompression]

        [HttpGet]
        [Route("api/excelupload/getallservicer")]
        public IActionResult GetAllServicer()
        {
            GenericResult _authenticationResult = null;
            List<ServicerDataContract> lstServicer = new List<ServicerDataContract>();

            //
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            TranscationLogic servicerlogic = new TranscationLogic();

            UserPermissionLogic upl = new UserPermissionLogic();
            List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID, "Deallist");

            if (permissionlist.Count > 0)
            {
                lstServicer = servicerlogic.GetAllServicer();
            }

            try
            {
                if (lstServicer != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",

                        lstServicer = lstServicer,
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
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.TransactionReconciliation.ToString(), "Error occurred in GetAllServicer ", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/excelupload/GetAllTranscation")]
        public IActionResult GetAllTranscation(bool ShowAllTransaction)
        {
            GenericResult _authenticationResult = null;
            DataTable dt = new DataTable();


            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            TranscationLogic servicerlogic = new TranscationLogic();
            dt = servicerlogic.GetAllTranscation();


            try
            {
                if (dt != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        dtCalcReq = dt
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
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.TransactionReconciliation.ToString(), "Error occurred in GetAllTranscation ", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/excelupload/GetAllTransc")]
        public IActionResult GetAllTrans([FromBody] string type, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            DataTable dt = new DataTable();
            IEnumerable<string> headerValues;
            bool ShowAllTransaction = false;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            int? totalCount = 0;
            TranscationLogic servicerlogic = new TranscationLogic();

            if (type.Contains("ShowAllTransaction"))
                ShowAllTransaction = true;
            else
                ShowAllTransaction = false;
            try
            {
                if (type.Contains("RefreshM61Amount"))
                    servicerlogic.RefreshM61Amount(Convert.ToString(headerUserID));




                dt = servicerlogic.GetAllTranscationPaging(ShowAllTransaction, headerUserID, pageSize, pageIndex, out totalCount);
                if (dt != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        dtCalcReq = dt
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
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.TransactionReconciliation.ToString(), "Error occurred in GetAllTransc ", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/excelupload/RefreshM61Amount")]
        public IActionResult RefreshM61Amount([FromBody] string type, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            DataTable dt = new DataTable();
            IEnumerable<string> headerValues;

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            int? totalCount;
            TranscationLogic servicerlogic = new TranscationLogic();

            // dt = servicerlogic.GetAllTranscation();

            try
            {
                if (type == "RefreshM61Amount")
                    servicerlogic.RefreshM61Amount(headerUserID);



                dt = servicerlogic.GetAllTranscationPaging(false, headerUserID, pageSize, pageIndex, out totalCount);
                if (dt != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        dtCalcReq = dt
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
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.TransactionReconciliation.ToString(), "Error occurred in GetAllTransc ", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/excelupload/GetHistoricalTranscation")]
        public IActionResult GetHistoricalDataforTranscationRecon()
        {
            GenericResult _authenticationResult = null;
            DataTable dt = new DataTable();
            IEnumerable<string> headerValues;

            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            TranscationLogic servicerlogic = new TranscationLogic();


            try
            {
                dt = servicerlogic.GetHistoricalDataforTranscationRecon();
                if (dt != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        dtCalcReq = dt
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
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.TransactionReconciliation.ToString(), "Error occurred in GetHistoricalDataforTranscationRecon ", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);

        }


        [HttpPost]
        [Route("api/excelupload/uploadfiletoazureblob")]
        public async Task<IActionResult> UploadFileToAzureBlob()
        {
            string[] _supportedMimeTypes = { "xlsx/xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
            var headerUserID = String.Empty;
            GenericResult _authenticationResult = null;
            try
            {
                GetConfigSetting();
                var Container = Sectionroot.GetSection("storage:container:name").Value;
                var files = Request.Form.Files[0];

                var provider = new FileExtensionContentTypeProvider();
                bool isMimeTypeSupported = true;
                string contentType;


                CRES.DataContract.FileDetail fDetail = new CRES.DataContract.FileDetail();
                // List<CRES.DataContract.FileDetail> fDetail = new List<CRES.DataContract.FileDetail>();
                if (!isMimeTypeSupported)
                {
                    return BadRequest($"Fail==Only xlsx is supported");
                }
                TranscationLogic _ServicerLogic = new TranscationLogic();

                var Servicername = String.Empty;
                var Servicerid = String.Empty;
                var ScenarioId = String.Empty;
               
                int Filebatchid;
                var queryString = HttpContext.Request.Query;
                if (!string.IsNullOrEmpty(queryString["userid"]))
                {
                    headerUserID = queryString["userid"];
                    Servicername = queryString["Servicer"];
                    Servicerid = queryString["Servicerid"];
                    ScenarioId = queryString["ScenarioId"];
                }

                var storagetype = "AzureBlob";


                var accountName = Sectionroot.GetSection("storage:account:name").Value;
                var accountKey = Sectionroot.GetSection("storage:account:key").Value;
                var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                CloudBlobContainer excelContainer = blobClient.GetContainerReference(Container);
                //
                try
                {


                    string fileName = ContentDispositionHeaderValue.Parse(files.ContentDisposition).FileName.Trim('"');
                    string UniqueFileName = GetUniqueFileName(fileName);

                    fDetail.FileName = fileName;
                    fDetail.NewFileName = UniqueFileName;

                    using (var stream = files.OpenReadStream())
                    {
                        CloudBlockBlob cloudBlockBlob = excelContainer.GetBlockBlobReference(UniqueFileName);
                        await cloudBlockBlob.UploadFromStreamAsync(stream);
                    }

                }
                catch (Exception ex)
                {
                    return BadRequest($"Fail==An error has occured. Details: {ex.Message}");
                }


                AzureStorageReadFile azureStorageReadFile = new AzureStorageReadFile();

                JsonFileConfiguration fileConf = azureStorageReadFile.GetJsonFile(fDetail.NewFileName, Servicername);
                if (fileConf.MappingColumns != null)
                {
                    FileBatchLogDataContract fb = new FileBatchLogDataContract();
                    fb.ServcerMasterID = Convert.ToInt32(Servicerid);
                    fb.OrigFileName = fDetail.FileName;
                    fb.BlobFileName = fDetail.NewFileName;
                    fb.CreatedBy = headerUserID;
                    Filebatchid = _ServicerLogic.insertupdateFileBatchLog(fb, "");


                    string LandingTableName = fileConf.LandingTableName;
                    DateTime? periodCloseDate = _ServicerLogic.GetAllServicer().FirstOrDefault().CloseDate;
                    DataTable dtExcel = azureStorageReadFile.GetFileData(fb.BlobFileName, fb.OrigFileName, fileConf);

                    if (dtExcel.Rows.Count > 0)
                    {
                        if (dtExcel.Rows[0]["ErrorMessage"].ToString() != "")
                        {
                            _ServicerLogic.insertupdateFileBatchDetail(headerUserID, Convert.ToInt32(Filebatchid), "Validation failed.", dtExcel.Rows[0]["ErrorMessage"].ToString());
                            CloudBlockBlob _blockBlob = excelContainer.GetBlockBlobReference(fb.BlobFileName);
                            // _blockBlob.Delete();
                            //  return Ok($"Fail==" + dtExcel.Rows[0]["ErrorMessage"]);

                            _authenticationResult = new GenericResult()
                            {
                                Succeeded = false,
                                Message = " Fail == " + Servicername + " File : " + dtExcel.Rows[0]["ErrorMessage"].ToString()
                            };
                        }
                        else
                        {
                            dtExcel.Columns.Remove("ErrorMessage");
                            string result = string.Empty, ignoredrows = string.Empty;
                            // DataTable transdt = new DataTable();
                            result = _ServicerLogic.BulkInsertbyServicer(dtExcel, fileConf, Convert.ToInt32(Filebatchid), periodCloseDate);
                            var smessage = result.Split('#');

                            if (smessage[0] == "Success")
                            {
                                _ServicerLogic.insertupdateFileBatchDetail(headerUserID, Convert.ToInt32(Filebatchid), "Data uploaded in landing table", "");
                                //=========Insert into Transcation Reconciliation

                                ignoredrows = _ServicerLogic.insertintoTranscation(fileConf.ImportProcedureName, Filebatchid, ScenarioId);
                                var smessageignoredrows = ignoredrows.Split('#');
                                if (smessageignoredrows[0] == "Success")
                                {

                                    _ServicerLogic.insertupdateFileBatchDetail(headerUserID, Convert.ToInt32(Filebatchid), "Data imported in Transcation table", "");
                                }

                                _authenticationResult = new GenericResult()
                                {
                                    Succeeded = true,
                                    Message = "Success==" + Servicername + " File : <br/>" + smessageignoredrows[1],

                                };

                                // return Ok($"Success==File:  { fileDisplayName} has successfully uploaded.");

                            }
                            else
                            {
                                _ServicerLogic.insertupdateFileBatchDetail(headerUserID, Convert.ToInt32(Filebatchid), "Failed to insert into landing table", result);
                                _authenticationResult = new GenericResult()
                                {
                                    Succeeded = false,
                                    Message = " Fail == " + Servicername + " File : " + smessage[1]

                                };
                            }
                        }
                    }
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = " Fail == " + Servicername + "File : " + "Configuration file does not exist."

                    };
                }
                return Ok(_authenticationResult);
            }
            catch (Exception ex)
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = " Fail ==  " + "File does not have proper data."

                };
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.TransactionReconciliation.ToString(), "Error occurred in UploadFileToAzureBlob ", "", "", ex.TargetSite.Name.ToString(), "", ex);

                return Ok(_authenticationResult);
            }

        }


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/excelupload/AddUpdateTranscationReconciliation")]
        public IActionResult AddUpdateTranscationReconciliation([FromBody] DataTable dt)
        {
            GenericResult _actionResult = null;
            int res = 0;

            IEnumerable<string> headerValues;

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            TranscationLogic _ServicerLogic = new TranscationLogic();
            if (dt != null)
            {
                res = _ServicerLogic.UpdateTranscationRecon(dt, headerUserID);
            }

            try
            {
                if (res != 0)
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Transaction(s) reconciled successfully.",
                    };
                }
                else
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Failed to reconcile.",
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.TransactionReconciliation.ToString(), "Error occurred in AddUpdateTranscationReconciliation ", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/excelupload/saveTranscationReconciliation")]
        public IActionResult SaveTranscationReconciliation([FromBody] DataTable dt)
        {
            GenericResult _actionResult = null;
            int res = 0;


            //var json = JsonConvert.SerializeObject(dt);
            //DataTable dataTable = (DataTable)JsonConvert.DeserializeObject(json, (typeof(DataTable)));





            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            TranscationLogic _ServicerLogic = new TranscationLogic();


            try
            {

                if (dt != null)
                {
                    res = _ServicerLogic.SaveTranscation(dt, headerUserID);
                }
                if (res != 0)
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Transaction saved successfully.",
                    };
                }
                else
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Failed to save.",
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
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.TransactionReconciliation.ToString(), "Error occurred in SaveTranscationReconciliation ", "", "", ex.TargetSite.Name.ToString(), "", ex);
            }

            return Ok(_actionResult);
            //  return Request.CreateResponse(HttpStatusCode.OK, _actionResult);

        }



        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/excelupload/UnreconcileTranscation")]
        public IActionResult UnreconcileTranscation([FromBody] DataTable dt)
        {
            GenericResult _actionResult = null;
            string res = "";

            IEnumerable<string> headerValues;

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            TranscationLogic _ServicerLogic = new TranscationLogic();


            try
            {
                if (dt != null)
                {
                    res = _ServicerLogic.UnreconcileTranscation(dt, headerUserID);
                }
                if (res == "Unreconciled successfully.")
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = res,
                    };
                }
                else
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = res,
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
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.TransactionReconciliation.ToString(), "Error occurred in UnreconcileTranscation ", "", "", ex.TargetSite.Name.ToString(), "", ex);

            }

            return Ok(_actionResult);


        }


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/excelupload/FilterTranscation")]
        public IActionResult FilterTranscation([FromBody] string filterstr)
        {
            GenericResult _actionResult = null;

            IEnumerable<string> headerValues;
            DataTable dt = new DataTable();

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            TranscationLogic _ServicerLogic = new TranscationLogic();

            try
            {

                dt = _ServicerLogic.FilterTranscation(filterstr, headerUserID);
                if (dt.Rows.Count > 0)
                {
                    if (dt.Rows[0]["ErrorMessage"].ToString() == "")
                    {
                        dt.Columns.Remove("ErrorMessage");
                        dt.Rows.RemoveAt(dt.Rows.Count - 1);
                        _actionResult = new GenericResult()
                        {
                            Succeeded = true,
                            dtCalcReq = dt,
                            //  Message = "Transcation saved  succeessfully.",
                        };
                    }
                    else
                    {
                        DataTable dtt = new DataTable();
                        _actionResult = new GenericResult()
                        {
                            Succeeded = false,
                            dtCalcReq = dtt,
                            Message = dt.Rows[0]["ErrorMessage"].ToString(),

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
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.TransactionReconciliation.ToString(), "Error occurred in FilterTranscation ", "", "", ex.TargetSite.Name.ToString(), "", ex);
            }
            return Ok(_actionResult);


        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/excelupload/getallTranscationAuditLog")]
        public IActionResult GetAllTranscationAuditLog()
        {
            GenericResult _authenticationResult = null;
            List<TransactionAuditDataContract> lstTransAudit = new List<TransactionAuditDataContract>();


            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            TranscationLogic servicerlogic = new TranscationLogic();

            lstTransAudit = servicerlogic.GetAllTranscationAuditLog();

            try
            {
                if (lstTransAudit != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        lstTransactionAudit = lstTransAudit,

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
                Logger.Write("Error in loading all deal detail", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.TransactionReconciliation.ToString(), "Error occurred in GetAllTranscationAuditLog ", "", "", ex.TargetSite.Name.ToString(), "", ex);

            }
            return Ok(_authenticationResult);

        }

        [HttpPost]
        [Services.Controllers.DeflateCompression]
        [Services.Controllers.IsAuthenticate]
        [Route("api/excelupload/getTranscationAuditDetail")]
        public IActionResult GetTranscationAuditDetail([FromBody] int batchid)
        {
            GenericResult _authenticationResult = null;
            //  List<TransactionAuditDataContract> lstTransAudit = new List<TransactionAuditDataContract>();

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            TranscationLogic servicerlogic = new TranscationLogic();
            DataTable dt = new DataTable();
            try
            {
                dt = servicerlogic.GetAllTranscationbyBatchID(batchid);
                if (dt != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        dtTestCase = dt,

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
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.TransactionReconciliation.ToString(), "Error occurred in getTranscationAuditDetail ", "", "", ex.TargetSite.Name.ToString(), "", ex);
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
        [Route("api/excelupload/DeleteAuditbyBatchlogId")]
        public IActionResult DeleteAuditbyBatchlogId([FromBody] int batchid)
        {
            GenericResult _authenticationResult = null;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            TranscationLogic servicerlogic = new TranscationLogic();



            try
            {
                int res = servicerlogic.DeleteAuditbyBatchlogId(batchid);
                if (res != 0)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true

                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false

                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.TransactionReconciliation.ToString(), "Error occurred in DeleteAuditbyBatchlogId ", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/excelupload/AllTransactionsByNoteId")]
        public IActionResult AllTransactionsByNoteId([FromBody] string filterstr)
        {
            GenericResult _actionResult = null;


            DataTable dt = new DataTable();

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            TranscationLogic _ServicerLogic = new TranscationLogic();
            try
            {
                dt = _ServicerLogic.GetAllTransactionsByNoteId(filterstr);
                if (dt.Rows.Count > 0)
                {

                    _actionResult = new GenericResult()
                    {
                        Succeeded = true,
                        lstFundingSchedule = dt,
                        //  Message = "Transcation saved  succeessfully.",
                    };

                }
                else
                {
                    //  DataTable dtt = new DataTable();
                    _actionResult = new GenericResult()
                    {
                        Succeeded = false,
                        lstFundingSchedule = dt,
                        Message = "No record exist",

                    };
                }

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.TransactionReconciliation.ToString(), "Error occurred in AllTransactionsByNoteId ", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_actionResult);


        }

        private readonly string[] _acorereportingsupportedfile = { "xls", "xlsx", "csv" };
        [HttpPost]
        [Route("api/excelupload/generatefile")]
        //public HttpResponseMessage GenerateAccountingReport([FromBody] string ReportFileGUID)
        public async Task<IActionResult> GenerateAccountingReport([FromBody] ReportFileDataContract reportDC)
        {
            AccountingReportLogic reportLogic = new AccountingReportLogic();
            GenericResult _actionResult = null;
            DataSet dsTemplate = null;
            var headerUserID = string.Empty;
            Stream msFile = null;
            string CurrentDate = System.DateTime.Now.ToString("yyyyMMdd");
            try
            {

                string DocumentStorageID = "";


                if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                {
                    headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
                }

                //get report file detail
                ReportFileDataContract _reportdc = GetReportFileByGUID(reportDC.ReportFileGUID);
                _reportdc.DefaultAttributes = reportDC.DefaultAttributes;
                _reportdc.NewFileName = _reportdc.ReportFileName + "_" + System.DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss") + "." + _reportdc.ReportFileFormat.Replace("pipe", "").Replace("PIPE", "");
                //read template from source

                if (_reportdc.ReportFileFormat.ToLower() == "xlsx")
                {
                    using (var memStream = await AzureStorageReadFile.ReadAccountingReportInStream(_reportdc))
                    {

                        DataSet dsReportData = new DataSet();
                        //get the data from the database 
                        DataTable dtReport = new DataTable();
                        DataTable dtCopy = new DataTable();
                        _reportdc.lstReportFileSheet.ForEach(i => i.DefaultAttributes = reportDC.DefaultAttributes);

                        foreach (ReportFileSheetDataContract dc in _reportdc.lstReportFileSheet)
                        {
                            DataSet ds = new DataSet();
                            ds = reportLogic.GetReportDataFromSource(dc);
                            if (ds != null && ds.Tables.Count > 0)
                            {
                                if (ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
                                {
                                    dtReport = ds.Tables[0];
                                }

                                //additional parameters 
                                if (!string.IsNullOrEmpty(_reportdc.DefaultAttributes))
                                {
                                    if (ds.Tables.Count > 1)
                                    {
                                        if (ds.Tables[1] != null && ds.Tables[1].Rows.Count > 0)
                                        {
                                            foreach (DataRow dr1 in ds.Tables[1].Rows)
                                            {
                                                dc.AdditionalParameters = Convert.ToString(dr1["Date"]);
                                            }
                                        }
                                    }
                                }
                            }
                            dtReport.TableName = dc.SheetName;
                            dtCopy = dtReport.Copy();
                            dsReportData.Tables.Add(dtCopy);
                        }
                        //WriteFileDataToExistingFile(_reportdc, dsTemplate, dsReportData, true);
                        //await WriteDataToFile(_reportdc, dsReportData);
                        DocumentStorageID = await WriteDataToStream(_reportdc, dsReportData, memStream);
                    }
                }
                else if (_reportdc.ReportFileFormat.ToLower() == "csv")
                {

                    dsTemplate = await AzureStorageReadFile.ReadAccountingReportTemplateAsDataSet(_reportdc);
                    //get the data from the database 
                    DataTable dtReport = new DataTable();
                    DataTable dtCopy = new DataTable();
                    _reportdc.lstReportFileSheet.ForEach(i => i.DefaultAttributes = reportDC.DefaultAttributes);
                    DataSet dsReportData = new DataSet();
                    foreach (ReportFileSheetDataContract dc in _reportdc.lstReportFileSheet)
                    {
                        DataSet ds = new DataSet();
                        ds = reportLogic.GetReportDataFromSource(dc);
                        if (ds != null && ds.Tables.Count > 0)
                        {
                            dtReport = ds.Tables[0];
                        }
                        dtReport.TableName = dc.SheetName;
                        dtCopy = dtReport.Copy();
                        dsReportData.Tables.Add(dtCopy);
                    }
                    var _isIncludeHeader = _reportdc.lstReportFileSheet[0].IsIncludeHeader;
                    //ReportFileSheetDataContract sheetDC = ReportFileDC.lstReportFileSheet.Where(s => s.SheetName == worksheet.Name).FirstOrDefault();
                    msFile = WriteFileDataToStream(_reportdc, dsTemplate, dsReportData, _isIncludeHeader);
                    //using (Stream msFile = WriteFileDataToStream(_reportdc, dsTemplate, dsReportData, true))
                    //{

                    //upload file to destination
                    FileUploadParameterDataContract fileParam = new FileUploadParameterDataContract();
                    fileParam.StorageTypeID = _reportdc.DestinationStorageTypeID;
                    fileParam.StorageLocation = _reportdc.DestinationStorageLocation;
                    fileParam.FileName = _reportdc.NewFileName;
                    DocumentStorageID = await UploadFilesByStorageType(fileParam, msFile);//
                    msFile.Flush();
                    //}
                }
                else if (_reportdc.ReportFileFormat.ToLower() == "csvpipe")
                {


                    dsTemplate = await AzureStorageReadFile.ReadAccountingReportTemplateAsDataSet(_reportdc);
                    //get the data from the database 
                    DataTable dtReport = new DataTable();
                    DataTable dtCopy = new DataTable();
                    _reportdc.lstReportFileSheet.ForEach(i => i.DefaultAttributes = reportDC.DefaultAttributes);
                    DataSet dsReportData = new DataSet();
                    foreach (ReportFileSheetDataContract dc in _reportdc.lstReportFileSheet)
                    {
                        DataSet ds = new DataSet();
                        ds = reportLogic.GetReportDataFromSource(dc);
                        if (ds != null && ds.Tables.Count > 0)
                        {
                            dtReport = ds.Tables[0];
                        }
                        dtReport.TableName = dc.SheetName + "_" + CurrentDate;
                        dtCopy = dtReport.Copy();
                        dsReportData.Tables.Add(dtCopy);
                    }
                    var _isIncludeHeader = _reportdc.lstReportFileSheet[0].IsIncludeHeader;
                    //ReportFileSheetDataContract sheetDC = ReportFileDC.lstReportFileSheet.Where(s => s.SheetName == worksheet.Name).FirstOrDefault();
                    msFile = WriteFileDataToStream(_reportdc, dsTemplate, dsReportData, _isIncludeHeader);
                    //using (Stream msFile = WriteFileDataToStream(_reportdc, dsTemplate, dsReportData, true))
                    //{

                    //upload file to destination
                    FileUploadParameterDataContract fileParam = new FileUploadParameterDataContract();
                    fileParam.StorageTypeID = _reportdc.DestinationStorageTypeID;
                    fileParam.StorageLocation = _reportdc.DestinationStorageLocation;
                    fileParam.FileName = _reportdc.NewFileName;
                    DocumentStorageID = await UploadFilesByStorageType(fileParam, msFile);//
                    msFile.Flush();
                    //}
                }
                //insert file log
                ReportFileLogDataContract fileLogDC = new ReportFileLogDataContract();
                fileLogDC.CreatedBy = headerUserID.ToString();
                fileLogDC.UpdatedBy = headerUserID.ToString();
                fileLogDC.FileName = _reportdc.NewFileName;

                //fileLogDC.OriginalFileName = _reportdc.ReportFileName + "_" + CurrentDate + "." + _reportdc.ReportFileFormat.Replace("pipe", "").Replace("PIPE", "");
                fileLogDC.OriginalFileName = _reportdc.DownloadFileName + "_" + CurrentDate + "." + _reportdc.ReportFileFormat.Replace("pipe", "").Replace("PIPE", "");

                fileLogDC.StorageTypeID = _reportdc.DestinationStorageTypeID;
                fileLogDC.ObjectGUID = _reportdc.ReportFileGUID.ToString();
                fileLogDC.ObjectID = _reportdc.ReportFileID;
                fileLogDC.ObjectTypeID = "643";
                fileLogDC.Comment = "";
                fileLogDC.StorageLocation = _reportdc.DestinationStorageLocation;
                if (_reportdc.DestinationStorageTypeID == 459)
                {
                    fileLogDC.DocumentStorageID = DocumentStorageID;
                }
                Logger.Write(ModuleName.AccountingReport.ToString(), "Inserting file log ", MessageLevel.Info, "", "");
                reportLogic.InsertReportFileLog(fileLogDC);

                _actionResult = new GenericResult()
                {
                    Succeeded = true,
                    ReportFileLog = fileLogDC
                };

                //if need to download the file
                //if (reportDC.IsDownloadRequire)
                //{
                //    response = await DownloadFileByStorageTypeAndLocation(DocumentStorageID, _reportdc.DestinationStorageTypeID.ToString(), _reportdc.DestinationStorageLocation);

                //    return response;
                //}
            }
            catch (Exception ex)
            {
                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };

                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.AccountingReport.ToString(), "Error in generating account report file (method:GenerateAccountingReport)", "", "", ex.TargetSite.Name.ToString(), "", ex);

                LogDB(ex.Message);
            }
            finally
            {

            }
            return Ok(_actionResult);
        }



        public ReportFileDataContract GetReportFileByGUID(Guid ReportFileGUID)
        {
            ReportFileDataContract ReportFileDC = new ReportFileDataContract();
            AccountingReportLogic reportLogic = new AccountingReportLogic();
            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            ReportFileDC = reportLogic.GetReportFileByGUID(ReportFileGUID, headerUserID);
            return ReportFileDC;
        }

        public async Task<string> UploadFilesByStorageType(FileUploadParameterDataContract fileParamter, Stream msfile)
        {
            string DocumentStorageID = fileParamter.FileName;
            //392-AzureBlob, 459-Box, 641-LocalServer, 642-FTPServer
            GetConfigSetting();
            try
            {
                if (fileParamter.StorageTypeID == 392)
                {
                    var Container = Sectionroot.GetSection("storage:container:name").Value;
                    var accountName = Sectionroot.GetSection("storage:account:name").Value;
                    var accountKey = Sectionroot.GetSection("storage:account:key").Value;
                    var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer excelContainer = blobClient.GetContainerReference(Container);
                    CloudBlobDirectory dr = excelContainer.GetDirectoryReference(fileParamter.StorageLocation);
                    CloudBlockBlob cloudBlockBlob = dr.GetBlockBlobReference(fileParamter.FileName);
                    msfile.Seek(0, SeekOrigin.Begin);
                    await cloudBlockBlob.UploadFromStreamAsync(msfile);
                }
                else if (fileParamter.StorageTypeID == 459)
                {

                    //create folder structure
                    var folderid = await new BoxHelper().CheckAndCreateBoxFolder("", fileParamter.StorageLocation);
                    DocumentDataContract _docDC = new DocumentDataContract();

                    // Generate a new filename for every new blob
                    _docDC.FileName = fileParamter.FileName;
                    _docDC.Storagetype = "Box";
                    DocumentStorageID = await new BoxHelper().UploadFileToFolder(folderid.ToString(), _docDC, msfile);

                }
                else if (fileParamter.StorageTypeID == 641)
                {
                    string filePath = Path.Combine(Directory.GetCurrentDirectory() + "/wwwroot", fileParamter.StorageLocation);

                    string fileName = filePath + "/" + fileParamter.FileName;
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }
                    msfile.Seek(0, SeekOrigin.Begin);
                    using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
                    {
                        msfile.CopyTo(fs);
                    }
                }
                else if (fileParamter.StorageTypeID == 642)
                {
                    //result = await UploadFileToFTPServer();
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.FileUpload.ToString(), "Error in UploadFilesByStorageType", "", "", ex.TargetSite.Name.ToString(), "", ex);

                throw ex;
            }
            finally
            {
                msfile.Flush();
            }
            return DocumentStorageID;


        }


        public void UploadObjectFileToAzureBlob(FileUploadParameterDataContract fileParamter, Stream filestream)
        {
            GetConfigSetting();
            var Container = Sectionroot.GetSection("storage:container:name").Value;
            var accountName = Sectionroot.GetSection("storage:account:name").Value;
            var accountKey = Sectionroot.GetSection("storage:account:key").Value;
            var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer excelContainer = blobClient.GetContainerReference(Container);
            CloudBlobDirectory dr = excelContainer.GetDirectoryReference(fileParamter.StorageLocation);
            CloudBlockBlob cloudBlockBlob = dr.GetBlockBlobReference(fileParamter.FileName);
            filestream.Seek(0, SeekOrigin.Begin);
            cloudBlockBlob.UploadFromStream(filestream);
            filestream.Flush();
        }

        public Stream GetFileStream(ReportFileDataContract ReportFileDC, DataTable dtreport, bool includeHeaders, List<string> lstTemplateLines)
        {
            MemoryStream ms = new MemoryStream();
            string fileName = ReportFileDC.NewFileName;
            //392-AzureBlob, 459-Box, 641-LocalServer, 642-FTPServer
            try
            {
                StreamWriter writer = new StreamWriter(ms);
                foreach (string str in lstTemplateLines)
                {
                    writer.WriteLine(str);
                }
                if (includeHeaders)
                {
                    IEnumerable<String> headerValues = dtreport.Columns.OfType<DataColumn>().Select(column => QuoteValue(column.ColumnName));
                    writer.WriteLine(String.Join(",", headerValues));
                }
                IEnumerable<String> items = null;

                foreach (DataRow row in dtreport.Rows)
                {
                    items = row.ItemArray.Select(o => QuoteValue(o?.ToString() ?? String.Empty));
                    writer.WriteLine(String.Join(",", items));
                }

                writer.Flush();

            }
            catch (Exception ex)
            {
                string errormessage = Logger.GetExceptionString(ex);
                Logger.Write("GetFileStream", "Error while creating get Fil Stream " + errormessage, MessageLevel.Error, "", "");
            }
            return ms;
        }
        public Stream WriteFileDataToStream(ReportFileDataContract ReportFileDC, DataSet dsTemplateData, DataSet dsReportData, bool includeHeaders)
        {
            MemoryStream ms = new MemoryStream();
            string fileName = ReportFileDC.NewFileName;
            //392-AzureBlob, 459-Box, 641-LocalServer, 642-FTPServer
            try
            {
                StreamWriter writer = new StreamWriter(ms);
                if (ReportFileDC.ReportFileFormat.ToLower() == "xlsx")
                {
                    //FileStream file_stream = new FileStream("To_stream.xls", FileMode.Create);
                    //wbToStream.SaveToStream(file_stream);

                    using (var package = new OfficeOpenXml.ExcelPackage())
                    {
                        //Step 2 : Add a new worksheet to ExcelPackage object and give a suitable name
                        foreach (DataTable dtc in dsTemplateData.Tables)
                        {
                            if (dtc.Rows.Count > 0)
                            {

                                OfficeOpenXml.ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(dtc.TableName);
                                dtc.TableName = dtc.TableName;
                                //write template content
                                worksheet.Cells[1, 1].LoadFromDataTable(dtc, false, OfficeOpenXml.Table.TableStyles.None);
                                //write db content(actual data)
                                worksheet.Cells[ReportFileDC.HeaderPosition, 1].LoadFromDataTable(dsReportData.Tables[dtc.TableName], includeHeaders);

                                for (int col = 1; col <= dsReportData.Tables[dtc.TableName].Columns.Count; col++)
                                {
                                    worksheet.Cells[ReportFileDC.HeaderPosition, col].Style.Font.Bold = true;
                                    worksheet.Column(col).Width = 15;
                                }
                            }
                        }

                        Byte[] fileBytes = package.GetAsByteArray();
                        ms = new MemoryStream(fileBytes);
                    }

                }
                else if (ReportFileDC.ReportFileFormat.ToLower() == "csv")
                {
                    IEnumerable<String> items = null;
                    int skeplineCount = 0;
                    //write template content
                    foreach (DataRow row in dsTemplateData.Tables[0].Rows)
                    {
                        items = row.ItemArray.Select(o => QuoteValue(o?.ToString() ?? String.Empty));
                        writer.WriteLine(String.Join(",", items));
                    }

                    ReportFileSheetDataContract sheetDC = ReportFileDC.lstReportFileSheet.FirstOrDefault();
                    if (sheetDC.HeaderPosition > 0)
                    {
                        skeplineCount = sheetDC.HeaderPosition - dsTemplateData.Tables[0].Rows.Count;
                        for (int i = 0; i < skeplineCount; i++)
                        {
                            writer.Write("");
                        }

                    }

                    //write db content header
                    if (includeHeaders)
                    {
                        IEnumerable<String> headerValues = dsReportData.Tables[0].Columns.OfType<DataColumn>().Select(column => QuoteValue(column.ColumnName));
                        writer.WriteLine(String.Join(",", headerValues));
                    }

                    //write db content(actual data)
                    foreach (DataRow row in dsReportData.Tables[0].Rows)
                    {
                        items = row.ItemArray.Select(o => QuoteValue(o?.ToString() ?? String.Empty));
                        writer.WriteLine(String.Join(",", items));
                    }

                    writer.Flush();
                }
                else if (ReportFileDC.ReportFileFormat.ToLower() == "csvpipe")
                {
                    IEnumerable<String> items = null;
                    int skeplineCount = 0;
                    //write template content
                    foreach (DataRow row in dsTemplateData.Tables[0].Rows)
                    {
                        items = row.ItemArray.Select(o => (o?.ToString() ?? String.Empty));
                        //items = row.ItemArray.Select(o => Convert.ToString(o));
                        writer.WriteLine(QuoteValue(String.Join("|", items)));
                    }

                    ReportFileSheetDataContract sheetDC = ReportFileDC.lstReportFileSheet.FirstOrDefault();
                    if (sheetDC.HeaderPosition > 0)
                    {
                        skeplineCount = sheetDC.HeaderPosition - dsTemplateData.Tables[0].Rows.Count;
                        for (int i = 0; i < skeplineCount; i++)
                        {
                            writer.Write("");
                        }

                    }

                    //write db content header
                    if (includeHeaders)
                    {
                        IEnumerable<String> headerValues = dsReportData.Tables[0].Columns.OfType<DataColumn>().Select(column => (column.ColumnName));
                        writer.WriteLine(String.Join("|", headerValues));
                    }

                    //write db content(actual data)
                    foreach (DataRow row in dsReportData.Tables[0].Rows)
                    {
                        items = row.ItemArray.Select(o => (o?.ToString() ?? String.Empty));
                        //items = row.ItemArray.Select(o => Convert.ToString(o).Replace(",", ""));
                        writer.WriteLine(QuoteValue(String.Join("|", items)));
                    }

                    writer.Flush();
                }

            }
            catch (Exception ex)
            {
                string errormessage = Logger.GetExceptionString(ex);
                Logger.Write("WriteFileDataToStream", "Error while Write File Data to Stream " + errormessage, MessageLevel.Error, "", "");
            }
            return ms;
        }

        public Stream WriteFileDataToExistingFile(ReportFileDataContract ReportFileDC, DataSet dsTemplateData, DataSet dsReportData, bool includeHeaders)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory() + "/wwwroot", ReportFileDC.DestinationStorageLocation + "/" + ReportFileDC.NewFileName);
            var fileinfo = new FileInfo(filePath);
            MemoryStream ms = new MemoryStream();
            string fileName = ReportFileDC.NewFileName;
            //392-AzureBlob, 459-Box, 641-LocalServer, 642-FTPServer
            try
            {
                StreamWriter writer = new StreamWriter(ms);
                if (ReportFileDC.ReportFileFormat.ToLower() == "xlsx")
                {
                    //FileStream file_stream = new FileStream("To_stream.xls", FileMode.Create);
                    //wbToStream.SaveToStream(file_stream);

                    using (var package = new OfficeOpenXml.ExcelPackage(fileinfo))
                    {
                        //Step 2 : Add a new worksheet to ExcelPackage object and give a suitable name
                        foreach (ReportFileSheetDataContract dtc in ReportFileDC.lstReportFileSheet)
                        {


                            OfficeOpenXml.ExcelWorksheet worksheet = package.Workbook.Worksheets[dtc.SheetName];
                            //dtc.TableName = dtc.TableName;
                            //write template content
                            //worksheet.Cells[1, 1].LoadFromDataTable(dtc, false, OfficeOpenXml.Table.TableStyles.None);
                            //write db content(actual data)
                            worksheet.Cells[ReportFileDC.HeaderPosition, 1].LoadFromDataTable(dsReportData.Tables[dtc.SheetName], includeHeaders);

                            for (int col = 1; col <= dsReportData.Tables[dtc.SheetName].Columns.Count; col++)
                            {
                                worksheet.Cells[ReportFileDC.HeaderPosition, col].Style.Font.Bold = true;
                                worksheet.Column(col).Width = 15;
                            }

                        }
                        package.Save();
                    }

                }
            }

            catch (Exception ex)
            {

                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.FileUpload.ToString(), "Error while Write File Data To Existing File", "", "", ex.TargetSite.Name.ToString(), "", ex);

            }
            return ms;
        }
        private string QuoteValue(string value)
        {
            return String.Concat("\"",
            value.Replace("\"", "\"\""), "\"");
        }

        private string QuoteValueWithoutBackSlash(string value)
        {

            string output = value.ToString();
            if (output.Contains(",") || output.Contains("\""))
                output = '"' + output.Replace("\"", "\"\"") + '"';
            return output;
        }
        [HttpGet]//http get as it return file 
        [Route("api/excelupload/downloadobjectfile")]

        public async Task<IActionResult> DownloadFileByStorageTypeAndLocation(string ID, string StorageTypeID, string Location)
        // public async Task<HttpResponseMessage> DownloadFileByStorageTypeAndLocation([FromBody] ReportFileLogDataContract filelog)
        {
            //ID = "DrawFee.pdf"; StorageTypeID = "459"; Location = "Invoice";

            GetConfigSetting();
            IActionResult result = null;
            //392-AzureBlob, 459-Box, 641-LocalServer, 642-FTPServer

            MemoryStream memStreamDownloaded = new MemoryStream();
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            try
            {
                if (StorageTypeID == "392")
                {
                    var Container = Sectionroot.GetSection("storage:container:name").Value;
                    var accountName = Sectionroot.GetSection("storage:account:name").Value;
                    var accountKey = Sectionroot.GetSection("storage:account:key").Value;
                    var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer excelContainer = blobClient.GetContainerReference(Container);
                    CloudBlobDirectory dr = excelContainer.GetDirectoryReference(Location);
                    CloudBlockBlob cloudBlockBlob = dr.GetBlockBlobReference(ID);
                    await cloudBlockBlob.DownloadToStreamAsync(memStreamDownloaded);
                    MemoryStream memStream = new MemoryStream(memStreamDownloaded.ToArray());

                    //blockBlob.DownloadToStream(memStream);

                    return File(memStream, "application/octet-stream");
                }
                else if (StorageTypeID == "459")
                {


                    DocumentDataContract _docDC = new DocumentDataContract();
                    _docDC.FolderName = Location;//"Invoice";
                    _docDC.FileName = ID;//"ACORE Fee Invoice – Plaza 1900 No Future Repay , FF, PIK , Amort - 19-1635-DR-0002.pdf";

                    _docDC.DocumentStorageID = ID;
                    memStreamDownloaded = await new BoxHelper().DownloadFile(_docDC);
                    MemoryStream memStream = new MemoryStream(memStreamDownloaded.ToArray());


                    return File(memStream, "application/pdf", ID);

                }
                else if (StorageTypeID == "641")
                {
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), Location);
                    string fileName = filePath + "/" + ID;
                    if (!System.IO.File.Exists(fileName))
                    {
                        //Throw 404 (Not Found) exception if File not found.
                        return NotFound("File not found: " + fileName);
                    }

                    byte[] bytes = System.IO.File.ReadAllBytes(fileName);
                    return File(bytes, "application/octet-stream");
                }
                else if (StorageTypeID == "642")
                {
                    //result = await UploadFileToFTPServer();

                }
                return result;
            }
            catch (Exception ex)
            {
                memStreamDownloaded.Dispose();

                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.AccountingReportHistory.ToString(), "Error in downloading account report file (method:DownloadFileByStorageTypeAndLocation) : ", "", "", ex.TargetSite.Name.ToString(), "", ex);

                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }

        }

        public async Task<string> ReadAndUploadTemplateFile(ReportFileDataContract ReportFileDC)
        {
            GetConfigSetting();
            Logger.Write(ModuleName.AccountingReport.ToString(), "Reading template file " + ReportFileDC.ReportFileTemplate, MessageLevel.Info, "", "");
            string DocumentStorageID = "";
            DataTable dt = new DataTable();
            DataSet ds;
            string fileName = "";
            Stream TemplateMemoryStream = new MemoryStream();
            List<string> lstTemplateLines = new List<string>();
            //392-AzureBlob, 459-Box, 641-LocalServer, 642-FTPServer
            if (ReportFileDC.SourceStorageTypeID == 641)
            {
                fileName = Path.Combine(Directory.GetCurrentDirectory() + "/wwwroot", ReportFileDC.SourceStorageLocation + "/" + ReportFileDC.ReportFileTemplate);
                TemplateMemoryStream = System.IO.File.OpenRead(fileName);
            }
            else if (ReportFileDC.SourceStorageTypeID == 392)
            {
                var Container = Sectionroot.GetSection("storage:container:name").Value;
                CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);

                // Create the blob client.
                CloudBlobDirectory dr = container.GetDirectoryReference(ReportFileDC.SourceStorageLocation);
                CloudBlockBlob cloudBlockBlob = dr.GetBlockBlobReference(ReportFileDC.ReportFileTemplate);
                cloudBlockBlob.DownloadToStream(TemplateMemoryStream);
            }
            else if (ReportFileDC.SourceStorageTypeID == 459)
            {
                DocumentDataContract _docDC = new DocumentDataContract();
                _docDC.DocumentStorageID = ReportFileDC.DocumentStorageID;
                TemplateMemoryStream = await new CRES.Services.Infrastructure.BoxHelper().DownloadFile(_docDC);
            }
            else if (ReportFileDC.SourceStorageTypeID == 642)
            {

            }

            try
            {
                //upload file to destination
                Logger.Write(ModuleName.AccountingReport.ToString(), "Writing template file " + ReportFileDC.NewFileName + " to the destination", MessageLevel.Info, "", "");
                FileUploadParameterDataContract fileParam = new FileUploadParameterDataContract();
                fileParam.StorageTypeID = ReportFileDC.DestinationStorageTypeID;
                fileParam.StorageLocation = ReportFileDC.DestinationStorageLocation;
                fileParam.FileName = ReportFileDC.NewFileName;
                DocumentStorageID = await UploadFilesByStorageType(fileParam, TemplateMemoryStream);//
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.AccountingReport.ToString(), "Error detail(method: ReadAndUploadTemplateFile) :  ", "", "", ex.TargetSite.Name.ToString(), "", ex);

                throw ex;
            }
            return DocumentStorageID;
        }

        public async Task<string> WriteDataToFile(ReportFileDataContract ReportFileDC, DataSet dsReportData)
        {
            try
            {
                Logger.Write(ModuleName.AccountingReport.ToString(), "Writing data to file " + ReportFileDC.NewFileName, MessageLevel.Info, "", "");
                DataTable dt = new DataTable();
                DataSet ds;
                string fileName = "";
                Stream TemplateMemoryStream = new MemoryStream();
                List<string> lstTemplateLines = new List<string>();
                //392-AzureBlob, 459-Box, 641-LocalServer, 642-FTPServer

                if (ReportFileDC.ReportFileFormat.ToLower() == "xlsx")
                {
                    if (ReportFileDC.DestinationStorageTypeID == 641)
                    {
                        fileName = Path.Combine(Directory.GetCurrentDirectory() + "/wwwroot", ReportFileDC.DestinationStorageLocation + "/" + ReportFileDC.NewFileName);
                        var fileinfo = new FileInfo(fileName);
                        using (var package = new OfficeOpenXml.ExcelPackage(fileinfo))
                        {
                            //Step 2 : Add a new worksheet to ExcelPackage object and give a suitable name

                            int iSheetsCount = 0;
                            try
                            {
                                iSheetsCount = package.Workbook.Worksheets.Count;
                            }
                            catch (Exception)
                            {
                                iSheetsCount = package.Workbook.Worksheets.Count;
                            }
                            if (iSheetsCount > 0)
                            {
                                for (int i = 1; i <= iSheetsCount; i++)
                                {
                                    // Get the sheet by index
                                    OfficeOpenXml.ExcelWorksheet worksheet;
                                    try
                                    {
                                        worksheet = package.Workbook.Worksheets[i];
                                    }
                                    catch (Exception)
                                    {
                                        worksheet = package.Workbook.Worksheets[i];
                                    }

                                    worksheet.Cells[ReportFileDC.HeaderPosition, 1].LoadFromDataTable(dsReportData.Tables[worksheet.Name], true);
                                    for (int col = 1; col <= dsReportData.Tables[worksheet.Name].Columns.Count; col++)
                                    {
                                        worksheet.Cells[ReportFileDC.HeaderPosition, col].Style.Font.Bold = true;
                                        worksheet.Column(col).Width = 15;
                                    }
                                }
                                package.Save();
                            }

                        }
                    }
                    else if (ReportFileDC.DestinationStorageTypeID == 392)
                    {
                        var Container = Sectionroot.GetSection("storage:container:name").Value;
                        CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);

                        // Create the blob client.
                        CloudBlobDirectory dr = container.GetDirectoryReference(ReportFileDC.DestinationStorageLocation);
                        CloudBlockBlob cloudBlockBlob = dr.GetBlockBlobReference(ReportFileDC.NewFileName);
                        cloudBlockBlob.DownloadToStream(TemplateMemoryStream);



                        using (var package = new OfficeOpenXml.ExcelPackage(TemplateMemoryStream))
                        {
                            //Step 2 : Add a new worksheet to ExcelPackage object and give a suitable name

                            int iSheetsCount = 0;
                            try
                            {
                                iSheetsCount = package.Workbook.Worksheets.Count;
                            }
                            catch (Exception)
                            {
                                iSheetsCount = package.Workbook.Worksheets.Count;
                            }
                            if (iSheetsCount > 0)
                            {
                                for (int i = 1; i <= iSheetsCount; i++)
                                {
                                    // Get the sheet by index
                                    OfficeOpenXml.ExcelWorksheet worksheet;
                                    try
                                    {
                                        worksheet = package.Workbook.Worksheets[i];
                                    }
                                    catch (Exception)
                                    {
                                        worksheet = package.Workbook.Worksheets[i];
                                    }

                                    worksheet.Cells[ReportFileDC.HeaderPosition, 1].LoadFromDataTable(dsReportData.Tables[worksheet.Name], true);
                                    for (int col = 1; col <= dsReportData.Tables[worksheet.Name].Columns.Count; col++)
                                    {
                                        worksheet.Cells[ReportFileDC.HeaderPosition, col].Style.Font.Bold = true;
                                        worksheet.Column(col).Width = 15;
                                    }
                                }

                                Byte[] fileBytes = package.GetAsByteArray();
                                TemplateMemoryStream = new MemoryStream(fileBytes);
                            }
                            TemplateMemoryStream.Seek(0, SeekOrigin.Begin);
                            await cloudBlockBlob.UploadFromStreamAsync(TemplateMemoryStream);
                        }

                    }

                    else if (ReportFileDC.DestinationStorageTypeID == 459)
                    {
                        DocumentDataContract _docDC = new DocumentDataContract();
                        _docDC.DocumentStorageID = ReportFileDC.DocumentStorageID;
                        TemplateMemoryStream = await new CRES.Services.Infrastructure.BoxHelper().DownloadFile(_docDC);
                    }
                    else if (ReportFileDC.DestinationStorageTypeID == 642)
                    {

                    }
                }
                else if (ReportFileDC.ReportFileFormat.ToLower() == "csv")
                {
                    if (ReportFileDC.DestinationStorageTypeID == 641)
                    {
                        fileName = Path.Combine(Directory.GetCurrentDirectory() + "/wwwroot", ReportFileDC.DestinationStorageLocation + "/" + ReportFileDC.NewFileName);
                        var fileinfo = new FileInfo(fileName);
                        using (var package = new OfficeOpenXml.ExcelPackage(fileinfo))
                        {
                            //Step 2 : Add a new worksheet to ExcelPackage object and give a suitable name

                            int iSheetsCount = 0;
                            try
                            {
                                iSheetsCount = package.Workbook.Worksheets.Count;
                            }
                            catch (Exception)
                            {
                                iSheetsCount = package.Workbook.Worksheets.Count;
                            }
                            if (iSheetsCount > 0)
                            {
                                for (int i = 1; i <= iSheetsCount; i++)
                                {
                                    // Get the sheet by index
                                    OfficeOpenXml.ExcelWorksheet worksheet;
                                    try
                                    {
                                        worksheet = package.Workbook.Worksheets[i];
                                    }
                                    catch (Exception)
                                    {
                                        worksheet = package.Workbook.Worksheets[i];
                                    }

                                    worksheet.Cells[ReportFileDC.HeaderPosition, 1].LoadFromDataTable(dsReportData.Tables[worksheet.Name], true);
                                    for (int col = 1; col <= dsReportData.Tables[worksheet.Name].Columns.Count; col++)
                                    {
                                        worksheet.Cells[ReportFileDC.HeaderPosition, col].Style.Font.Bold = true;
                                        worksheet.Column(col).Width = 15;
                                    }
                                }
                                package.Save();
                            }

                        }
                    }
                    else if (ReportFileDC.DestinationStorageTypeID == 392)
                    {
                        var Container = Sectionroot.GetSection("storage:container:name").Value;
                        CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);

                        // Create the blob client.
                        CloudBlobDirectory dr = container.GetDirectoryReference(ReportFileDC.DestinationStorageLocation);
                        CloudBlockBlob cloudBlockBlob = dr.GetBlockBlobReference(ReportFileDC.NewFileName);
                        cloudBlockBlob.DownloadToStream(TemplateMemoryStream);



                        using (var package = new OfficeOpenXml.ExcelPackage(TemplateMemoryStream))
                        {
                            //Step 2 : Add a new worksheet to ExcelPackage object and give a suitable name

                            int iSheetsCount = 0;
                            try
                            {
                                iSheetsCount = package.Workbook.Worksheets.Count;
                            }
                            catch (Exception)
                            {
                                iSheetsCount = package.Workbook.Worksheets.Count;
                            }
                            if (iSheetsCount > 0)
                            {
                                for (int i = 1; i <= iSheetsCount; i++)
                                {
                                    // Get the sheet by index
                                    OfficeOpenXml.ExcelWorksheet worksheet;
                                    try
                                    {
                                        worksheet = package.Workbook.Worksheets[i];
                                    }
                                    catch (Exception)
                                    {
                                        worksheet = package.Workbook.Worksheets[i];
                                    }

                                    worksheet.Cells[ReportFileDC.HeaderPosition, 1].LoadFromDataTable(dsReportData.Tables[worksheet.Name], true);
                                    for (int col = 1; col <= dsReportData.Tables[worksheet.Name].Columns.Count; col++)
                                    {
                                        worksheet.Cells[ReportFileDC.HeaderPosition, col].Style.Font.Bold = true;
                                        worksheet.Column(col).Width = 15;
                                    }
                                }

                                Byte[] fileBytes = package.GetAsByteArray();
                                TemplateMemoryStream = new MemoryStream(fileBytes);
                            }
                            TemplateMemoryStream.Seek(0, SeekOrigin.Begin);
                            await cloudBlockBlob.UploadFromStreamAsync(TemplateMemoryStream);
                        }

                    }

                    else if (ReportFileDC.DestinationStorageTypeID == 459)
                    {
                        DocumentDataContract _docDC = new DocumentDataContract();
                        _docDC.DocumentStorageID = ReportFileDC.DocumentStorageID;
                        TemplateMemoryStream = await new CRES.Services.Infrastructure.BoxHelper().DownloadFile(_docDC);
                    }
                    else if (ReportFileDC.DestinationStorageTypeID == 642)
                    {

                    }
                }


            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.AccountingReport.ToString(), "Error detail(method: ReadAndUploadTemplateFile) :  ", "", "", ex.TargetSite.Name.ToString(), "", ex);

                throw ex;
            }
            return "Success";
        }

        public async Task<string> WriteDataToStream(ReportFileDataContract ReportFileDC, DataSet dsReportData, Stream strm)
        {
            string DocumentStorageID = "";
            DataTable dt = new DataTable();
            Stream TemplateMemoryStream = new MemoryStream();
            List<string> lstTemplateLines = new List<string>();
            try
            {

                //MemoryStream ms = new MemoryStream();
                //using (FileStream fs = System.IO.File.OpenRead(Directory.GetCurrentDirectory()+ @"\sample1.xlsx"))
                //using (ExcelPackage excelPackage = new ExcelPackage(fs))
                //{
                //    ExcelWorkbook excelWorkBook = excelPackage.Workbook;
                //    ExcelWorksheet excelWorksheet = excelWorkBook.Worksheets.First();
                //    excelWorksheet.Cells[1, 1].Value = "Test";
                //    excelWorksheet.Cells[3, 2].Value = "Test2";
                //    excelWorksheet.Cells[3, 3].Value = "Test3";

                //    excelPackage.SaveAs(ms); // This is the important part.
                //}


                Logger.Write(ModuleName.AccountingReport.ToString(), "Writing data to file " + ReportFileDC.NewFileName, MessageLevel.Info, "", "");

                //392-AzureBlob, 459-Box, 641-LocalServer, 642-FTPServer

                if (ReportFileDC.ReportFileFormat.ToLower() == "xlsx")
                {
                    using (var package = new OfficeOpenXml.ExcelPackage(strm))
                    {

                        //Step 2 : Add a new worksheet to ExcelPackage object and give a suitable name

                        int iSheetsCount = 0;
                        try
                        {
                            iSheetsCount = package.Workbook.Worksheets.Count;
                        }
                        catch (Exception)
                        {
                            iSheetsCount = package.Workbook.Worksheets.Count;
                        }
                        if (iSheetsCount > 0)
                        {
                            for (int i = 0; i < iSheetsCount; i++)
                            {
                                // Get the sheet by index
                                OfficeOpenXml.ExcelWorksheet worksheet;
                                try
                                {
                                    worksheet = package.Workbook.Worksheets[i];
                                }
                                catch (Exception)
                                {
                                    worksheet = package.Workbook.Worksheets[i];
                                }

                                ReportFileSheetDataContract sheetDC = ReportFileDC.lstReportFileSheet.Where(s => s.SheetName == worksheet.Name).FirstOrDefault();
                                worksheet.Cells[sheetDC.HeaderPosition, 1].LoadFromDataTable(dsReportData.Tables[worksheet.Name], sheetDC.IsIncludeHeader);

                                if (ReportFileDC.ReportFileID == 4) //need bold as last row in disclosure report
                                {
                                    for (int col = 1; col <= dsReportData.Tables[worksheet.Name].Columns.Count; col++)
                                    {
                                        //if (dsReportData.Tables[worksheet.Name].Columns[col - 1].DataType.Name == "Decimal")
                                        //{
                                        //    worksheet.Column(col).Style.Numberformat.Format = "$#,##0.00";
                                        //}
                                        ////dsReportData.Tables[worksheet.Name].Columns[col].GetType
                                        //worksheet.Cells[sheetDC.HeaderPosition, col].Style.Font.Bold = true;
                                        worksheet.Cells[sheetDC.HeaderPosition + dsReportData.Tables[worksheet.Name].Rows.Count - 1, col].Style.Font.Bold = true;
                                        if (dsReportData.Tables[worksheet.Name].Columns[col - 1].DataType.Name == "Decimal")
                                        {
                                            worksheet.Cells[sheetDC.HeaderPosition + dsReportData.Tables[worksheet.Name].Rows.Count - 1, col].Style.Numberformat.Format = "$#,##0.00";
                                        }

                                        //worksheet.Column(col).Width = 15;
                                    }
                                }

                                if (string.IsNullOrEmpty(sheetDC.DBAdditionalParameters))
                                {
                                    var query = from cell in worksheet.Cells["A:XFD"]
                                                where cell.Value?.ToString().Contains("Custom_Date") == true
                                                select cell;

                                    foreach (var cell in query)
                                    {
                                        cell.Value = cell.Value.ToString().Replace("Custom_Date", DateTime.Now.ToString("MM/dd/yyyy"));
                                        //var LastDateOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
                                        //cell.Value = cell.Value.ToString().Replace("Custom_Date", LastDateOfMonth.ToString("MM/dd/yyyy"));
                                    }
                                }

                                else if (!string.IsNullOrEmpty(sheetDC.DBAdditionalParameters))
                                {
                                    var dynamicColsplittedvalues = sheetDC.AdditionalParameters.Split(","); // 'date'
                                    var dbsplittedCommavalues = sheetDC.DBAdditionalParameters.Split(",");  // startdate|B
                                    for (int j = 0; j < dynamicColsplittedvalues.Length; j++)
                                    {
                                        if (!string.IsNullOrEmpty(dbsplittedCommavalues[j]))
                                        {
                                            var dbsplittedPipevalues = dbsplittedCommavalues[j].Split("|");
                                            var cellvalue = dbsplittedPipevalues[1];
                                            var cellContainsvalue = dynamicColsplittedvalues[j];
                                            var cellnum = cellvalue + ":XFD";
                                            var query1 = from cell1 in worksheet.Cells[cellnum]
                                                         where (string.IsNullOrEmpty(Convert.ToString(cell1.Value))) ? false : cell1.Value.ToString().Contains(dbsplittedPipevalues[0])
                                                         select cell1;
                                            foreach (var cell1 in query1)
                                            {
                                                cell1.Value = cell1.Value.ToString().Replace(dbsplittedPipevalues[0], cellContainsvalue);
                                            }
                                        }
                                    }
                                }
                            }

                            Byte[] fileBytes = package.GetAsByteArray();
                            TemplateMemoryStream = new MemoryStream(fileBytes);
                        }
                        FileUploadParameterDataContract fileParam = new FileUploadParameterDataContract();
                        fileParam.StorageTypeID = ReportFileDC.DestinationStorageTypeID;
                        fileParam.StorageLocation = ReportFileDC.DestinationStorageLocation;
                        fileParam.FileName = ReportFileDC.NewFileName;
                        DocumentStorageID = await UploadFilesByStorageType(fileParam, TemplateMemoryStream);//
                        TemplateMemoryStream.Flush();
                    }
                }
                else if (ReportFileDC.ReportFileFormat.ToLower() == "csv")
                {
                    int LineCount = new StreamReader(strm).ReadToEnd().Split(new char[] { '\n' }).Length;

                    StreamWriter writer = new StreamWriter(strm);
                    IEnumerable<String> items = null;
                    int skeplineCount = 0;
                    //write template content
                    //foreach (DataRow row in dsTemplateData.Tables[0].Rows)
                    //{
                    //    items = row.ItemArray.Select(o => QuoteValue(o?.ToString() ?? String.Empty));
                    //    writer.WriteLine(String.Join(",", items));
                    //}

                    if (ReportFileDC.HeaderPosition > 0)
                    {
                        skeplineCount = ReportFileDC.HeaderPosition - LineCount;
                        for (int i = 0; i < skeplineCount; i++)
                        {
                            writer.Write("");
                        }

                    }

                    //write db content header
                    IEnumerable<String> headerValues = dsReportData.Tables[0].Columns.OfType<DataColumn>().Select(column => QuoteValue(column.ColumnName));
                    writer.WriteLine(String.Join(",", headerValues));
                    //write db content(actual data)
                    foreach (DataRow row in dsReportData.Tables[0].Rows)
                    {
                        items = row.ItemArray.Select(o => QuoteValue(o?.ToString() ?? String.Empty));
                        writer.WriteLine(String.Join(",", items));
                    }

                    writer.Flush();
                }


            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.AccountingReport.ToString(), "Error detail(method: ReadAndUploadTemplateFile) :  ", "", "", ex.TargetSite.Name.ToString(), "", ex);

                throw ex;
            }
            return DocumentStorageID;
        }
        public async Task<string> WriteDataToStreamWithoutSheet(ReportFileDataContract ReportFileDC, DataSet dsReportData, Stream strm)
        {
            string DocumentStorageID = "";
            DataTable dt = new DataTable();
            Stream TemplateMemoryStream = new MemoryStream();
            List<string> lstTemplateLines = new List<string>();
            try
            {

                Logger.Write(ModuleName.AccountingReport.ToString(), "Writing data to file " + ReportFileDC.NewFileName, MessageLevel.Info, "", "");

                //392-AzureBlob, 459-Box, 641-LocalServer, 642-FTPServer

                if (ReportFileDC.ReportFileFormat.ToLower() == "xlsx")
                {
                    using (var package = new OfficeOpenXml.ExcelPackage(strm))
                    {

                        //Step 2 : Add a new worksheet to ExcelPackage object and give a suitable name

                        int iSheetsCount = 0;
                        try
                        {
                            iSheetsCount = package.Workbook.Worksheets.Count;
                        }
                        catch (Exception)
                        {
                            iSheetsCount = package.Workbook.Worksheets.Count;
                        }
                        if (iSheetsCount > 0)
                        {
                            for (int i = 0; i < iSheetsCount; i++)
                            {
                                // Get the sheet by index
                                OfficeOpenXml.ExcelWorksheet worksheet;
                                try
                                {
                                    worksheet = package.Workbook.Worksheets[i];
                                }
                                catch (Exception)
                                {
                                    worksheet = package.Workbook.Worksheets[i];
                                }
                                worksheet.Cells[1, 1].LoadFromDataTable(dsReportData.Tables[i], true);
                            }

                            Byte[] fileBytes = package.GetAsByteArray();
                            TemplateMemoryStream = new MemoryStream(fileBytes);
                        }
                        FileUploadParameterDataContract fileParam = new FileUploadParameterDataContract();
                        fileParam.StorageTypeID = ReportFileDC.DestinationStorageTypeID;
                        fileParam.StorageLocation = ReportFileDC.DestinationStorageLocation;
                        fileParam.FileName = ReportFileDC.NewFileName;
                        DocumentStorageID = await UploadFilesByStorageType(fileParam, TemplateMemoryStream);//
                        TemplateMemoryStream.Flush();
                    }
                }
                else if (ReportFileDC.ReportFileFormat.ToLower() == "csv")
                {
                    int LineCount = new StreamReader(strm).ReadToEnd().Split(new char[] { '\n' }).Length;

                    StreamWriter writer = new StreamWriter(strm);
                    IEnumerable<String> items = null;
                    int skeplineCount = 0;
                    //write template content
                    //foreach (DataRow row in dsTemplateData.Tables[0].Rows)
                    //{
                    //    items = row.ItemArray.Select(o => QuoteValue(o?.ToString() ?? String.Empty));
                    //    writer.WriteLine(String.Join(",", items));
                    //}

                    if (ReportFileDC.HeaderPosition > 0)
                    {
                        skeplineCount = ReportFileDC.HeaderPosition - LineCount;
                        for (int i = 0; i < skeplineCount; i++)
                        {
                            writer.Write("");
                        }

                    }

                    //write db content header
                    IEnumerable<String> headerValues = dsReportData.Tables[0].Columns.OfType<DataColumn>().Select(column => QuoteValue(column.ColumnName));
                    writer.WriteLine(String.Join(",", headerValues));
                    //write db content(actual data)
                    foreach (DataRow row in dsReportData.Tables[0].Rows)
                    {
                        items = row.ItemArray.Select(o => QuoteValue(o?.ToString() ?? String.Empty));
                        writer.WriteLine(String.Join(",", items));
                    }

                    writer.Flush();
                }


            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.AccountingReport.ToString(), "Error detail(method: ReadAndUploadTemplateFile) :  ", "", "", ex.TargetSite.Name.ToString(), "", ex);

                throw ex;
            }
            return DocumentStorageID;
        }
        [HttpGet]
        [Route("api/excelupload/importServicerfile")]
        public async Task<IActionResult> ImportFileToDB()
        {
            GetConfigSetting();

            //392-AzureBlob, 459-Box, 641-LocalServer, 642-FTPServer
            GenericResult _actionResult = null;
            MemoryStream memStreamDownloaded = new MemoryStream();
            var headerUserID = "";
            DataSet dsImport = null;
            DataTable dtImport = null;
            List<FileImportColumnMappingDataContract> lstColumnMapping = null;
            List<FileImportMasterDataContract> lstFileImportMaster = new List<FileImportMasterDataContract>();
            var connectionString = Sectionroot.GetSection("storage:servicerimportcontainer:connectionstring").Value;
            var sourceContainerName = Sectionroot.GetSection("storage:servicerimportcontainer:name").Value;
            try
            {
                //get files from the import master
                ImportExportLogic _importExportLogic = new ImportExportLogic();
                lstFileImportMaster = _importExportLogic.GetFileImportMaster();


                //read the file in dataset
                foreach (FileImportMasterDataContract filedc in lstFileImportMaster)
                {
                    try
                    {
                        //get column mapping for this file
                        lstColumnMapping = new List<FileImportColumnMappingDataContract>();
                        lstColumnMapping = _importExportLogic.GetFileImportColumnMappingByID(filedc.FileImportMasterID);


                        FileUploadParameterDataContract fileParams = new FileUploadParameterDataContract();
                        fileParams.FileName = filedc.FileName;
                        fileParams.StorageTypeID = 459; //filedc.SourceStorageTypeID;
                        fileParams.StorageLocation = filedc.SourceStorageLocation;
                        fileParams.HeaderPosition = filedc.HeaderPosition;
                        fileParams.Servicer = "BerkadiaWells";
                        //dsImport = await AzureStorageReadFile.ReadFileAsDataSet(fileParams, connectionString, sourceContainerName);

                        //650-WellsImport, 651-Berkedia

                        dsImport = await AzureStorageReadFile.ReadFileAsDataSet(fileParams, connectionString, sourceContainerName);
                        if (dsImport != null)
                        {
                            //650-WellsImport, 651-Berkedia

                            string[] selectedColumns = lstColumnMapping.Select(x => x.FileColumnName).Distinct().ToArray();
                            dtImport = new DataView(dsImport.Tables[0]).ToTable(false, selectedColumns);

                            if (filedc.ObjectTypeID == 650)
                            {
                                //_importExportLogic.InsertWellsDataTap(dt, "");
                                if (dtImport.Rows.Count > 0)
                                {
                                    DocumentLogic _documentLogic = new DocumentLogic();
                                    _documentLogic.BulkInsert(dtImport, "DW.WellsDataTap", lstColumnMapping, true);

                                    // DocumentLogic _documentLogic = new DocumentLogic();
                                    //_documentLogic.WellsDailyExtractBulkInsert(dsImport.Tables["tblACOREDailyExtract"], "DW.ServicingTransactionBI");
                                }
                            }
                            else if (filedc.ObjectTypeID == 651)
                            {
                                //insert datatable to db table
                                //bulk insert
                                // DocumentLogic _documentLogic = new DocumentLogic();
                                //_documentLogic.BulkInsert(dsImport.Tables[0], "CRE.BerkadiaDataTap", lstColumnMapping,true);
                                //normal insert
                                foreach (DataColumn dcol in dtImport.Columns)
                                {
                                    dcol.ColumnName = lstColumnMapping.Where(i => i.FileColumnName == dcol.ColumnName).FirstOrDefault().LandingColumnName;

                                }
                                if (dtImport.Rows.Count > 0)
                                {
                                    _importExportLogic.InsertBerkadiaDataTap(dtImport, "");
                                }
                            }
                        }

                        //manish called this stored Procedure                      
                        Thread ImportServicerBalanceThread = new Thread(() => ImportServicerBalance());
                        ImportServicerBalanceThread.Start();
                    }
                    catch (Exception ex)
                    {
                        string msg = ex.Message;
                        Logger.Write(ModuleName.ServicerFileImport.ToString(), "Error in importing file : " + ExceptionHelper.GetFullMessage(ex), MessageLevel.Error, headerUserID.ToString(), "");
                    }
                }

                _actionResult = new GenericResult()
                {
                    Succeeded = true,
                };
                return Ok(_actionResult);
            }
            catch (Exception ex)
            {
                memStreamDownloaded.Dispose();

                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.ServicerFileImport.ToString(), "Error in Getting  FileImportMaster detail :  ", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
                return Ok(_actionResult);
            }
            finally
            {
                memStreamDownloaded.Dispose();
            }

        }

        public void ImportServicerBalance()
        {
            try
            {
                ImportExportLogic _importExportLogic = new ImportExportLogic();
                _importExportLogic.ImportServicerBalance();
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.ServicerFileImport.ToString(), "Error in ImportServicerBalance :  ", "", "", ex.TargetSite.Name.ToString(), "", ex);

            }
        }
        /*
        [HttpGet]
        [Route("api/excelupload/importServicerfile")]
        public async Task<IActionResult> ImportFileToDB()
        {
            GetConfigSetting();

            //392-AzureBlob, 459-Box, 641-LocalServer, 642-FTPServer
            GenericResult _actionResult = null;
            MemoryStream memStreamDownloaded = new MemoryStream();
            var headerUserID = "";
            DataSet dsImport = null;
            DataTable dtImport = null;
            List<FileImportColumnMappingDataContract> lstColumnMapping = null;
            List<FileImportMasterDataContract> lstFileImportMaster = new List<FileImportMasterDataContract>();
            var connectionString = Sectionroot.GetSection("storage:servicerimportcontainer:connectionstring").Value;
            var sourceContainerName = Sectionroot.GetSection("storage:servicerimportcontainer:name").Value;
            try
            {
                //get files from the import master
                ImportExportLogic _importExportLogic = new ImportExportLogic();
                lstFileImportMaster = _importExportLogic.GetFileImportMaster();


                //read the file in dataset
                foreach (FileImportMasterDataContract filedc in lstFileImportMaster)
                {
                    try
                    {
                        //get column mapping for this file
                        lstColumnMapping = new List<FileImportColumnMappingDataContract>();
                        lstColumnMapping = _importExportLogic.GetFileImportColumnMappingByID(filedc.FileImportMasterID);


                        FileUploadParameterDataContract fileParams = new FileUploadParameterDataContract();
                        fileParams.FileName = filedc.FileName;
                        fileParams.StorageTypeID = filedc.SourceStorageTypeID;
                        fileParams.StorageLocation = filedc.SourceStorageLocation;
                        fileParams.HeaderPosition = filedc.HeaderPosition;
                        dsImport = await AzureStorageReadFile.ReadFileAsDataSet(fileParams, connectionString, sourceContainerName);

                        //650-WellsImport, 651-Berkedia

                        dsImport = await AzureStorageReadFile.ReadFileAsDataSet(fileParams, connectionString, sourceContainerName);
                        if (dsImport != null)
                        {
                            //650-WellsImport, 651-Berkedia

                            string[] selectedColumns = lstColumnMapping.Select(x => x.FileColumnName).Distinct().ToArray();
                            dtImport = new DataView(dsImport.Tables[0]).ToTable(false, selectedColumns);

                            if (filedc.ObjectTypeID == 650)
                            {
                                //_importExportLogic.InsertWellsDataTap(dt, "");
                                if (dtImport.Rows.Count > 0)
                                {
                                    DocumentLogic _documentLogic = new DocumentLogic();
                                    _documentLogic.BulkInsert(dtImport, "DW.WellsDataTap", lstColumnMapping, true);

                                    // DocumentLogic _documentLogic = new DocumentLogic();
                                    //_documentLogic.WellsDailyExtractBulkInsert(dsImport.Tables["tblACOREDailyExtract"], "DW.ServicingTransactionBI");
                                }
                            }
                            else if (filedc.ObjectTypeID == 651)
                            {
                                //insert datatable to db table
                                //bulk insert
                                // DocumentLogic _documentLogic = new DocumentLogic();
                                //_documentLogic.BulkInsert(dsImport.Tables[0], "CRE.BerkadiaDataTap", lstColumnMapping,true);
                                //normal insert
                                foreach (DataColumn dcol in dtImport.Columns)
                                {
                                    dcol.ColumnName = lstColumnMapping.Where(i => i.FileColumnName == dcol.ColumnName).FirstOrDefault().LandingColumnName;

                                }
                                if (dtImport.Rows.Count > 0)
                                {
                                    _importExportLogic.InsertBerkadiaDataTap(dtImport, "");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        string msg = ex.Message;
                        Logger.Write(ModuleName.ServicerFileImport.ToString(), "Error in importing file : " + ExceptionHelper.GetFullMessage(ex), MessageLevel.Error, headerUserID.ToString(), "");
                    }
                }

                _actionResult = new GenericResult()
                {
                    Succeeded = true,
                };
                return Ok(_actionResult);
            }
            catch (Exception ex)
            {
                memStreamDownloaded.Dispose();

                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.ServicerFileImport.ToString(), "Error in Getting  FileImportMaster detail :  ", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
                return Ok(_actionResult);
            }
            finally
            {
                memStreamDownloaded.Dispose();
            }

        }
        */


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


        public string GetUniqueFileName(string FileName)
        {
            var newfileName = FileName.Replace("\"", "").ToLower();
            int index = newfileName.LastIndexOf('.');
            //string onyName = fileName.Substring(0, index);
            string fileExtension = "." + newfileName.Substring(index + 1);

            // Generate a new filename for every new blob
            newfileName = FileName.Replace("\"", "").ToLower().Replace(fileExtension, "_" + System.DateTime.Now.ToString("yyyy-MM-dd-mm-ss") + fileExtension);
            return newfileName;
        }

        public void LogDB(string message)
        {
            try
            {
                Microsoft.Extensions.Configuration.IConfigurationBuilder builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
                builder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
                var root = builder.Build();
                System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection();
                connection.ConnectionString = root.GetSection("Application").GetSection("ConnectionStrings").Value;
                System.Data.SqlClient.SqlCommand dbCmd = new System.Data.SqlClient.SqlCommand("dbo.TestLog", connection);
                dbCmd.CommandType = CommandType.StoredProcedure;
                dbCmd.Parameters.AddWithValue("LogMessage", message);
                connection.Open();
                dbCmd.ExecuteNonQuery();
                connection.Close();
            }
            catch { }
        }

        [HttpPost]
        [Route("api/excelupload/downloadexcelfile")]
        public IActionResult DownloadFileByDataTable([FromBody] DataTable dealfunding)
        {
            int cnt = 0;

            try
            {

                int Notescnt = Convert.ToInt32(dealfunding.Rows[0]["NotesCount"]);



                DataTable dt = new DataTable();
                dt.Columns.Add("Date", typeof(DateTime));
                dt.Columns.Add("Debt Amount", typeof(decimal));
                dt.Columns.Add("Required Equity", typeof(decimal));
                dt.Columns.Add("Additional Equity", typeof(decimal));
                dt.Columns.Add("Purpose", typeof(string));
                dt.Columns.Add("Confirmed", typeof(string));
                dt.Columns.Add("Adjustment Type", typeof(string));
                dt.Columns.Add("Generated By", typeof(string));
                dt.Columns.Add("Status", typeof(string));
                dt.Columns.Add("Comment", typeof(string));

                dt.Columns.Add("Draw Fee Status", typeof(string));

                if (dealfunding.Rows.Count > 0)
                {
                    if (dealfunding.Columns.Contains("isValidDate"))
                    {
                        dealfunding.Columns.Remove("isValidDate");
                    }
                    if (dealfunding.Columns.Contains("NotesCount"))
                    {
                        dealfunding.Columns.Remove("NotesCount");
                    }

                    if (dealfunding.Columns.Contains("IsRowEdited"))
                    {
                        dealfunding.Columns.Remove("IsRowEdited");
                    }
                    if (dealfunding.Columns.Contains("GeneratedBy"))
                    {
                        dealfunding.Columns.Remove("GeneratedBy");
                    }
                    if (dealfunding.Columns.Contains("NonCommitmentAdj"))
                    {
                        dealfunding.Columns.Remove("NonCommitmentAdj");
                    }

                    if (dealfunding.Columns.Contains("OrgDealFundingRowno"))
                    {
                        dealfunding.Columns.Remove("OrgDealFundingRowno");
                    }
                    if (dealfunding.Columns.Contains("Funding_delete"))
                    {
                        dealfunding.Columns.Remove("Funding_delete");
                    }
                    //if (dealfunding.Columns.Contains("AdjustmentType"))
                    //{
                    //    dealfunding.Columns.Remove("AdjustmentType");
                    //}

                    if (dealfunding.Columns.Contains("OrgDealFundingRowno"))
                    {
                        dealfunding.Columns.Remove("OrgDealFundingRowno");
                    }

                    if (Notescnt > 0)
                    {
                        for (int j = dealfunding.Columns.Count - Notescnt; j < dealfunding.Columns.Count; j++)
                        {
                            dt.Columns.Add(dealfunding.Columns[j].ColumnName, typeof(decimal));
                        }
                    }



                    for (int i = 0; i < dealfunding.Rows.Count; i++)
                    {
                        DataRow dr = dt.NewRow();
                        if (dealfunding.Rows[i]["Date"] != null)
                        {
                            dr["Date"] = DateTime.Parse(dealfunding.Rows[i]["Date"].ToString()).Date;
                        }


                        dr["Debt Amount"] = dealfunding.Rows[i]["Value"];
                        dr["Required Equity"] = dealfunding.Rows[i]["RequiredEquity"];
                        dr["Additional Equity"] = dealfunding.Rows[i]["AdditionalEquity"];
                        dr["Purpose"] = dealfunding.Rows[i]["PurposeText"];
                        dr["Confirmed"] = dealfunding.Rows[i]["Applied"].ToBoolean() == true ? "TRUE" : "FALSE";
                        dr["Adjustment Type"] = dealfunding.Rows[i]["AdjustmentType"];
                        //dr["Non-CommitmentAdjustment"] = dealfunding.Rows[i]["NonCommitmentAdj"].ToBoolean() == true ? "Yes" : "No";
                        dr["Status"] = dealfunding.Rows[i]["WF_CurrentStatusDisplayName"];
                        dr["Comment"] = dealfunding.Rows[i]["Comment"];
                        dr["Draw Fee Status"] = dealfunding.Rows[i]["DrawFeeStatusName"];
                        dr["Generated By"] = dealfunding.Rows[i]["GeneratedByText"];
                        if (Notescnt > 0)
                        {
                            for (int j = dealfunding.Columns.Count - Notescnt; j < dealfunding.Columns.Count; j++)
                            {
                                if (!string.IsNullOrEmpty(dealfunding.Rows[i][dealfunding.Columns[j].ColumnName].ToString()))

                                    dr[dealfunding.Columns[j].ColumnName] = Math.Round(Convert.ToDecimal(dealfunding.Rows[i][dealfunding.Columns[j].ColumnName]), 2);
                            }
                        }
                        dt.Rows.Add(dr);

                    }
                }
                using (var package = new ExcelPackage())
                {
                    var workSheet = package.Workbook.Worksheets.Add("DealFunding");

                    workSheet.Cells["A1"].LoadFromDataTable(dt, true);
                    workSheet.DefaultRowHeight = 15;



                    workSheet.Column(1).Style.Numberformat.Format = "MM/dd/yyyy";
                    workSheet.Column(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                    workSheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    workSheet.Column(1).AutoFit();
                    workSheet.Row(1).Style.Font.Bold = true;

                    for (var k = 1; k <= workSheet.Dimension.End.Column; k++)
                    {
                        workSheet.Column(k).AutoFit();
                        cnt = k;
                    }
                    var maxcnt = dt.Rows.Count + 2;
                    var start = workSheet.Dimension.Start;
                    var end = workSheet.Dimension.End;

                    for (int col = start.Column + 1; col <= end.Column; col++)
                    {
                        if (workSheet.Cells[1, col].Text != "Purpose" && workSheet.Cells[1, col].Text != "Confirmed" && workSheet.Cells[1, col].Text != "Status" && workSheet.Cells[1, col].Text != "Comment" && workSheet.Cells[1, col].Text != "Draw Fee Status" && workSheet.Cells[1, col].Text != "Non-CommitmentAdjustment" && workSheet.Cells[1, col].Text != "GeneratedBy")
                        {
                            workSheet.Column(col).Style.Numberformat.Format = "#,##0.00";
                            var cell = workSheet.Cells[maxcnt, col];
                            cell.Formula = "=SUM(" + workSheet.Cells[2, col].Address + ":" + workSheet.Cells[maxcnt - 1, col].Address + ")";
                        }
                    }

                    workSheet.Row(maxcnt).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    workSheet.Row(maxcnt).Style.Font.Bold = true;
                    var stream = new MemoryStream(package.GetAsByteArray());
                    return File(stream, "application/octet-stream");
                }
            }
            catch (Exception ex)
            {
                string output = "";
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.ExportFutureFunding.ToString(), "Error occurred in downloadexcelfile ", "", "", ex.TargetSite.Name.ToString(), "", ex);
                return File(output, "application/octet-stream");

            }

        }

        [HttpPost]
        [Route("api/excelupload/downloadexcelfileFundingRule")]
        public IActionResult downloadexcelfileFundingRule([FromBody] DataTable fundingrule)
        {
            int cnt = 0;

            try
            {


                DataTable dt = new DataTable();
                dt.Columns.Add("Note Id", typeof(string));
                dt.Columns.Add("Name", typeof(string));
                dt.Columns.Add("Maturity", typeof(DateTime));
                dt.Columns.Add("Lien Position", typeof(string));
                dt.Columns.Add("Priority", typeof(decimal));
                dt.Columns.Add("Estimated current balance", typeof(decimal));
                dt.Columns.Add("Current PIK Balance", typeof(decimal));
                dt.Columns.Add("Total commitment", typeof(decimal));
                dt.Columns.Add("Adjusted Commitment", typeof(decimal));
                dt.Columns.Add("Unfunded Commitment", typeof(decimal));
                dt.Columns.Add("Initial funding amount", typeof(decimal));
                dt.Columns.Add("Use rule to determine note funding", typeof(string));
                dt.Columns.Add("Funding priority", typeof(decimal));
                dt.Columns.Add("Repayment priority", typeof(decimal));



                for (var m = 1; m < 26; m++)
                {
                    if (fundingrule.Columns.Contains("Funding sequence " + m))
                        dt.Columns.Add("Funding sequence " + m, typeof(decimal));
                }
                for (var n = 1; n < 26; n++)
                {
                    if (fundingrule.Columns.Contains("Repayment sequence " + n))
                        dt.Columns.Add("Repayment sequence " + n, typeof(decimal));
                }


                for (int i = 0; i < fundingrule.Rows.Count; i++)
                {

                    DataRow dr = dt.NewRow();
                    dr["Note Id"] = fundingrule.Rows[i]["CRENoteID"];
                    dr["Name"] = fundingrule.Rows[i]["Name"];
                    dr["Maturity"] = fundingrule.Rows[i]["Maturity"];
                    dr["Lien Position"] = fundingrule.Rows[i]["LienPositionText"];
                    dr["Priority"] = fundingrule.Rows[i]["Priority"];
                    dr["Estimated current balance"] = fundingrule.Rows[i]["EstBls"];
                    dr["Current PIK Balance"] = fundingrule.Rows[i]["CurrentPIKBalance"];
                    dr["Total commitment"] = fundingrule.Rows[i]["TotalCommitment"];
                    dr["Adjusted Commitment"] = fundingrule.Rows[i]["AdjustedTotalCommitment"];
                    dr["Unfunded Commitment"] = fundingrule.Rows[i]["UnfundedCommitment"];
                    dr["Initial funding amount"] = fundingrule.Rows[i]["InitialFundingAmount"];
                    dr["Use rule to determine note funding"] = fundingrule.Rows[i]["UseRuletoDetermineNoteFundingText"];
                    dr["Funding priority"] = fundingrule.Rows[i]["FundingPriority"];
                    dr["Repayment priority"] = fundingrule.Rows[i]["RepaymentPriority"];

                    for (var m = 1; m < 26; m++)
                    {
                        if (dt.Columns.Contains("Funding sequence " + m))
                            dr["Funding sequence " + m] = fundingrule.Rows[i]["Funding sequence " + m];
                    }
                    for (var n = 1; n < 26; n++)
                    {
                        if (dt.Columns.Contains("Repayment sequence " + n))
                            dr["Repayment sequence " + n] = fundingrule.Rows[i]["Repayment sequence " + n];
                    }

                    dt.Rows.Add(dr);

                }



                using (var package = new ExcelPackage())
                {
                    var workSheet = package.Workbook.Worksheets.Add("FundingRule");

                    workSheet.Cells["A1"].LoadFromDataTable(dt, true);
                    workSheet.DefaultRowHeight = 15;

                 

                    workSheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    workSheet.Column(1).AutoFit();
                    workSheet.Row(1).Style.Font.Bold = true;
                    workSheet.Column(3).Style.Numberformat.Format = "MM/dd/yyyy";
                    workSheet.Column(3).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    for (var k = 1; k <= workSheet.Dimension.End.Column; k++)
                    {
                        workSheet.Column(k).AutoFit();
                        cnt = k;
                    }
                    var maxcnt = dt.Rows.Count + 2;
                    var start = workSheet.Dimension.Start;
                    var end = workSheet.Dimension.End;

                    for (int col = start.Column + 1; col <= end.Column; col++)
                    {
                        if (workSheet.Cells[1, col].Text != "Note Id" && workSheet.Cells[1, col].Text != "Name" && workSheet.Cells[1, col].Text != "Maturity" && workSheet.Cells[1, col].Text != "Lien Position" && workSheet.Cells[1, col].Text != "Use rule to determine note funding" && workSheet.Cells[1, col].Text != "Priority" && workSheet.Cells[1, col].Text != "Funding priority" && workSheet.Cells[1, col].Text != "Repayment priority")
                        {
                            workSheet.Column(col).Style.Numberformat.Format = "#,##0.00";
                            var cell = workSheet.Cells[maxcnt, col];
                            cell.Formula = "=SUM(" + workSheet.Cells[2, col].Address + ":" + workSheet.Cells[maxcnt - 1, col].Address + ")";
                        }
                    }

                    workSheet.Row(maxcnt).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    workSheet.Row(maxcnt).Style.Font.Bold = true;
                    var stream = new MemoryStream(package.GetAsByteArray());
                    return File(stream, "application/octet-stream");
                }
            }
            catch (Exception ex)
            {
                string output = "";
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.ExportFutureFunding.ToString(), "Error occurred in downloadexcelfile ", "", "", ex.TargetSite.Name.ToString(), "", ex);
                return File(output, "application/octet-stream");

            }

        }




        [HttpPost]
        [Route("api/excelupload/downloadexcelcommitment")]
        public IActionResult DownloadExcelCommitment([FromBody] DataTable commitment)
        {
            int cnt = 0;


            try
            {

                int Notescnt = Convert.ToInt32(commitment.Rows[0]["NotesCount"]);

                commitment.Columns.Remove("NotesCount");

                DataTable dt = new DataTable();
                dt.Columns.Add("Date", typeof(DateTime));
                dt.Columns.Add("Type", typeof(string));
                dt.Columns.Add("Adjustment History (Deal)", typeof(decimal));
                dt.Columns.Add("Adjusted Commitment", typeof(decimal));
                dt.Columns.Add("M61 Total Commitment", typeof(decimal));
                dt.Columns.Add("Total Equity at Closing", typeof(decimal));
                dt.Columns.Add("Total Required Equity", typeof(decimal));
                dt.Columns.Add("Total Additional Equity", typeof(decimal));
                dt.Columns.Add("Comments", typeof(string));
                if (Notescnt > 0)
                {
                    for (int j = commitment.Columns.Count - Notescnt; j < commitment.Columns.Count; j++)
                    {
                        dt.Columns.Add(commitment.Columns[j].ColumnName, typeof(decimal));
                    }
                }



                for (int i = 0; i < commitment.Rows.Count; i++)
                {
                    DataRow dr = dt.NewRow();
                    if (commitment.Rows[i]["Date"] != null)
                    {
                        dr["Date"] = DateTime.Parse(commitment.Rows[i]["Date"].ToString()).Date;
                    }
                    dr["Type"] = commitment.Rows[i]["Type"];
                    dr["Adjustment History (Deal)"] = commitment.Rows[i]["Adjustment History (Deal)"];
                    dr["Adjusted Commitment"] = commitment.Rows[i]["Adjusted Commitment"];
                    dr["M61 Total Commitment"] = commitment.Rows[i]["M61 Total Commitment"];
                    dr["Total Equity at Closing"] = commitment.Rows[i]["Total Equity at Closing"];
                    dr["Total Required Equity"] = commitment.Rows[i]["Total Required Equity"];
                    dr["Total Additional Equity"] = commitment.Rows[i]["Total Additional Equity"];
                    dr["Comments"] = commitment.Rows[i]["Comments"];
                    if (Notescnt > 0)
                    {
                        for (int j = commitment.Columns.Count - Notescnt; j < commitment.Columns.Count; j++)
                        {
                            if (!string.IsNullOrEmpty(commitment.Rows[i][commitment.Columns[j].ColumnName].ToString()))

                                //   dr[commitment.Columns[j].ColumnName] = Math.Round(Convert.ToDecimal(commitment.Rows[i][commitment.Columns[j].ColumnName]), 2);
                                dr[commitment.Columns[j].ColumnName] = Math.Round(CommonHelper.StringToDecimal(commitment.Rows[i][commitment.Columns[j].ColumnName]).GetValueOrDefault(0), 2);
                        }
                    }
                    dt.Rows.Add(dr);

                }




                using (var package = new ExcelPackage())
                {
                    var workSheet = package.Workbook.Worksheets.Add("Commitment");

                    workSheet.Cells["A1"].LoadFromDataTable(dt, true);
                    workSheet.DefaultRowHeight = 15;



                    workSheet.Column(1).Style.Numberformat.Format = "MM/dd/yyyy";
                    workSheet.Column(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                    workSheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    workSheet.Column(1).AutoFit();
                    workSheet.Row(1).Style.Font.Bold = true;

                    for (var k = 1; k <= workSheet.Dimension.End.Column; k++)
                    {
                        workSheet.Column(k).AutoFit();
                        cnt = k;
                    }
                    var maxcnt = commitment.Rows.Count + 2;
                    var start = workSheet.Dimension.Start;
                    var end = workSheet.Dimension.End;

                    for (int col = start.Column + 1; col <= end.Column; col++)
                    {
                        if (workSheet.Cells[1, col].Text != "Type" && workSheet.Cells[1, col].Text != "Comments")
                        {
                            workSheet.Column(col).Style.Numberformat.Format = "#,##0.00";
                            var cell = workSheet.Cells[maxcnt, col];
                            cell.Formula = "=SUM(" + workSheet.Cells[2, col].Address + ":" + workSheet.Cells[maxcnt - 1, col].Address + ")";
                            //workSheet.Column(col).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        }
                    }

                    workSheet.Row(maxcnt).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    workSheet.Row(maxcnt).Style.Font.Bold = true;
                    var stream = new MemoryStream(package.GetAsByteArray());
                    return File(stream, "application/octet-stream");
                }
            }
            catch (Exception ex)
            {
                string output = "";
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.ExportFutureFunding.ToString(), "Error occurred in downloadexcelfile ", "", "", ex.TargetSite.Name.ToString(), "", ex);
                return File(output, "application/octet-stream");

            }

        }

        [HttpPost]
        //  [Services.Controllers.IsAuthenticate]
        // [Services.Controllers.DeflateCompression]
        [Route("api/excelupload/splitfeetransaction")]
        public IActionResult SplitFeeTransaction([FromBody] DataTable dt)
        {
            GenericResult _actionResult = null;
            DataTable dtresult = new DataTable();

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            TranscationLogic _ServicerLogic = new TranscationLogic();
            if (dt != null)
            {
                dtresult = _ServicerLogic.SplitFeeTransaction(dt);
            }

            try
            {
                if (dtresult.Rows.Count != 0)
                {
                    _actionResult = new GenericResult()
                    {
                        dt = dtresult,
                        Succeeded = true,
                        Message = "Transaction split successfully.",
                    };
                }
                else
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Failed to split.",
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
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.TransactionReconciliation.ToString(), "Error occurred in SplitFeeTranscation ", "", "", ex.TargetSite.Name.ToString(), "", ex);
            }

            return Ok(_actionResult);


        }



        [HttpPost]
        [Route("api/excelupload/reconcilesplitfeetransaction")]
        public IActionResult ReconcileSplitFeeTransaction([FromBody] DataTable dt)
        {
            GenericResult _actionResult = null;
            //DataTable dtresult = new DataTable();
            int result = 0;

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            TranscationLogic _ServicerLogic = new TranscationLogic();

            result = _ServicerLogic.ReconcileSplitFeeTransaction(dt, headerUserID);


            try
            {
                if (result != 0)
                {
                    _actionResult = new GenericResult()
                    {

                        Succeeded = true,
                        Message = "Transaction split successfully.",
                    };
                }
                else
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Failed to split.",
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
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.TransactionReconciliation.ToString(), "Error occurred in ReconcileSplitFeeTransaction ", "", "", ex.TargetSite.Name.ToString(), "", ex);
            }

            return Ok(_actionResult);


        }





        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/excelupload/getalltransactiontype")]
        public IActionResult GetAllTransactionType()
        {
            GenericResult _authenticationResult = null;
            DataTable dtTransactionType = new DataTable();


            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            TranscationLogic servicerlogic = new TranscationLogic();
            dtTransactionType = servicerlogic.GetAllTransactionType();

            try
            {
                if (dtTransactionType != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        dt = dtTransactionType
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
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.TransactionReconciliation.ToString(), "Error occurred in GetAllTransactionType ", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);

        }


        [HttpPost]
        [Route("api/excelupload/downloadxirrreturn")]
        //public HttpResponseMessage GenerateAccountingReport([FromBody] string ReportFileGUID)
        public async Task<IActionResult> DownloadXIRRReturn([FromBody] XIRRConfigDataContract xirrConfig)
        {
            ReportFileDataContract reportDC = new ReportFileDataContract();
            reportDC.ReportFileName = "XIRR_Output";
            reportDC.ReportFileFormat = "xlsx";
            reportDC.SourceStorageLocation = "ExcelTemplate";
            reportDC.SourceStorageTypeID = 641;
            reportDC.ReportFileTemplate = "XIRR_Output" + "." + reportDC.ReportFileFormat;

            AccountingReportLogic reportLogic = new AccountingReportLogic();
            GenericResult _actionResult = null;
            var headerUserID = string.Empty;
            string CurrentDate = System.DateTime.Now.ToString("yyyyMMdd");
            TagXIRRLogic xirrLogic = new TagXIRRLogic();
            try
            {
                if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                {
                    headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
                }

                if (reportDC.ReportFileFormat.ToLower() == "xlsx")
                {
                    //read template from source
                    using (var memStream = await AzureStorageReadFile.ReadAccountingReportInStream(reportDC))
                    {

                        DataSet dsXIRRData = new DataSet();
                        //get the data from the database 
                        DataTable dtXIRR = new DataTable();
                        dtXIRR = xirrLogic.GetXIRROutputByConfigID(xirrConfig.XIRRConfigID, headerUserID);
                        dsXIRRData.Tables.Add(dtXIRR);
                        // write data to stream
                        using (var package = new OfficeOpenXml.ExcelPackage(memStream))
                        {

                            //Step 2 : Add a new worksheet to ExcelPackage object and give a suitable name

                            int iSheetsCount = 0;
                            try
                            {
                                package.Workbook.Worksheets[0].Name = "XIRR Return";
                                iSheetsCount = package.Workbook.Worksheets.Count;
                            }
                            catch (Exception)
                            {
                                iSheetsCount = package.Workbook.Worksheets.Count;
                            }
                            if (iSheetsCount > 0)
                            {

                                // Get the sheet by index
                                OfficeOpenXml.ExcelWorksheet worksheet;
                                try
                                {
                                    worksheet = package.Workbook.Worksheets[0];
                                }
                                catch (Exception)
                                {
                                    worksheet = package.Workbook.Worksheets[0];
                                }

                                worksheet.Cells[2, 1].LoadFromDataTable(dsXIRRData.Tables[0], false);
                                var stream = new MemoryStream(package.GetAsByteArray());
                                return File(stream, "application/octet-stream");
                            }
                        }


                    }

                    //
                }

            }

            catch (Exception ex)
            {
                string output = "";
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRRDownload.ToString(), "Error occurred in downloading XIRRReturn ", "", "", ex.TargetSite.Name.ToString(), "", ex);
                return File(output, "application/octet-stream");

            }


            finally
            {

            }
            return Ok(_actionResult);
        }

        [HttpPost]
        [Route("api/excelupload/downloadxirrreturnarchive")]
        //public HttpResponseMessage GenerateAccountingReport([FromBody] string ReportFileGUID)
        public async Task<IActionResult> DownloadXIRRReturnArchive([FromBody] XIRRConfigDataContract xirrConfig)
        {
            ReportFileDataContract reportDC = new ReportFileDataContract();
            reportDC.ReportFileName = "XIRR_Output";
            reportDC.ReportFileFormat = "xlsx";
            reportDC.SourceStorageLocation = "ExcelTemplate";
            reportDC.SourceStorageTypeID = 641;
            reportDC.ReportFileTemplate = "XIRR_Output_Archive" + "." + reportDC.ReportFileFormat;

            AccountingReportLogic reportLogic = new AccountingReportLogic();
            GenericResult _actionResult = null;
            var headerUserID = string.Empty;
            string CurrentDate = System.DateTime.Now.ToString("yyyyMMdd");
            TagXIRRLogic xirrLogic = new TagXIRRLogic();
            try
            {
                if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                {
                    headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
                }

                if (reportDC.ReportFileFormat.ToLower() == "xlsx")
                {
                    //read template from source
                    using (var memStream = await AzureStorageReadFile.ReadAccountingReportInStream(reportDC))
                    {

                        DataSet dsXIRRData = new DataSet();
                        //get the data from the database 
                        DataTable dtXIRR = new DataTable();
                        dtXIRR = xirrLogic.GetXIRROutputArchive(xirrConfig.XIRRConfigID, Convert.ToDateTime(xirrConfig.ArchiveDate),headerUserID);
                        dsXIRRData.Tables.Add(dtXIRR);
                        // write data to stream
                        using (var package = new OfficeOpenXml.ExcelPackage(memStream))
                        {

                            //Step 2 : Add a new worksheet to ExcelPackage object and give a suitable name

                            int iSheetsCount = 0;
                            try
                            {
                                package.Workbook.Worksheets[0].Name = "XIRR Return";
                                iSheetsCount = package.Workbook.Worksheets.Count;
                            }
                            catch (Exception)
                            {
                                iSheetsCount = package.Workbook.Worksheets.Count;
                            }
                            if (iSheetsCount > 0)
                            {

                                // Get the sheet by index
                                OfficeOpenXml.ExcelWorksheet worksheet;
                                try
                                {
                                    worksheet = package.Workbook.Worksheets[0];
                                }
                                catch (Exception)
                                {
                                    worksheet = package.Workbook.Worksheets[0];
                                }

                                worksheet.Cells[2, 1].LoadFromDataTable(dsXIRRData.Tables[0], false);
                                var stream = new MemoryStream(package.GetAsByteArray());
                                return File(stream, "application/octet-stream");
                            }
                        }


                    }

                    //
                }

            }

            catch (Exception ex)
            {
                string output = "";
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRRDownload.ToString(), "Error occurred in downloading XIRRReturn ", "", "", ex.TargetSite.Name.ToString(), "", ex);
                return File(output, "application/octet-stream");

            }


            finally
            {

            }
            return Ok(_actionResult);
        }

        [HttpGet]
        [Route("api/excelupload/CheckAPI")]
        public IActionResult CheckAPI(string DealID, string type)
        {
            v1GenericResult _authenticationResult = null;
            try
            {
                ImportServicerBalance();                
                
                _authenticationResult = new v1GenericResult()
                {
                    Status = Microsoft.AspNetCore.Http.StatusCodes.Status200OK,
                    Succeeded = true,
                    Message = "Succeeded CheckAutomation for deal " + DealID,
                    ErrorDetails = ""
                };

            }
            catch (Exception ex)
            {
                _authenticationResult = new v1GenericResult()
                {
                    Status = Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError,
                    Succeeded = false,
                    Message = "Error contact administrator",
                    ErrorDetails = ex.Message
                };

            }
            return Ok(_authenticationResult);
        }

        public async Task<IActionResult> UploadXIRRFiles(ReportFileDataContract reportDC, DataTable dt)
        {
            //ReportFileDataContract reportDC = new ReportFileDataContract();
            //reportDC.ReportFileName = "XIRR_Output";
            //reportDC.ReportFileFormat = "xlsx";
            //reportDC.SourceStorageLocation = "ExcelTemplate";
            //reportDC.SourceStorageTypeID = 641;
            //reportDC.ReportFileTemplate = "XIRR_Output_Archive" + "." + reportDC.ReportFileFormat;

            AccountingReportLogic reportLogic = new AccountingReportLogic();
            GenericResult _actionResult = null;
            var headerUserID = string.Empty;
            string CurrentDate = System.DateTime.Now.ToString("yyyyMMdd");
            TagXIRRLogic xirrLogic = new TagXIRRLogic();
            try
            {
                //if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                //{
                //    headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
                //}

                if (reportDC.ReportFileFormat.ToLower() == "xlsx")
                {
                    //read template from source
                    using (var memStream = await AzureStorageReadFile.ReadAccountingReportInStream(reportDC))
                    {

                        DataSet dsXIRRData = new DataSet();
                        //get the data from the database 
                        //DataTable dtXIRR = new DataTable();
                        //dtXIRR = xirrLogic.GetXIRROutputArchive(xirrConfig.XIRRConfigID, Convert.ToDateTime(xirrConfig.ArchiveDate), headerUserID);
                        dsXIRRData.Tables.Add(dt);
                        // write data to stream
                        using (var package = new OfficeOpenXml.ExcelPackage(memStream))
                        {

                            //Step 2 : Add a new worksheet to ExcelPackage object and give a suitable name

                            int iSheetsCount = 0;
                            try
                            {
                                package.Workbook.Worksheets[0].Name = reportDC.ReportFileName;
                                iSheetsCount = package.Workbook.Worksheets.Count;
                            }
                            catch (Exception)
                            {
                                iSheetsCount = package.Workbook.Worksheets.Count;
                            }
                            if (iSheetsCount > 0)
                            {

                                // Get the sheet by index
                                OfficeOpenXml.ExcelWorksheet worksheet;
                                try
                                {
                                    worksheet = package.Workbook.Worksheets[0];
                                }
                                catch (Exception)
                                {
                                    worksheet = package.Workbook.Worksheets[0];
                                }

                                worksheet.Cells[2, 1].LoadFromDataTable(dsXIRRData.Tables[0], false);
                                var stream = new MemoryStream(package.GetAsByteArray());

                                //upload file to destination
                                FileUploadParameterDataContract fileParam = new FileUploadParameterDataContract();
                                fileParam.StorageTypeID = reportDC.DestinationStorageTypeID;
                                fileParam.StorageLocation = reportDC.DestinationStorageLocation;
                                fileParam.FileName = reportDC.NewFileName;
                                await UploadFilesByStorageType(fileParam, stream);//
                                stream.Flush();
                            }
                        }
                    }

                    //
                }

            }

            catch (Exception ex)
            {
                string output = "";
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRRDownload.ToString(), "Error occurred in downloading XIRRReturn ", "", "", ex.TargetSite.Name.ToString(), "", ex);
                return File(output, "application/octet-stream");

            }


            finally
            {

            }
            return Ok(_actionResult);
        }

        public async Task<IActionResult> UploadXIRRFiles(ReportFileDataContract reportDC, DataTable dt,bool IsPrintHeader,int HeaderPosition)
        {
            //ReportFileDataContract reportDC = new ReportFileDataContract();
            //reportDC.ReportFileName = "XIRR_Output";
            //reportDC.ReportFileFormat = "xlsx";
            //reportDC.SourceStorageLocation = "ExcelTemplate";
            //reportDC.SourceStorageTypeID = 641;
            //reportDC.ReportFileTemplate = "XIRR_Output_Archive" + "." + reportDC.ReportFileFormat;

            AccountingReportLogic reportLogic = new AccountingReportLogic();
            GenericResult _actionResult = null;
            var headerUserID = string.Empty;
            string CurrentDate = System.DateTime.Now.ToString("yyyyMMdd");
            TagXIRRLogic xirrLogic = new TagXIRRLogic();
            try
            {
                //if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                //{
                //    headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
                //}

                if (reportDC.ReportFileFormat.ToLower() == "xlsx")
                {
                    //read template from source
                    using (var memStream = await AzureStorageReadFile.ReadAccountingReportInStream(reportDC))
                    {

                        DataSet dsXIRRData = new DataSet();
                        //get the data from the database 
                        //DataTable dtXIRR = new DataTable();
                        //dtXIRR = xirrLogic.GetXIRROutputArchive(xirrConfig.XIRRConfigID, Convert.ToDateTime(xirrConfig.ArchiveDate), headerUserID);
                        dsXIRRData.Tables.Add(dt);
                        // write data to stream
                        using (var package = new OfficeOpenXml.ExcelPackage(memStream))
                        {

                            //Step 2 : Add a new worksheet to ExcelPackage object and give a suitable name

                            int iSheetsCount = 0;
                            try
                            {
                                //package.Workbook.Worksheets[0].Name = reportDC.ReportFileName;
                                iSheetsCount = package.Workbook.Worksheets.Count;
                            }
                            catch (Exception)
                            {
                                iSheetsCount = package.Workbook.Worksheets.Count;
                            }
                            if (iSheetsCount > 0)
                            {

                                // Get the sheet by index
                                OfficeOpenXml.ExcelWorksheet worksheet;
                                try
                                {
                                    worksheet = package.Workbook.Worksheets[0];
                                }
                                catch (Exception)
                                {
                                    worksheet = package.Workbook.Worksheets[0];
                                }

                                worksheet.Cells[HeaderPosition, 1].LoadFromDataTable(dsXIRRData.Tables[0], IsPrintHeader);
                                var stream = new MemoryStream(package.GetAsByteArray());

                                //upload file to destination
                                FileUploadParameterDataContract fileParam = new FileUploadParameterDataContract();
                                fileParam.StorageTypeID = reportDC.DestinationStorageTypeID;
                                fileParam.StorageLocation = reportDC.DestinationStorageLocation;
                                fileParam.FileName = reportDC.NewFileName;
                                await UploadFilesByStorageType(fileParam, stream);//
                                stream.Flush();
                            }
                        }
                    }

                    //
                }

            }

            catch (Exception ex)
            {
                string output = "";
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRRDownload.ToString(), "Error occurred in downloading XIRRReturn ", "", "", ex.TargetSite.Name.ToString(), "", ex);
                return File(output, "application/octet-stream");

            }


            finally
            {

            }
            return Ok(_actionResult);
        }

        public async Task<IActionResult> UploadXIRRFiles(ReportFileDataContract reportDC, DataSet dsXIRRData, bool IsPrintHeader)
        {
            //ReportFileDataContract reportDC = new ReportFileDataContract();
            //reportDC.ReportFileName = "XIRR_Output";
            //reportDC.ReportFileFormat = "xlsx";
            //reportDC.SourceStorageLocation = "ExcelTemplate";
            //reportDC.SourceStorageTypeID = 641;
            //reportDC.ReportFileTemplate = "XIRR_Output_Archive" + "." + reportDC.ReportFileFormat;
            string DocumentStorageID = "";
            AccountingReportLogic reportLogic = new AccountingReportLogic();
            GenericResult _actionResult = null;
            var headerUserID = string.Empty;
            string CurrentDate = System.DateTime.Now.ToString("yyyyMMdd");
            TagXIRRLogic xirrLogic = new TagXIRRLogic();
            try
            {
                //if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                //{
                //    headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
                //}

                if (reportDC.ReportFileFormat.ToLower() == "xlsx")
                {
                    
                    //read template from source
                    using (var memStream = await AzureStorageReadFile.ReadAccountingReportInStream(reportDC))
                    {
                        DocumentStorageID = await WriteDataToStreamWithoutSheet(reportDC, dsXIRRData, memStream);
                    }
                }
            }
            catch (Exception ex)
            {
                string output = "";
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRRDownload.ToString(), "Error occurred in downloading XIRRReturn ", "", "", ex.TargetSite.Name.ToString(), "", ex);
                return File(output, "application/octet-stream");

            }

            finally
            {

            }
            return Ok(_actionResult);
        }

        [HttpPost]
        [Route("api/excelupload/downloadxirroutputfiles")]
        //public HttpResponseMessage GenerateAccountingReport([FromBody] string ReportFileGUID)
        public async Task<IActionResult> DownloadXIRROutputFiles([FromBody] XIRRConfigDataContract xirrConfig)
        {
            ReportFileDataContract reportDC = new ReportFileDataContract();
            DataTable dt = new DataTable();
            ExceluploadController exc = new ExceluploadController();
            reportDC.SourceStorageLocation = "XIRRTemplates";
            reportDC.ReportFileFormat = "xlsx";
            reportDC.SourceStorageTypeID = 392;
            reportDC.DestinationStorageTypeID = 392;
            reportDC.ReportFileTemplate = "XIRR_Output_PortfolioLevel" + "." + reportDC.ReportFileFormat;
            if (xirrConfig.Type == "Deal")
            {
                reportDC.ReportFileTemplate = "XIRR_Output_DealLevel" + "." + reportDC.ReportFileFormat;
            }

            AccountingReportLogic reportLogic = new AccountingReportLogic();
            GenericResult _actionResult = null;
            var headerUserID = string.Empty;
            string CurrentDate = System.DateTime.Now.ToString("yyyyMMdd");
            TagXIRRLogic xirrLogic = new TagXIRRLogic();
            try
            {
                if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                {
                    headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
                }

                if (reportDC.ReportFileFormat.ToLower() == "xlsx")
                {
                    //read template from source
                    using (var memStream = await AzureStorageReadFile.ReadAccountingReportInStream(reportDC))
                    {
                        DataSet dsXIRRData = new DataSet();

                        if (xirrConfig.Type != "Deal")
                        {
                            DataTable dtportfolio = xirrLogic.GetXIRROutputPortfolioLevel(xirrConfig.XIRRConfigID, "");

                            if (dtportfolio.Columns.IndexOf("XIRRConfigID") != -1)
                            {
                                dtportfolio.Columns.Remove("XIRRConfigID");
                            }

                            if (dtportfolio.Columns.IndexOf("G1_Hidden") != -1)
                            {
                                dtportfolio.Columns.Remove("G1_Hidden");
                            }
                            dsXIRRData.Tables.Add(dtportfolio);
                        }
                        DataTable dtdeal = xirrLogic.GetXIRROutputDealLevel(xirrConfig.XIRRConfigID, "");

                        if (dtdeal.Columns.IndexOf("XIRRConfigID") != -1)
                        {
                            dtdeal.Columns.Remove("XIRRConfigID");
                        }

                        if (dtdeal.Columns.IndexOf("XIRRReturnGroupID") != -1)
                        {
                            dtdeal.Columns.Remove("XIRRReturnGroupID");
                        }
                        dsXIRRData.Tables.Add(dtdeal);

                        // write data to stream
                        using (var package = new OfficeOpenXml.ExcelPackage(memStream))
                        {

                            //Step 2 : Add a new worksheet to ExcelPackage object and give a suitable name

                            int iSheetsCount = 0;
                            try
                            {
                                //package.Workbook.Worksheets[0].Name = "XIRR Return";
                                iSheetsCount = package.Workbook.Worksheets.Count;
                            }
                            catch (Exception)
                            {
                                iSheetsCount = package.Workbook.Worksheets.Count;
                            }
                            if (iSheetsCount > 0)
                            {

                                for (int i = 0; i < iSheetsCount; i++)
                                {
                                    // Get the sheet by index
                                    OfficeOpenXml.ExcelWorksheet worksheet;
                                    try
                                    {
                                        worksheet = package.Workbook.Worksheets[i];
                                    }
                                    catch (Exception)
                                    {
                                        worksheet = package.Workbook.Worksheets[i];
                                    }

                                    worksheet.Cells[1, 1].LoadFromDataTable(dsXIRRData.Tables[i], true);
                                }
                                var stream = new MemoryStream(package.GetAsByteArray());
                                return File(stream, "application/octet-stream");
                            }
                        }


                    }

                    //
                }

            }

            catch (Exception ex)
            {
                string output = "";
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.XIRRDownload.ToString(), "Error occurred in downloading XIRRReturn ", "", "", ex.TargetSite.Name.ToString(), "", ex);
                return File(output, "application/octet-stream");

            }


            finally
            {

            }
            return Ok(_actionResult);
        }

    }




}