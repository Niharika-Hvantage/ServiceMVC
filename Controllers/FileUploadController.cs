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
using System.Drawing;
using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using CRES.Services;
using System.IO;
using System.Data;
using CRES.Services.Infrastructure;
using Microsoft.Azure;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using CRES.BusinessLogic;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Hosting;

namespace CRES.ServicesNew.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class FileUploadController : ControllerBase
    {
        private IHostingEnvironment _env;
        private readonly string[] _supportedMimeTypes = { "image/png", "image/jpeg", "image/jpg", "csv/csv", "xlsx/xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/pdf", "application/msword" };
        private readonly string[] _supportedExtensions = { "png", "jpeg", "jpg", "csv", "xlsx","xls","doc","docx","pdf"};
        private DocumentLogic _documentLogic = new DocumentLogic();
        private static readonly IEmailNotification _iEmailNotification;
        string AzureFileName = "";
        //private const string Container = "images";

        //[HttpPost]
        //[Route("api/fileupload/uploadfiletofolder")]
        //public HttpResponseMessage UploadFileToFolder()
        //{
        //    HttpResponseMessage response = new HttpResponseMessage();
        //    var httpRequest = HttpContext.Current.Request;
        //    if (httpRequest.Files.Count > 0)
        //    {
        //        foreach (string file in httpRequest.Files)
        //        {
        //            var postedFile = httpRequest.Files[file];
        //            var filePath = HttpContext.Current.Server.MapPath("~/UploadFile/" + postedFile.FileName);
        //            postedFile.SaveAs(filePath);
        //        }
        //    }
        //    return response;
        //}

        [HttpPost]
        [Route("api/fileupload/uploadcsvfiletoazureblob")]
        public async Task<IActionResult> UploadCSVFileToAzureBlob()
        {
            GetConfigSetting();
            var files = Request.Form.Files;

            DocumentDataContract _docDC = new DocumentDataContract();
            List<FileDetail> fDetail = new List<FileDetail>();

            //var Container = ConfigurationManager.AppSettings["storage:container:name"];
            var Container = Sectionroot.GetSection("storage:container:name").Value;


            var provider = new FileExtensionContentTypeProvider();
            string contentType;
            bool isMimeTypeSupported = true;

            if (files.Count() > 0)
            {
                for (int i = 0; i < files.Count; i++)
                {
                    string fileName = ContentDispositionHeaderValue.Parse(files[i].ContentDisposition).FileName.Trim('"');
                    string fileExtension = Path.GetExtension(fileName).Replace(".", "").ToLower();
                    provider.TryGetContentType(fileName, out contentType);

                    if (!_supportedMimeTypes.Contains(contentType.ToLower()))
                    {
                        if (!_supportedExtensions.Contains(fileExtension))
                        {
                            isMimeTypeSupported = false;
                            break;
                        }

                    }
                }

            }

            if (!isMimeTypeSupported)
            {
                return BadRequest($"Fail==Only jpeg,png,csv,xlsx,pdf,doc are supported");
            }


            var headerUserID = String.Empty;
            string _fromDate = string.Empty, _todate = string.Empty, isconfirmed = string.Empty;

            var queryString = HttpContext.Request.Query;
            if (!string.IsNullOrEmpty(queryString["userid"]))
            {
                headerUserID = queryString["userid"];
                _fromDate = queryString["Fromdate"];
                _todate = queryString["ToDate"];
                isconfirmed = queryString["isconfirmed"];
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
                
                if (files.Count() > 0)
                {
                    for (int i = 0; i < files.Count; i++)
                    {
                        string fileName = ContentDispositionHeaderValue.Parse(files[i].ContentDisposition).FileName.Trim('"');
                        string UniqueFileName = GetUniqueFileName(fileName);
                        fDetail.Add(new FileDetail { FileName = fileName, NewFileName = UniqueFileName });
                        using (var stream = files[i].OpenReadStream())
                        {
                            CloudBlockBlob cloudBlockBlob = excelContainer.GetBlockBlobReference(UniqueFileName);
                            await cloudBlockBlob.UploadFromStreamAsync(stream);
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                return BadRequest($"Fail==An error has occured. Details: {ex.Message}");
            }
            //

            var sourceBlobFileName = fDetail[0].NewFileName;

            // Retrieve the filename of the file you have uploaded
            //  var filename = ((provider.FileData.FirstOrDefault()?.Headers).ContentDisposition).FileName.Replace("\"", ""); 
            var fileDisplayName = fDetail[0].FileName.Replace("\"", "");

            if (string.IsNullOrEmpty(sourceBlobFileName))
            {
                return BadRequest($"Fail==An error occurred during the upload, please try again or contact M61 Support.");
            }


            AzureStorageReadFile azureStorageReadFile = new AzureStorageReadFile();
            DataTable dtExcel = azureStorageReadFile.GetExcelData(sourceBlobFileName, fileDisplayName, _fromDate, _todate, isconfirmed);
            if (dtExcel.Rows.Count > 0)
            {

                if (dtExcel.Rows[0]["ErrorMessage"].ToString() != "")
                {
                    // return Ok($"Fail==File: {fileDisplayName}  is not in correct format.");
                    CloudBlockBlob _blockBlob = excelContainer.GetBlockBlobReference(sourceBlobFileName);
                    _blockBlob.Delete();
                    //  return Ok($"Fail==File: {fileDisplayName}  " + dtExcel.Rows[0]["ErrorMessage"]);
                    return Ok($"Fail==" + dtExcel.Rows[0]["ErrorMessage"]);

                }
                else
                {
                    dtExcel.Columns.Remove("ErrorMessage");
                }
            }
            ImportServicingLogLogic _importservicinglog = new ImportServicingLogLogic();
            int result = _importservicinglog.ImportIntoINServicingTransaction(dtExcel, headerUserID.ToString(), sourceBlobFileName, fileDisplayName, storagetype, _fromDate, _todate);
            if (result != 0)
            {
                return Ok($"Success==File:  { fileDisplayName} has successfully uploaded.");
                //  return Ok($"Success==File:" + {fileDisplayName} + "has successfully uploaded");
                //  return Ok($"File: {fileDisplayName} has successfully uploaded");
            }
            else
            {
                return Ok($"Fail==Upload Failed.Some Error occured.Please try after some time.");
                //  return BadRequest($"Upload Failed.Some Error occured.Please try after some time.");
            }
        }


        //[HttpPost]
        //[Route("api/fileupload/uploadfiletoazureblob")]
        //public async Task<IHttpActionResult> UploadFileToAzureBlob()
        //{
        //    var Container = ConfigurationManager.AppSettings["storage:container:name"];

        //    if (!Request.Content.IsMimeMultipartContent("form-data"))
        //    {
        //        throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
        //    }

        //    var accountName = ConfigurationManager.AppSettings["storage:account:name"];
        //    var accountKey = ConfigurationManager.AppSettings["storage:account:key"];
        //    var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
        //    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

        //    CloudBlobContainer imagesContainer = blobClient.GetContainerReference(Container);
        //    var provider = new AzureStorageMultipartFormDataStreamProvider(imagesContainer);

        //    try
        //    {
        //        await Request.Content.ReadAsMultipartAsync(provider);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest($"An error has occured. Details: {ex.Message}");
        //    }

        //    // Retrieve the filename of the file you have uploaded
        //    var filename = provider.FileData.FirstOrDefault()?.LocalFileName;
        //    if (string.IsNullOrEmpty(filename))
        //    {
        //        return BadRequest("An error has occured while uploading your file. Please try again.");
        //    }

        //    return Ok($"File: {filename} has successfully uploaded");
        //}

        [HttpPost]
        [Route("api/fileupload/uploadobjectfiletoazureblob")]
        public async Task<IActionResult> UploadObjectFileToAzureBlob()
        {
            GetConfigSetting();
            var files = Request.Form.Files;
            DocumentDataContract _docDC = new DocumentDataContract();
            List<FileDetail> fDetail = new List<FileDetail>();
            GenericResult _authenticationResult = null;
            //var Container = ConfigurationManager.AppSettings["storage:container:name"];
            var Container = Sectionroot.GetSection("storage:container:name").Value;


            var provider = new FileExtensionContentTypeProvider();
            string contentType;
            bool isMimeTypeSupported = true;

            if (files.Count() > 0)
            {
                for (int i = 0; i < files.Count; i++)
                {
                    string fileName = ContentDispositionHeaderValue.Parse(files[i].ContentDisposition).FileName.Trim('"');
                    string fileExtension = Path.GetExtension(fileName).Replace(".","").ToLower();
                    provider.TryGetContentType(fileName, out contentType);

                    if (!_supportedMimeTypes.Contains(contentType.ToLower()))
                    {
                        if (!_supportedExtensions.Contains(fileExtension))
                        {
                            isMimeTypeSupported = false;
                            break;
                        }
                        
                    }
                }

            }

            if (!isMimeTypeSupported)
            {
                return BadRequest($"Fail==Only jpeg,png,csv,xlsx,pdf,doc are supported");
            }

            var headerUserID = String.Empty;

            var queryString = HttpContext.Request.Query;

            if (queryString["userid"] != "")
            {
                headerUserID = queryString["userid"];
                _docDC.CreatedBy = headerUserID;
            }

            if (queryString["comment"] != "")
            {
                _docDC.Comment = queryString["comment"];
            }

            if (queryString["documentTypeID"] != "")
            {
                _docDC.DocumentTypeID = queryString["documentTypeID"];
            }

            if (queryString["ObjectID"] != "")
            {
                _docDC.ObjectID = queryString["ObjectID"];
            }

            if (queryString["ObjectTypeID"] != "")
            {
                _docDC.ObjectTypeID = queryString["ObjectTypeID"];
            }


            //var storagetype = "AzureBlob";
            try
            {
            var accountName = Sectionroot.GetSection("storage:account:name").Value;
            var accountKey = Sectionroot.GetSection("storage:account:key").Value;
            var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer excelContainer = blobClient.GetContainerReference(Container);

            if (files.Count()> 0)
            {
                    for (int i = 0; i < files.Count; i++)
                    {
                        string fileName = ContentDispositionHeaderValue.Parse(files[i].ContentDisposition).FileName.Trim('"');
                        string UniqueFileName = GetUniqueFileName(fileName);
                        fDetail.Add(new FileDetail {FileName= fileName,NewFileName= UniqueFileName });
                        using (var stream = files[i].OpenReadStream())
                        {
                            CloudBlockBlob cloudBlockBlob = excelContainer.GetBlockBlobReference(UniqueFileName);
                            await cloudBlockBlob.UploadFromStreamAsync(stream);
                        }
                    }
                    
            }

            }
            catch (Exception ex)
            {
              //  return BadRequest($"Fail==An error has occured. Details: {ex.Message}");
                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = "Fail==An error has occured. Details: "+ ex.Message
                };
            }

            string result = "";
            for (int i = 0; i < fDetail.Count(); i++)
            {
                _docDC.FileName = fDetail[i].NewFileName;
                _docDC.OriginalFileName = fDetail[i].FileName;
                _docDC.Storagetype = "azureblob";

                AzureFileName = fDetail[i].NewFileName;
                result = _documentLogic.InsertUploadedDocumentLog(_docDC);
               
            }
          //  return Ok($"Success==File:  { ""} has successfully uploaded.");

            _authenticationResult = new GenericResult()
            {
                Succeeded = true,
                Message = "Success==File has successfully uploaded."
                
            };
            return Ok(_authenticationResult);
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/fileupload/getdocumentsbyobjectid")]
        public IActionResult GetDocumentsByObjectID([FromBody] DocumentDataContract _documentDC, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            List<DocumentDataContract> lstDocuments = new List<DocumentDataContract>();

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            int? totalCount = 0;
            //lstDocuments = _documentLogic.GetAllDocumentByObjectId(_documentDC.ObjectID, _documentDC.ObjectTypeID, _documentDC.CurrentTime, headerUserID, pageIndex, pageSize, out totalCount);
            lstDocuments = _documentLogic.GetAllDocumentByObjectId(_documentDC, headerUserID, pageIndex, pageSize, out totalCount);

            try
            {
                if (lstDocuments != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        lstDocument = lstDocuments
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


        ///*
        //[HttpPost]
        //[Route("api/fileupload/uploadfiletoazureblob")]
        //public  async void DownfileFromAzureBlob()
        //{
        //    //BlobWrapper _blobWrapper = new BlobWrapper();
        //    Task<byte[]> data = DownloadFromBlob("123.docx");
        //    byte[] d = await data;
        //}

        //public async Task<byte[]> DownloadFromBlob(string FileName)
        //{

        //    // Get Blob Container
        //    CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference("documents");

        //    // Get reference to blob (binary content)
        //    CloudBlockBlob blockBlob = container.GetBlockBlobReference(FileName);

        //    // Read content
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        blockBlob.DownloadToStream(ms);
        //        return ms.ToArray();
        //    }
        //}
        //*/

        [HttpPost]
        [Route("api/fileupload/updatedocumentstatus")]
        public IActionResult UpdateDocumentStatus([FromBody]List<DocumentDataContract> _docDC)
        {

            GenericResult _authenticationResult = null;
            List<DocumentDataContract> lstDocuments = new List<DocumentDataContract>();

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            try
            {
                if (_docDC != null && _docDC.Count > 0)
                    _documentLogic.UpdateDocumentStatus(_docDC, headerUserID);
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
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

        [HttpGet]//http get as it return file 
        [Route("api/fileupload/downloadfilefromlocalstorage")]
        public async Task<IActionResult> DownloadFileFromLocalStorage(string filePath, string fileName)
        {
            MemoryStream memStreamDownloaded = new MemoryStream();
            try
            {
                //string fileName = ID;
                //Create HTTP Response.
                //HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);

                //Set the File Path.
                //  string filePath = HttpContext.Current.Server.MapPath("~/Files/") + fileName;
                string fePath = System.IO.Directory.GetCurrentDirectory();
                //Check whether File exists.
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound();
                }

                //Read the File into a Byte Array.
                byte[] bytes = System.IO.File.ReadAllBytes(filePath);

                return File(bytes, "application/octet-stream");
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }



        [HttpGet]//http get as it return file 
        [Route("api/fileupload/downloadfilefromazureblob")]
        public async Task<IActionResult> DownloadFileFromAzureBlob(string ID)
        {
            GetConfigSetting();
            MemoryStream memStreamDownloaded = new MemoryStream();
            try
            {
                HttpResponseMessage response = null;
                var Container = Sectionroot.GetSection("storage:container:name").Value;

                string fileName = ID;
                // Get Blob Container
                CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);

                // Get reference to blob (binary content)
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);

                await blockBlob.DownloadToStreamAsync(memStreamDownloaded);

                MemoryStream memStream = new MemoryStream(memStreamDownloaded.ToArray());
                memStreamDownloaded.Dispose();
                return File(memStream, "application/octet-stream");
            }
            catch (Exception ex)
            {
                memStreamDownloaded.Dispose();
                return BadRequest(ex);
            }
        }

        //[HttpPost]
        //[Route("api/fileupload/uploadobjectfiletobox")]
        //public async Task<IHttpActionResult> UploadFileToBox()
        //{
        //    DocumentDataContract _docDC = new DocumentDataContract();


        //    if (!Request.Content.IsMimeMultipartContent("form-data"))
        //    {
        //        throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
        //    }


        //    var headerUserID = String.Empty;
        //    var queryString = Request.RequestUri.ParseQueryString();
        //    if (queryString["userid"] != null)
        //    {
        //        headerUserID = queryString["userid"];
        //        _docDC.CreatedBy = headerUserID;
        //    }

        //    if (queryString["comment"] != null)
        //    {
        //        _docDC.Comment = queryString["comment"];
        //    }

        //    if (queryString["documentTypeID"] != null)
        //    {
        //        _docDC.DocumentTypeID = queryString["documentTypeID"];
        //    }

        //    if (queryString["ObjectID"] != null)
        //    {
        //        _docDC.ObjectID = queryString["ObjectID"];
        //    }

        //    if (queryString["ObjectTypeID"] != null)
        //    {
        //        _docDC.ObjectTypeID = queryString["ObjectTypeID"];
        //    }

        //    if (queryString["FolderName"] != null)
        //    {
        //        _docDC.FolderName = queryString["FolderName"];
        //    }
        //    if (queryString["ParentFolderName"] != null)
        //    {
        //        _docDC.ParentFolderName = queryString["ParentFolderName"];
        //    }


        //    //var storagetype = "AzureBlob";

        //    var provider = new BoxStorageMultipartFormDataStreamProvider();

        //    try
        //    {
        //        await Request.Content.ReadAsMultipartAsync(provider);

        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest($"Fail==An error has occured. Details: {ex.Message}");
        //    }

        //    string result = "";
        //    var httpRequest = HttpContext.Current.Request;

        //    if (httpRequest.Files.Count > 0)
        //    {
        //        try
        //        {
        //            //create folder structure
        //            var folderid = await new BoxHelper().CheckAndCreateBoxFolder(_docDC.ParentFolderName, _docDC.FolderName);
        //            //
        //            for (int i = 0; i < httpRequest.Files.Count; i++)
        //            {
        //                var postedFile = httpRequest.Files[i];


        //                //string onyName = fileName.Substring(0, index);
        //                string fileExtension = "." + postedFile.FileName.Substring(postedFile.FileName.LastIndexOf('.') + 1);
        //                // Generate a new filename for every new blob
        //                _docDC.FileName = postedFile.FileName.Replace("\"", "").ToLower().Replace(fileExtension, "_" + System.DateTime.Now.ToString("yyyy-MM-dd-mm-ss") + fileExtension);
        //                _docDC.OriginalFileName = postedFile.FileName;
        //                _docDC.Storagetype = "Box";
        //                await new BoxHelper().UploadFileToFolder(folderid, _docDC, postedFile.InputStream);
        //                result = _documentLogic.InsertUploadedDocumentLog(_docDC);
        //            }

        //        }
        //        catch (Exception ex)
        //        {
        //            return Ok($"Fail==An error has occured. Details: {ex.Message}");
        //        }
        //    }

        //    return Ok($"Success==File:  { ""} has successfully uploaded.");
        //}


        //[HttpPost]
        //[Route("api/fileupload/uploadobjectfile")]
        //public async Task<IActionResult> UploadFilesByStorageType(List<IFormFile> files)
        //{
        //    IActionResult result = null;

        //    if (HttpContext.Request.Query["StorageType"] != "" && HttpContext.Request.Query["StorageType"].ToString().ToLower() == "box")
        //    {
        //        result = await UploadFileToBox();
        //    }
        //    else if (HttpContext.Request.Query["StorageType"] != "" && HttpContext.Request.Query["StorageType"].ToString().ToLower() == "azureblob")
        //    {
        //        result = await UploadObjectFileToAzureBlob();
        //    }

        //    //if (HttpContext.Request.Query["ObjectTypeID"] != "" && HttpContext.Request.Query["ObjectTypeID"].ToString().ToLower() == "588") // dump data in DB in case of wells daily extract
        //    //{

        //    //    string connectionString = CloudConfigurationManager.GetSetting("storage:container:connectionstring"); //blob connection string
        //    //    string sourceContainerName = ConfigurationManager.AppSettings["storage:container:name"]; //source blob container name
        //    //    AzureStorageReadFile azureStorageReadFile = new AzureStorageReadFile();
        //    //    DataSet dt= AzureStorageReadFile.GetExcelBlobData(AzureFileName, connectionString, sourceContainerName);
        //    //    if (dt.Tables["tblACOREDailyExtract"].Rows.Count > 0)
        //    //    {
        //    //        _documentLogic.WellsDailyExtractBulkInsert(dt.Tables["tblACOREDailyExtract"], "DW.ServicingTransactionBI");
        //    //    }
        //    //}

        //        return result;
        //}


        ////[HttpGet]//http get as it return file 
        ////[Route("api/fileupload/downloadfileFromBox")]
        ////public async Task DownloadFileFromBox(string ID)
        ////{

        ////    MemoryStream memStreamDownloaded = new MemoryStream();

        ////    DocumentDataContract _docDC = new DocumentDataContract();
        ////    _docDC.FileName = ID;
        ////    _docDC.BoxID = "319980043060";
        ////    //var result = new BoxHelper().DownloadFile(_docDC);
        ////    await new BoxHelper().DownloadFile(_docDC);
        ////}

        ////[HttpGet]//http get as it return file 
        ////[Route("api/fileupload/downloadfileFromBox")]
        ////public HttpResponseMessage DownloadFileFromBox(string ID)
        ////{


        ////    MemoryStream memStreamDownloaded = new MemoryStream();
        ////    try
        ////    {
        ////        HttpResponseMessage response = null;
        ////        DocumentDataContract _docDC = new DocumentDataContract();
        ////        _docDC.FileName = ID;
        ////        _docDC.BoxID = "319980043060";
        ////        //var result = new BoxHelper().DownloadFile(_docDC);

        ////       var result = new BoxHelper().DownloadFile(_docDC);

        ////        MemoryStream memStream = new MemoryStream();

        ////        //blockBlob.DownloadToStream(memStream);

        ////        response = new HttpResponseMessage
        ////        {
        ////            StatusCode = HttpStatusCode.OK,
        ////            Content = new StreamContent(memStream)
        ////        };
        ////        //set content header of reponse as file attached in reponse
        ////        //response.Content.Headers.ContentDisposition =
        ////        //new ContentDispositionHeaderValue("attachment")
        ////        //{
        ////        //    FileName = fileName
        ////        //};
        ////        ////set the content header content type as application/octet-stream as it returning file as reponse 
        ////        //response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        ////        memStreamDownloaded.Dispose();
        ////        return response;
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        memStreamDownloaded.Dispose();
        ////        return this.Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
        ////    }
        ////}

        //[HttpGet]//http get as it return file 
        //[Route("api/fileupload/downloadfilefrombox")]
        public async Task<IActionResult> DownloadFileFromBoxAPI(string ID)
        {

            MemoryStream memStreamDownloaded = new MemoryStream();
            HttpResponseMessage response = null;
            DocumentDataContract _docDC = new DocumentDataContract();
            try
            {

                _docDC.DocumentStorageID = ID;

                memStreamDownloaded = await new BoxHelper().DownloadFile(_docDC);

                MemoryStream memStream = new MemoryStream(memStreamDownloaded.ToArray());


                response = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StreamContent(memStream)
                };
                //set content header of reponse as file attached in reponse
                //response.Content.Headers.ContentDisposition =
                //new ContentDispositionHeaderValue("attachment")
                //{
                //    FileName = FileName
                //};
                //set the content header content type as application/octet-stream as it returning file as reponse 
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                //   return response;
                return Ok(response);
            }
            catch (Exception ex)
            {
                string msg = Logger.GetExceptionString(ex);
                msg = msg.Replace("'", "''");
                Logger.Write(ModuleName.Deal.ToString(), "Error occurred  generating wells file: " + msg, MessageLevel.Error, "00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000");

                memStreamDownloaded.Dispose();
                return Ok();
               // return this.Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }


        }


        [HttpGet]//http get as it return file 
        [Route("api/fileupload/downloadobjectfile")]
        public async Task<IActionResult> DownloadFileByStorageType(string ID, string StorageType)
        {

            IActionResult result = null;

            if (!string.IsNullOrEmpty(StorageType) && StorageType.ToLower() == "box" && !string.IsNullOrEmpty(ID))
            {
                result = await DownloadFileFromBoxAPI(ID);
            }
            else if (!string.IsNullOrEmpty(StorageType) && StorageType.ToLower() == "azureblob" && !string.IsNullOrEmpty(ID))
            {
                result = await DownloadFileFromAzureBlob(ID);
            }
            else if (!string.IsNullOrEmpty(StorageType) && StorageType.ToLower() == "localstorage" && !string.IsNullOrEmpty(ID))
            {
                ////Set the File Path.
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/ExcelTemplate/" + ID);
                
                result = await DownloadFileFromLocalStorage(filePath, ID);
            }
            return result;

        }


        ////[HttpGet]//http get as it return file 
        ////[Route("api/fileupload/syncboxdocument")]
        ////public async Task<HttpResponseMessage> SyncBoxDocument(string ParentID,string ID)
        ////{
        ////    try
        ////    {
        ////        IEnumerable<string> headerValues;

        ////        var headerUserID = new Guid();

        ////        if (Request.Headers.TryGetValues("TokenUId", out headerValues))
        ////        {
        ////            headerUserID = new Guid(headerValues.FirstOrDefault());
        ////        }

        ////        var folderid = await new BoxHelper().CheckAndCreateBoxFolder(ParentID, ID);

        ////        //ID - CREDealID
        ////        var result = await new BoxHelper().GetDocumentFromBox("", folderid.ToString());
        ////        List<DocumentDataContract> _docDC = new List<DocumentDataContract>();

        ////        foreach (Box.V2.Models.BoxItem itm in result.Entries)
        ////        {

        ////            _docDC.Add(new DocumentDataContract()
        ////            {
        ////                FileName = itm.Name,
        ////                DocumentStorageID =itm.Id,
        ////                CreatedBy = Convert.ToString(itm.OwnedBy.Name),
        ////                CreatedDate = itm.CreatedAt,

        ////            });
        ////        }

        ////        if (result!=null && result.Entries.Count>0)
        ////            _documentLogic.SyncBoxDocument(_docDC, headerUserID, ID);


        ////        return this.Request.CreateResponse(HttpStatusCode.OK, "Success");
        ////    }

        ////    catch (Exception ex)
        ////    {
        ////        return this.Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
        ////    }
        ////}

        [HttpGet]//http get as it return file 
        [Route("api/fileupload/downloadfilefromwells")]
        public async Task<IActionResult> DownloadFileFromWellsAPI(string ID)
        {
            NoteController _objNoteController = new Controllers.NoteController(_iEmailNotification, _env);
            DataSet ds = _objNoteController.ImportWellsDataByDealID(ID);
            MemoryStream memStreamDownloaded = GenerateExcel(ds, "Master", "Investor", "Property", "Tax", "Insurance", "Reserve", "ARM");
            //DocumentDataContract _docDC = new DocumentDataContract();
            try
            {
                MemoryStream memStream = new MemoryStream(memStreamDownloaded.ToArray());
                return File(memStream, "application/octet-stream");
            }
            catch (Exception ex)
            {
                string msg = Logger.GetExceptionString(ex);
                msg = msg.Replace("'", "''");
                Logger.Write(ModuleName.WellsExtract.ToString(), "Error occurred  generating wells file: Deal ID " + msg, MessageLevel.Error, "00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000");

                memStreamDownloaded.Dispose();
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }

        private MemoryStream GenerateExcel(DataSet dataToExcel, params string[] tabName)
        {
            string fileName = "M61_Wells_Export";// + string.Format("{0:ddmmyyyhhmmss}", System.DateTime.Now);
            string currentDirectorypath = Path.Combine(Directory.GetCurrentDirectory(), "ExcelTemplate");
            string finalFileNameWithPath = string.Empty;
            string cellLocation = "";
            try
            {
                //fileName = string.Format("{0}_{1}", fileName, DateTime.Now.ToString("dd-MM-yyyy"));
                finalFileNameWithPath = string.Format("{0}\\{1}.xlsx", currentDirectorypath, fileName);

                //Delete existing file with same file name.
                //if (File.Exists(finalFileNameWithPath))
                //File.Delete(finalFileNameWithPath);

                var newFile = new FileInfo(finalFileNameWithPath);

                //Step 1 : Create object of ExcelPackage class and pass file path to constructor.
                using (var package = new OfficeOpenXml.ExcelPackage(newFile))
                {
                    //Step 2 : Add a new worksheet to ExcelPackage object and give a suitable name
                    foreach (string excelSheetName in tabName)
                    {
                        if (dataToExcel.Tables[excelSheetName].Rows.Count > 0)
                        {
                            OfficeOpenXml.ExcelWorksheet worksheet = package.Workbook.Worksheets[excelSheetName];
                            if (excelSheetName == "ARM")
                                cellLocation = "A9";
                            else if (excelSheetName == "Property" || excelSheetName == "Reserve")
                                cellLocation = "A8";
                            else
                                cellLocation = "A7";
                            worksheet.Cells[cellLocation].LoadFromDataTable(dataToExcel.Tables[excelSheetName], false, OfficeOpenXml.Table.TableStyles.None);
                        }
                    }

                    Byte[] fileBytes = package.GetAsByteArray();
                    MemoryStream ms = new MemoryStream(fileBytes);
                    return ms;
                }
            }
            catch (Exception ex)
            {
                string msg = Logger.GetExceptionString(ex);
                msg = msg.Replace("'", "''");
                Logger.Write(ModuleName.WellsExtract.ToString(), "Error occurred  generating wells file in GenerateExcel" + msg, MessageLevel.Error, "00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000");
                throw ex;
            }
        }

        [HttpGet]
        [Route("api/fileupload/importdbtoazureblob")]
        public IActionResult ImportDBToAzureBlob([FromBody] PeriodicDataContract _periodicDC)
        {
            DateTime closeDate = Convert.ToDateTime(_periodicDC.EndDate);
            HttpResponseMessage response = new HttpResponseMessage();
            AzureStorageImportExportDatabases _importDatabases = new AzureStorageImportExportDatabases();
            string result = _importDatabases.BackupDatabaseToBlob(_periodicDC);

            if (result == "Success")
            {

                return Ok("Database imported successfully");
            }
            else
            {
                return Ok("Import failed.Please try after some time");
            }
        }


        [HttpGet]//http get as it return file 
        [Route("api/fileupload/downloadfilefromurl")]
        public async Task<IActionResult> DownloadFileFromURL(string ID)
        {
            NoteController _objNoteController = new Controllers.NoteController(_iEmailNotification, _env);
            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }


            byte[] myDataBuffer = new WebClient().DownloadData(ID);
            MemoryStream storeStream = new MemoryStream();
            storeStream.SetLength(myDataBuffer.Length);
            storeStream.Write(myDataBuffer, 0, (int)storeStream.Length);
            storeStream.Flush();
            //DocumentDataContract _docDC = new DocumentDataContract();
            try
            {

                //_docDC.DocumentStorageID = ID;

                //memStreamDownloaded = await new BoxHelper().DownloadFile(_docDC);

                MemoryStream memStream = new MemoryStream(storeStream.ToArray());

                return File(memStream, "application/octet-stream");
            }
            catch (Exception ex)
            {
                storeStream.Dispose();
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }

        ////[HttpPost]//http get as it return file 
        ////[Route("api/fileupload/downloadfilecalcoutput")]
        ////public async Task<HttpResponseMessage> DownloadFileCalcOutput([FromBody] CalculationManagerDataContract _calcMgrData)
        ////{
        ////    CalculationManagerLogic calcMgr = new CalculationManagerLogic();
        ////    CalculatorOutputJsonInfoDataContract _objCalcOutput = new CalculatorOutputJsonInfoDataContract();

        ////    //create DatSet from JSON 
        ////    DataSet ds = new DataSet();

        ////    _objCalcOutput = calcMgr.GetCalculatorOutputJsonInfo(_calcMgrData.CalculationRequestID,new Guid(_calcMgrData.NoteId), _calcMgrData.AnalysisID, new Guid(_calcMgrData.UserName));
        ////    string JSON_Filename = _objCalcOutput.FileName;

        ////    CalculatorDebugData _calculatorDebugData = new CalculatorDebugData();
        ////    _calculatorDebugData = GetJsonFile(JSON_Filename);

        ////    ds = getdataset(_calculatorDebugData);


        ////    MemoryStream memStreamDownloaded = GenerateCalcOutPutExcel(ds, "Output_Periodic", "Dates", "Rates", "Balance", "Fees", "FeeOutput", "Coupon", "PIK_Interest", "GAAP_Basis", "Financing");
        ////    HttpResponseMessage response = null;
        ////    try
        ////    {                
        ////        MemoryStream memStream = new MemoryStream(memStreamDownloaded.ToArray());
        ////        response = new HttpResponseMessage
        ////        {
        ////            StatusCode = HttpStatusCode.OK,
        ////            Content = new StreamContent(memStream)
        ////        };

        ////        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        ////        return response;
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        string msg = Logger.GetExceptionString(ex);
        ////        msg = msg.Replace("'", "''");
        ////        Logger.Write(ModuleName.WellsExtract.ToString(), "Error occurred  generating Calc output file: Note ID " + msg, MessageLevel.Error, "00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000");

        ////        memStreamDownloaded.Dispose();
        ////        return this.Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
        ////    }
        ////}


        ////public DataSet getdataset(CalculatorDebugData _calculatorDebugData)
        ////{
        ////    DataSet ds = new DataSet();

        ////    DataTable dtNotePeriodicOutputsDataContract = new DataTable();
        ////    DataTable dtBalanceTab = new DataTable();
        ////    DataTable dtCouponTab = new DataTable();
        ////    DataTable dtPIKInterestTab = new DataTable();
        ////    DataTable dtFinancingTab = new DataTable();
        ////    DataTable dtRateTab = new DataTable();
        ////    DataTable dtFeesTab = new DataTable();
        ////    DataTable dtDatesTab = new DataTable();
        ////    DataTable dtGAAPBasisTab = new DataTable();
        ////    DataTable dtFeeOutputDataContract = new DataTable();

        ////    dtNotePeriodicOutputsDataContract = ObjToDataTable.ToDataTable(_calculatorDebugData.ListNotePeriodicOutput);
        ////    dtDatesTab = ObjToDataTable.ToDataTable(_calculatorDebugData.ListDatesTab);
        ////    dtRateTab = ObjToDataTable.ToDataTable(_calculatorDebugData.ListRateTab);
        ////    dtBalanceTab = ObjToDataTable.ToDataTable(_calculatorDebugData.ListBalanceTab);
        ////    dtFeesTab = ObjToDataTable.ToDataTable(_calculatorDebugData.ListFeesTab);
        ////    dtFeeOutputDataContract = ObjToDataTable.ToDataTable(_calculatorDebugData.ListFeeOutput);
        ////    dtCouponTab = ObjToDataTable.ToDataTable(_calculatorDebugData.ListCouponTab);
        ////    dtPIKInterestTab = ObjToDataTable.ToDataTable(_calculatorDebugData.ListPIKInterestTab);
        ////    dtGAAPBasisTab = ObjToDataTable.ToDataTable(_calculatorDebugData.ListGAAPBasisTab);
        ////    dtFinancingTab = ObjToDataTable.ToDataTable(_calculatorDebugData.ListFinancingTab);

        ////    ds.Tables.Add(dtNotePeriodicOutputsDataContract);
        ////    ds.Tables[ds.Tables.Count - 1].TableName = "Output_Periodic";

        ////    ds.Tables.Add(dtDatesTab);
        ////    ds.Tables[ds.Tables.Count - 1].TableName = "Dates";

        ////    //Rates
        ////    ds.Tables.Add(dtRateTab);
        ////    ds.Tables[ds.Tables.Count - 1].TableName = "Rates";
        ////    //Balance
        ////    ds.Tables.Add(dtBalanceTab);
        ////    ds.Tables[ds.Tables.Count - 1].TableName = "Balance";
        ////    //Fees
        ////    ds.Tables.Add(dtFeesTab);
        ////    ds.Tables[ds.Tables.Count - 1].TableName = "Fees";
        ////    //FeeOutput
        ////    ds.Tables.Add(dtFeeOutputDataContract);
        ////    ds.Tables[ds.Tables.Count - 1].TableName = "FeeOutput";
        ////    //Coupon
        ////    ds.Tables.Add(dtCouponTab);
        ////    ds.Tables[ds.Tables.Count - 1].TableName = "Coupon";
        ////    //PIK_Interest
        ////    ds.Tables.Add(dtPIKInterestTab);
        ////    ds.Tables[ds.Tables.Count - 1].TableName = "PIK_Interest";
        ////    //GAAP_Basis
        ////    ds.Tables.Add(dtGAAPBasisTab);
        ////    ds.Tables[ds.Tables.Count - 1].TableName = "GAAP_Basis";
        ////    //Financing
        ////    ds.Tables.Add(dtFinancingTab);
        ////    ds.Tables[ds.Tables.Count - 1].TableName = "Financing";

        ////    return ds;
        ////}

        ////public CalculatorDebugData GetJsonFile(string FileName)
        ////{
        ////    string json = "";
        ////    //string fileext = FileName.Split('.')[1];

        ////    CalculatorDebugData JFile = new CalculatorDebugData();

        ////    string filepath = @"C:\Users\vishal.balapure\Desktop\JSON\resultjson.json";//AppDomain.CurrentDomain.BaseDirectory + @"\JSONFile\" + ServicerName + "_" + fileext.ToUpper() + ".json";
        ////    try
        ////    {
        ////        using (StreamReader r = new StreamReader(filepath))
        ////        {
        ////            json = r.ReadToEnd();
        ////            JFile = JsonConvert.DeserializeObject<CalculatorDebugData>(json);
        ////        }
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        string exc = ex.ToString();
        ////    }
        ////    return JFile;

        ////}



        ////private MemoryStream GenerateCalcOutPutExcel(DataSet dataToExcel, params string[] tabName)
        ////{
        ////    string fileName = "M61_Note_OutPut";// + string.Format("{0:ddmmyyyhhmmss}", System.DateTime.Now);
        ////    string currentDirectorypath = HostingEnvironment.MapPath("~/ExcelTemplate/");
        ////    string finalFileNameWithPath = string.Empty;
        ////    string cellLocation = "";
        ////    try
        ////    {
        ////        //fileName = string.Format("{0}_{1}", fileName, DateTime.Now.ToString("dd-MM-yyyy"));
        ////        finalFileNameWithPath = string.Format("{0}\\{1}.xlsx", currentDirectorypath, fileName);

        ////        //Delete existing file with same file name.
        ////        //if (File.Exists(finalFileNameWithPath))
        ////        //File.Delete(finalFileNameWithPath);

        ////        var newFile = new FileInfo(finalFileNameWithPath);

        ////        //Step 1 : Create object of ExcelPackage class and pass file path to constructor.
        ////        using (var package = new OfficeOpenXml.ExcelPackage(newFile))
        ////        {
        ////            //Step 2 : Add a new worksheet to ExcelPackage object and give a suitable name
        ////            foreach (string excelSheetName in tabName)
        ////            {
        ////                if (dataToExcel.Tables[excelSheetName].Rows.Count > 0)
        ////                {
        ////                    OfficeOpenXml.ExcelWorksheet worksheet = package.Workbook.Worksheets[excelSheetName];
        ////                    worksheet.Cells[cellLocation].LoadFromDataTable(dataToExcel.Tables[excelSheetName], false, OfficeOpenXml.Table.TableStyles.None);
        ////                }
        ////            }

        ////            Byte[] fileBytes = package.GetAsByteArray();
        ////            MemoryStream ms = new MemoryStream(fileBytes);
        ////            return ms;
        ////        }
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        string msg = Logger.GetExceptionString(ex);
        ////        msg = msg.Replace("'", "''");
        ////        Logger.Write(ModuleName.WellsExtract.ToString(), "Error occurred  generating Note debug output file in GenerateExcel" + msg, MessageLevel.Error, "00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000");
        ////        throw ex;
        ////    }
        ////}

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

        [HttpPost]
        [Route("api/fileupload/uploadobjectfile")]
        public async Task<IActionResult> UploadFilesByStorageType()
        {
            IActionResult result = null;
            string res = string.Empty;

            if (HttpContext.Request.Query["StorageType"] != "" && HttpContext.Request.Query["StorageType"].ToString().ToLower() == "box")
            {
                //result = await UploadFileToBox();
            }
            else if (HttpContext.Request.Query["StorageType"] != "" && HttpContext.Request.Query["StorageType"].ToString().ToLower() == "azureblob")
            {
                result = await UploadObjectFileToAzureBlob();
            }

            if (HttpContext.Request.Query["ObjectTypeID"] != "" && HttpContext.Request.Query["ObjectTypeID"].ToString().ToLower() == "588") // dump data in DB in case of wells daily extract
            {

                GetConfigSetting();
                var connectionString = Sectionroot.GetSection("storage:container:connectionstring").Value;
                var sourceContainerName = Sectionroot.GetSection("storage:container:name").Value;
                
                AzureStorageReadFile azureStorageReadFile = new AzureStorageReadFile();
                DataSet dt = AzureStorageReadFile.GetExcelBlobData(AzureFileName, connectionString, sourceContainerName);
                string UploadType = HttpContext.Request.Query["UploadType"];
                if (UploadType == "wellsupload")
                {
                    if (dt.Tables["tblACOREDailyExtract"].Rows.Count > 0)
                    {
                        _documentLogic.WellsDailyExtractBulkInsert(dt.Tables["tblACOREDailyExtract"], "DW.ServicingTransactionBI");
                    }
                }
                else
                {
                    JsonFileConfiguration fileConf = azureStorageReadFile.GetJsonFile(AzureFileName, UploadType);
                    DataTable dtExcel = azureStorageReadFile.GetFileDataNoteMatrix(AzureFileName, AzureFileName, fileConf);
                    if (dtExcel.Rows.Count > 0)
                    {
                        res = _documentLogic.BulkInsertForNoteMatrix(dtExcel, fileConf);
                        if (res == "Success")
                        {
                            //var reslt = _documentLogic.InsertIntoNoteMatrix();
                            //if (reslt == -1)
                            return Ok($"Success==File has successfully uploaded.");
                            //else
                            //    return Ok($"Failed==Error Occured while upload.");
                        }
                        else
                        {
                            return Ok($"Failed==Error Occured while upload.");
                        }
                    }
                    else
                    {
                        return Ok($"Failed==File is not in correct format.");
                    }

                }
            }

            return result;
        }

    }
}
