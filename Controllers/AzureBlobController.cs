using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CRES.DataContract;
using CRES.Services;
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
using System;
using System.Data;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;

namespace CRES.ServicesNew.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class AzureBlobController : ControllerBase
    {
        private readonly IEmailNotification _iEmailNotification;
        private IHostingEnvironment _env;
        private string useridforSys_Scheduler = "3D6DB33D-2B3A-4415-991D-A3DA5CEB8B50";

        [HttpGet]
        [Route("api/azureblob/deleteblobfile")]
        public IActionResult DeleteBlobFile()
        {
            List<string> FilesDealeted = new List<string>();
            LoggerLogic Log = new LoggerLogic();
            v1GenericResult _authenticationResult = null;
            DealLogic dealLogic = new DealLogic();

            DataTable dt = new DataTable();
            dt = dealLogic.GetAllDataForBlobFileDelete();
            if (dt != null)
            {
                string FolderName = "";
                int numberOfDays;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    List<string> BlobFiles = new List<string>();
                    FolderName = dt.Rows[i]["FolderName"].ToString();
                    numberOfDays = Convert.ToInt16(dt.Rows[i]["DeletedDays"]);
                    BlobFiles = AzureStorageReadFile.DeleteBlobFileByNumberofdays(FolderName, numberOfDays);


                    foreach (var item in BlobFiles)
                    {
                        FilesDealeted.Add(item.ToString());
                    }
                    if (BlobFiles != null)
                    {
                        string[] arry = BlobFiles.ToArray();
                        string arraystring = string.Join(",", arry);
                        arraystring = arraystring.Replace(FolderName + "/", "");

                        string message = "Files Delete for Container :" + FolderName + " Count :" + BlobFiles.Count + " Files Name :" + arraystring;
                        Log.WriteLogInfo(CRESEnums.Module.DeleteBlobFile.ToString(), message, "", useridforSys_Scheduler);
                    }
                }
            }
            else
            {
                Log.WriteLogInfo(CRESEnums.Module.DeleteBlobFile.ToString(), "No Files to delete ", "", useridforSys_Scheduler);
            }
            try
            {
                _authenticationResult = new v1GenericResult()
                {
                    Status = Microsoft.AspNetCore.Http.StatusCodes.Status200OK,
                    Succeeded = true,
                    Message = "File Deleted from Blob",
                    ErrorDetails = ""
                };

            }
            catch (Exception ex)
            {
                Log.WriteLogException(CRESEnums.Module.DeleteBlobFile.ToString(), "Error occurred  in DeleteBlobFile" + ex.Message, "", useridforSys_Scheduler, ex.Source, "", ex);
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

        [HttpGet]
        [Route("api/azureblob/deletexirrblobfile")]
        public IActionResult DeleteXIRRBlobFile()
        {
            GenericResult _authenticationResult = null;
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
        
            LoggerLogic Log = new LoggerLogic();
         

            DataTable dt = tagXIRRLogic.GetAllFileNameXIRR();

            AzureStorageReadFile.DeleteBlobFileByFileName(dt);
            //  BlobFiles = AzureStorageReadFile.DeleteBlobFileByNumberofdays(DeletedFolderName, Convert.ToInt32(deletedDays));

            try
            {
                _authenticationResult = new GenericResult()
                {

                    Succeeded = true,
                    Message = "File deleted from Blob",

                };

            }
            catch (Exception ex)
            {
                Log.WriteLogException(CRESEnums.Module.DeleteBlobFile.ToString(), "Error occurred  in DeleteBlobFile" + ex.Message, "", useridforSys_Scheduler, ex.Source, "", ex);
                _authenticationResult = new GenericResult()
                {

                    Succeeded = false,
                    Message = "Error contact administrator",

                };

            }
            return Ok(_authenticationResult);

        }

    }
}
