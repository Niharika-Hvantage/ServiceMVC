using System.Collections.Generic;
using CRES.DataContract;
using CRES.BusinessLogic;
using System.Net.Http;
using System.Linq;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Data;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using CRES.BusinessLogic;
using Microsoft.Extensions.Configuration;

namespace CRES.Services.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class TagMasterController : ControllerBase
    {
        private TagMasterLogic _TagMasterLogic = new TagMasterLogic();

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/tags/GetTagMaster")]
        public IActionResult GetTagMaster([FromBody] TagMasterDataContract tmdc)
        {

            GenericResult _authenticationResult = null;
            List<TagMasterDataContract> lstTagMaster = new List<TagMasterDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            UserPermissionLogic upl = new UserPermissionLogic();
            List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "Tag_Master");

            if (permissionlist.Count > 0)
            {
                lstTagMaster = _TagMasterLogic.GetTagMaster(headerUserID, new Guid("00000000-0000-0000-0000-000000000000"));
            }

            

            try
            {
                if (lstTagMaster != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TagMasterList = lstTagMaster,
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
        [Route("api/tags/InsertTagMaster")]
        public IActionResult InsertTagMaster([FromBody] TagMasterDataContract tmdc)
        {

            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            string res = _TagMasterLogic.InsertTagMaster(tmdc, headerUserID);

            try
            {
                if (res == "TRUE")
                {

                    Thread SecondThread = new Thread(() => ImportIntoTransactionEntryClose(null, null, null, new Guid(headerUserID), tmdc.TagMasterID, tmdc.AnalysisID));
                    SecondThread.Start();

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TagMasterDC = tmdc
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


        public void ImportIntoTransactionEntryClose(DateTime? StartDate, DateTime? EndDate, Guid? PeriodId, Guid? userID, Guid? TagMasterID, Guid? AnalysisID)
        {
            PeriodicLogic _periodicLogic = new PeriodicLogic();
            _periodicLogic.ImportIntoTransactionEntryClose(StartDate, EndDate, PeriodId, userID, TagMasterID, AnalysisID);

        }

        public void ImportIntoTransactionEntryCloseArchive(Guid? TagMasterID, Guid? AnalysisID)
        {
            _TagMasterLogic.ImportIntoTransactionEntryCloseArchive(TagMasterID, AnalysisID);

        }


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/tags/GetNoteCashflowsExportDataFromTransactionClose")]
        public IActionResult GetNoteCashflowsExportDataFromTransactionClose([FromBody] TagMasterDataContract tmdc)
        {
            GenericResult _authenticationResult = null;
            DataTable tagdatatable = new DataTable();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            TagMasterLogic tagLogic = new TagMasterLogic();

            tagdatatable = tagLogic.GetNoteCashflowsExportDataFromTransactionClose(tmdc.AnalysisID.ToString(), tmdc.TagMasterID.ToString());

            try
            {
                if (tagdatatable != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        dttagWiseCashflow = tagdatatable,
                        TotalCount = 0,
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
        [Route("api/tags/UploadNoteCashflowsExportDataFromTransactionCloseToAzure")]
        public IActionResult UploadNoteCashflowsExportDataFromTransactionCloseToAzure([FromBody] TagMasterDataContract tmdc)
        {
            bool isSuccess = false;
            List<TagFileDataContract> lstTagFile = new List<TagFileDataContract>();
            GenericResult _authenticationResult = null;
            DataTable tagdatatable = new DataTable();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            TagMasterLogic tagLogic = new TagMasterLogic();

         try
            {
            if (string.IsNullOrEmpty(tmdc.TagFileName))
            {
                tagdatatable = tagLogic.GetNoteCashflowsExportDataFromTransactionClose(tmdc.AnalysisID.ToString(), tmdc.TagMasterID.ToString());
               
                isSuccess = UploadDataTableToAzureblob(tagdatatable, tmdc.NewTagFileName);
                if (isSuccess)
                {
                    lstTagFile.Add(new TagFileDataContract { TagMasterID = new Guid(tmdc.TagMasterID.ToString()), TagFileName = tmdc.NewTagFileName });
                    _TagMasterLogic.UpdateTagFileName(lstTagFile);
                    lstTagFile.Clear();
                    Thread SecondThread = new Thread(() => ImportIntoTransactionEntryCloseArchive(tmdc.TagMasterID, tmdc.AnalysisID));
                    SecondThread.Start();
                    }
            }
            _authenticationResult = new GenericResult()
            {
                TotalCount = 0,
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

        public bool UploadDataTableToAzureblob(System.Data.DataTable dt, string csvname)
        {
            GetConfigSetting();
            System.Text.UnicodeEncoding uniEncoding = new System.Text.UnicodeEncoding();

            MemoryStream ms1 = new MemoryStream();

            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    StreamWriter sw = new StreamWriter(ms);
                    int columnCount = dt.Columns.Count;

                    for (int i = 0; i < columnCount; i++)
                    {
                        sw.Write(dt.Columns[i]);

                        if (i < columnCount - 1)
                        {
                            sw.Write(",");
                        }
                    }

                    sw.Write(sw.NewLine);

                    foreach (DataRow dr in dt.Rows)
                    {
                        for (int i = 0; i < columnCount; i++)
                        {
                            if (!Convert.IsDBNull(dr[i]))
                            {
                                sw.Write(dr[i].ToString());
                            }

                            if (i < columnCount - 1)
                            {
                                sw.Write(",");
                            }
                        }

                        sw.Write(sw.NewLine);
                    }
                    ms1 = ms;

                    //var Container = System.Configuration.ConfigurationManager.AppSettings["storage:container:name"];
                    var Container = Sectionroot.GetSection("storage:container:name").Value;

                    // Get Blob Container
                    Microsoft.WindowsAzure.Storage.Blob.CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);
                    // Get reference to blob (binary content)
                    Microsoft.WindowsAzure.Storage.Blob.CloudBlockBlob blockBlob = container.GetBlockBlobReference(csvname);
                    blockBlob.Properties.ContentType = "application/octet-stream";
                    ms1.Seek(0, SeekOrigin.Begin);
                    blockBlob.UploadFromStream(ms1);
                }


                return true;

            }
            catch (Exception ex)
            {
                return true;
            }

        }



        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/tags/DeleteTagByTagID")]
        public IActionResult DeleteTagByTagID([FromBody] TagMasterDataContract tmdc)
        {

            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            _TagMasterLogic.DeleteTagByTagID(headerUserID, tmdc.AnalysisID, tmdc.TagMasterID);

            try
            {


                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Deleted successfully",
                    TagMasterDC = tmdc
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

    }
}