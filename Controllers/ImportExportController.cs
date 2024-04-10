
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using CRES.DataContract;
using CRES.BusinessLogic;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using CRES.BusinessLogic;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using System.Diagnostics;
using System.IO;

using iTextSharp.tool.xml;
using iTextSharp.text.html.simpleparser;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using iTextSharp.tool.xml.pipeline.html;
using iTextSharp.tool.xml.html;
using Microsoft.AspNetCore.Html;
using CRES.Utilities;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using CRES.Services.Infrastructure;
using System.Threading.Tasks;


namespace CRES.Services.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class ImportExportController : ControllerBase
    {

        //string BackshopConnString = ConfigurationManager.ConnectionStrings["BackshopConnString"].ConnectionString;
        [HttpPost]
        [Services.Controllers.DeflateCompression]
        [Route("api/ImportExportController/ImportBackShopInUnderwritingtable")]
        public IActionResult ImportBackShopInUnderwritingtable(BackShopImportDataContract backShopImportDataContract)
        {
            GenericResult _authenticationResult = null;

            bool result = false;

            //INSERT IN BATCH LOG 
            Guid? BatchLogID = InsertBatchLog(250, backShopImportDataContract.UserName);


            BackShopImportDataContract _backShopImportDataContract = new BackShopImportDataContract();
            _backShopImportDataContract = GetBackshopDealByDealIdOdDealname(backShopImportDataContract);
            _backShopImportDataContract.BatchLogID = BatchLogID;

            if (_backShopImportDataContract.DealID != null)
            {

                ImportExportLogic _importExportLogic = new ImportExportLogic();
                _importExportLogic.ImportBackShopInUnderwritingtable(_backShopImportDataContract.DealID, backShopImportDataContract.UserName, BatchLogID);

                result = true;

            }
            else
            {
                result = false;
            }

            try
            {
                if (result)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        //Message = "Password Changed successfully",
                        //   Token = Encryptor.MD5Hash(_userDataContract.Login + EncruptOldPassword),
                        //Token = Encryptor.MD5Hash(headerUserID + EncruptNewPassword),
                        BackShopImportDataContract = _backShopImportDataContract
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Deal Id " + backShopImportDataContract.DealName + " couldn't be found.",
                        // Token = Encryptor.MD5Hash(_userDataContract.Login + EncruptOldPassword)
                        //Token = Encryptor.MD5Hash(headerUserID + EncruptOldPassword)
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

            //var javaScriptSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            //string jsonString = javaScriptSerializer.Serialize(_authenticationResult);

            //var json = new JavaScriptSerializer().Serialize(_authenticationResult);
            ////JavaScriptSerializer js = new JavaScriptSerializer();
            ////string returnstring = js.Serialize(_authenticationResult);

            //Request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            return Ok(_authenticationResult);
        }

        [HttpPost]
        [Services.Controllers.DeflateCompression]
        [Route("api/ImportExportController/ImportLandingtableToMainDB")]
        public IActionResult ImportLandingtableToMainDB(BackShopImportDataContract backShopImportDataContract)
        {
            GenericResult _authenticationResult = null;
            bool result = false;

            if (backShopImportDataContract.DealID != null)
            {

                ImportExportLogic _importExportLogic = new ImportExportLogic();
                _importExportLogic.ImportLandingtableToMainDB(backShopImportDataContract.DealID, backShopImportDataContract.UserName, backShopImportDataContract.BatchLogID);

                result = true;

            }
            else
            {
                result = false;
            }

            try
            {
                if (result)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        //Message = "Password Changed successfully",
                        //Token = Encryptor.MD5Hash(_userDataContract.Login + EncruptOldPassword),
                        //Token = Encryptor.MD5Hash(headerUserID + EncruptNewPassword),

                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        //Message = "Authentication failed",
                        // Token = Encryptor.MD5Hash(_userDataContract.Login + EncruptOldPassword)
                        //Token = Encryptor.MD5Hash(headerUserID + EncruptOldPassword)
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
        [Route("api/ImportExportController/DeleteINUnderwritingDealDataByDealID")]
        public IActionResult DeleteINUnderwritingDealDataByDealID([FromBody] BackShopImportDataContract backShopImportDataContract)
        {
            GenericResult _authenticationResult = null;
            bool result = false;

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            if (backShopImportDataContract.DealID != null)
            {

                ImportExportLogic _importExportLogic = new ImportExportLogic();

                _importExportLogic.DeleteBatchLogByBatchLogID(backShopImportDataContract.BatchLogID);

                _importExportLogic.DeleteINUnderwritingDealDataByDealID(backShopImportDataContract.DealID, headerUserID);

                result = true;

            }
            else
            {
                result = false;
            }

            try
            {
                if (result)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        //Message = "Password Changed successfully",
                        //Token = Encryptor.MD5Hash(_userDataContract.Login + EncruptOldPassword),
                        //Token = Encryptor.MD5Hash(headerUserID + EncruptNewPassword),

                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        //Message = "Authentication failed",
                        // Token = Encryptor.MD5Hash(_userDataContract.Login + EncruptOldPassword)
                        //Token = Encryptor.MD5Hash(headerUserID + EncruptOldPassword)
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

        [Services.Controllers.DeflateCompression]
        public BackShopImportDataContract GetBackshopDealByDealIdOdDealname(BackShopImportDataContract backShopImportDataContract)
        {

            BackShopImportDataContract _backShopImportDataContract = new BackShopImportDataContract();
            ImportExportLogic _importExportLogic = new ImportExportLogic();
            _backShopImportDataContract = _importExportLogic.GetBackshopDealByDealName(backShopImportDataContract.DealName);


            //bool result = false;
            //if(_backShopImportDataContract.DealID != null)
            //{
            //    result = true;
            //}

            return _backShopImportDataContract;

        }


        [HttpPost]
        [Services.Controllers.DeflateCompression]
        [Route("api/ImportExportController/GetInUnderwritingNotesByDealID")]
        public IActionResult GetInUnderwritingNotesByDealID([FromBody] BackShopImportDataContract backShopImportDataContract)
        {
            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            List<IN_UnderwritingNoteDataContract> _iN_UnderwritingNoteDataContractList = new List<IN_UnderwritingNoteDataContract>();
            ImportExportLogic _importExportLogic = new ImportExportLogic();
            _iN_UnderwritingNoteDataContractList = _importExportLogic.GetInUnderwritingNotesByDealID(backShopImportDataContract.DealID, headerUserID);


            try
            {
                if (_iN_UnderwritingNoteDataContractList.Count > 0)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        //Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(_iN_UnderwritingNoteDataContractList.Count),
                        lstIN_UnderwritingNotes = _iN_UnderwritingNoteDataContractList
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false
                        //Message = "Authentication failed",
                        // Token = Encryptor.MD5Hash(_userDataContract.Login + EncruptOldPassword)
                        //Token = Encryptor.MD5Hash(headerUserID + EncruptOldPassword)
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
        [Route("api/ImportExportController/GetINUnderwritingRateSpreadScheduleByNoteID")]
        public IActionResult GetINUnderwritingRateSpreadScheduleByNoteID([FromBody] IN_UnderwritingNoteDataContract in_underwritingnotedatacontract)
        {
            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            List<IN_UnderwritingRateSpreadScheduleDataContract> _in_UnderwritingRateSpreadScheduleDataContractList = new List<IN_UnderwritingRateSpreadScheduleDataContract>();
            ImportExportLogic _importExportLogic = new ImportExportLogic();
            _in_UnderwritingRateSpreadScheduleDataContractList = _importExportLogic.GetINUnderwritingRateSpreadScheduleByNoteID(in_underwritingnotedatacontract.IN_UnderwritingNoteID, headerUserID);


            try
            {
                if (_in_UnderwritingRateSpreadScheduleDataContractList.Count > 0)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        //Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(_in_UnderwritingRateSpreadScheduleDataContractList.Count),
                        lstIN_UnderwritingRateSpreadScheduleDataContractList = _in_UnderwritingRateSpreadScheduleDataContractList
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false
                        //Message = "Authentication failed",
                        // Token = Encryptor.MD5Hash(_userDataContract.Login + EncruptOldPassword)
                        //Token = Encryptor.MD5Hash(headerUserID + EncruptOldPassword)
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
        [Route("api/ImportExportController/GetINUnderwritingStrippingScheduleByNoteID")]
        public IActionResult GetINUnderwritingStrippingScheduleByNoteID([FromBody] IN_UnderwritingNoteDataContract in_underwritingnotedatacontract)
        {
            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            List<IN_UnderwritingStrippingScheduleDataContract> _in_UnderwritingStrippingScheduleDataContractList = new List<IN_UnderwritingStrippingScheduleDataContract>();
            ImportExportLogic _importExportLogic = new ImportExportLogic();
            _in_UnderwritingStrippingScheduleDataContractList = _importExportLogic.GetINUnderwritingStrippingScheduleByNoteID(in_underwritingnotedatacontract.IN_UnderwritingNoteID, headerUserID);


            try
            {
                if (_in_UnderwritingStrippingScheduleDataContractList.Count > 0)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        //Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(_in_UnderwritingStrippingScheduleDataContractList.Count),
                        lstIN_UnderwritingStrippingScheduleDataContractList = _in_UnderwritingStrippingScheduleDataContractList
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false
                        //Message = "Authentication failed",
                        // Token = Encryptor.MD5Hash(_userDataContract.Login + EncruptOldPassword)
                        //Token = Encryptor.MD5Hash(headerUserID + EncruptOldPassword)
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
        [Route("api/ImportExportController/GetINUnderwritingPIKScheduleByNoteID")]
        public IActionResult GetINUnderwritingPIKScheduleByNoteID([FromBody] IN_UnderwritingNoteDataContract in_underwritingnotedatacontract)
        {
            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            List<IN_UnderwritingPIKScheduleDataContract> _in_UnderwritingPIKScheduleDataContractList = new List<IN_UnderwritingPIKScheduleDataContract>();
            ImportExportLogic _importExportLogic = new ImportExportLogic();
            _in_UnderwritingPIKScheduleDataContractList = _importExportLogic.GetINUnderwritingPIKScheduleByNoteID(in_underwritingnotedatacontract.IN_UnderwritingNoteID, headerUserID);


            try
            {
                if (_in_UnderwritingPIKScheduleDataContractList.Count > 0)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        //Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(_in_UnderwritingPIKScheduleDataContractList.Count),
                        lstIN_UnderwritingPIKScheduleDataContractList = _in_UnderwritingPIKScheduleDataContractList
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false
                        //Message = "Authentication failed",
                        // Token = Encryptor.MD5Hash(_userDataContract.Login + EncruptOldPassword)
                        //Token = Encryptor.MD5Hash(headerUserID + EncruptOldPassword)
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
        [Route("api/ImportExportController/GetINUnderwritingFundingScheduleByNoteID")]
        public IActionResult GetINUnderwritingFundingScheduleByNoteID([FromBody] IN_UnderwritingNoteDataContract in_underwritingnotedatacontract)
        {
            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            List<IN_UnderwritingFundingScheduleDataContract> _in_UnderwritingFundingScheduleDataContractList = new List<IN_UnderwritingFundingScheduleDataContract>();
            ImportExportLogic _importExportLogic = new ImportExportLogic();
            _in_UnderwritingFundingScheduleDataContractList = _importExportLogic.GetINUnderwritingFundingScheduleByNoteID(in_underwritingnotedatacontract.IN_UnderwritingNoteID, headerUserID);


            try
            {
                if (_in_UnderwritingFundingScheduleDataContractList.Count > 0)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        //Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(_in_UnderwritingFundingScheduleDataContractList.Count),
                        lstIN_UnderwritingFundingScheduleDataContractList = _in_UnderwritingFundingScheduleDataContractList
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false
                        //Message = "Authentication failed",
                        // Token = Encryptor.MD5Hash(_userDataContract.Login + EncruptOldPassword)
                        //Token = Encryptor.MD5Hash(headerUserID + EncruptOldPassword)
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
        [Route("api/ImportExportController/getINUnderwritingDealByDealIdorDealName")]
        public IActionResult GetINUnderwritingDealByDealIdorDealName([FromBody] BackShopImportDataContract backShopImportDataContract)
        {
            GenericResult _authenticationResult = null;
            bool result = false;

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            IN_UnderwritingDealDataContract _iN_UnderwritingDealDataContract = new IN_UnderwritingDealDataContract();

            if (backShopImportDataContract != null)
            {

                ImportExportLogic _importExportLogic = new ImportExportLogic();

                _iN_UnderwritingDealDataContract = _importExportLogic.GetINUnderwritingDealByDealIdorDealName(backShopImportDataContract.DealName);
                if (_iN_UnderwritingDealDataContract.ClientDealID != null)
                {
                    result = true;
                }
                else
                {
                    result = false;
                }
            }
            else
            {
                result = false;
            }

            try
            {
                if (result)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Deal Id " + backShopImportDataContract.DealName + " already exist, do you want to override existing deal information?",
                        //Token = Encryptor.MD5Hash(_userDataContract.Login + EncruptOldPassword),
                        //Token = Encryptor.MD5Hash(headerUserID + EncruptNewPassword),

                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        //Message = "Authentication failed",
                        // Token = Encryptor.MD5Hash(_userDataContract.Login + EncruptOldPassword)
                        //Token = Encryptor.MD5Hash(headerUserID + EncruptOldPassword)
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
        [Route("api/ImportExportController/getINUnderwritingDealDataByDealId")]
        public IActionResult getINUnderwritingDealDataByDealId([FromBody] BackShopImportDataContract backShopImportDataContract)
        {
            GenericResult _authenticationResult = null;
            bool result = false;

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            IN_UnderwritingDealDataContract _iN_UnderwritingDealDataContract = new IN_UnderwritingDealDataContract();

            if (backShopImportDataContract != null)
            {

                ImportExportLogic _importExportLogic = new ImportExportLogic();

                _iN_UnderwritingDealDataContract = _importExportLogic.GetINUnderwritingDealByDealIdorDealName(backShopImportDataContract.DealID);
                if (_iN_UnderwritingDealDataContract.ClientDealID != null)
                {
                    result = true;
                }
                else
                {
                    result = false;
                }
            }
            else
            {
                result = false;
            }

            try
            {
                if (result)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        //Message = "Deal Id " + backShopImportDataContract.DealName + " already exist, do you want to override existing deal information?",
                        //Token = Encryptor.MD5Hash(_userDataContract.Login + EncruptOldPassword),
                        //Token = Encryptor.MD5Hash(headerUserID + EncruptNewPassword),
                        IN_UnderwritingDeal = _iN_UnderwritingDealDataContract

                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        //Message = "Authentication failed",
                        // Token = Encryptor.MD5Hash(_userDataContract.Login + EncruptOldPassword)
                        //Token = Encryptor.MD5Hash(headerUserID + EncruptOldPassword)
                        IN_UnderwritingDeal = null
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


        public Guid? InsertBatchLog(int batchlogid, string username)
        {

            Guid? ret_val = new Guid();

            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }


            BatchLogDataContract _batchLogDataContract = new BatchLogDataContract();
            _batchLogDataContract.BatchTypeID = batchlogid;
            _batchLogDataContract.StartedByUserID = headerUserID;


            ImportExportLogic _importExportLogic = new ImportExportLogic();
            Guid? var = _importExportLogic.InsertBatchLog(_batchLogDataContract, username);


            try
            {
                if (var != null)
                {
                    ret_val = var;
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

            return ret_val;
        }
        IConfigurationSection Sectionroot = null;

        [HttpGet]
        [Services.Controllers.DeflateCompression]
        [Route("api/ImportExportController/ConvertToPDF")]
        public async Task<IActionResult> ConvertToPDF()
        {

            GenericResult _authenticationResult = null;
            string fileName = "DrawFee";
            string filepath = Directory.GetCurrentDirectory() + @"\wwwroot\InvoiceTemplate\" + fileName + ".html";
            string storagetype = "Box";

            if (storagetype == "Blob")
            {

                using (MemoryStream stream = new System.IO.MemoryStream())
                {
                    //Create document
                    Document doc = new Document();
                    string FirstName = "pushp singh";
                    string sr = string.Empty;
                    using (StreamReader reader = new StreamReader(filepath))
                    {
                        sr = reader.ReadToEnd();
                    }

                    if (sr.Length > 0)
                    {
                        sr = sr.Replace("{firstname}", FirstName);
                    }

                    StringReader htmlContent = new StringReader(sr);
                    //Without saving local pdf
                    GetConfigSetting();
                    var Container = Sectionroot.GetSection("storage:container:name").Value;
                    CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);
                    CloudBlobDirectory blobDirectory = container.GetDirectoryReference("Invoice");
                    CloudBlockBlob blockBlob = blobDirectory.GetBlockBlobReference(fileName + ".pdf");


                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (var docm = new iTextSharp.text.Document())
                        {
                            PdfWriter writer = PdfWriter.GetInstance(docm, ms);
                            docm.Open();
                            XMLWorkerHelper.GetInstance().ParseXHtml(writer, docm, htmlContent);
                        }
                        var byteArray = ms.ToArray();

                        blockBlob.Properties.ContentType = "application/pdf";
                        blockBlob.UploadFromByteArray(byteArray, 0, byteArray.Length);
                    }
                }
            }
            if (storagetype == "Box")
            {

                using (MemoryStream stream = new System.IO.MemoryStream())
                {
                    //Create document
                    Document doc = new Document();
                    string FirstName = "pushp singh";
                    string sr = string.Empty;
                    using (StreamReader reader = new StreamReader(filepath))
                    {
                        sr = reader.ReadToEnd();
                    }

                    if (sr.Length > 0)
                    {
                        sr = sr.Replace("{firstname}", FirstName);
                    }

                    DocumentDataContract _docDC = new DocumentDataContract();
                    StringReader htmlContent = new StringReader(sr);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (var docm = new iTextSharp.text.Document())
                        {
                            PdfWriter writer = PdfWriter.GetInstance(docm, ms);
                            writer.CloseStream = false;
                            docm.Open();
                            XMLWorkerHelper.GetInstance().ParseXHtml(writer, docm, htmlContent);
                        }

                        _docDC.FileName = fileName + ".pdf";
                        _docDC.Storagetype = "Box";
                        string DocumentStorageID = await new BoxHelper().UploadFileToFolder("128220075132", _docDC, ms);
                    }


                }
            }

            _authenticationResult = new GenericResult()
            {
                Succeeded = true,
                Message = "Success==File has successfully uploaded."

            };
            return Ok(_authenticationResult);

        }




        [HttpGet]
        [Services.Controllers.DeflateCompression]
        [Route("api/ImportExportController/SendInvoiceEmail")]
        public async Task SendInvoiceEmailAsync(EmailDataContract emailDC)
        {
            string Location = "Invoice";
            string ID = "DrawFee.pdf";
            string storagetype = "Box";
            MemoryStream memStreamDownloaded = new MemoryStream();
            if (storagetype == "Blob")
            {
                GetConfigSetting();
                var Container = Sectionroot.GetSection("storage:container:name").Value;
                var accountName = Sectionroot.GetSection("storage:account:name").Value;
                var accountKey = Sectionroot.GetSection("storage:account:key").Value;
                var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer excelContainer = blobClient.GetContainerReference(Container);
                CloudBlobDirectory dr = excelContainer.GetDirectoryReference(Location);
                CloudBlockBlob cloudBlockBlob = dr.GetBlockBlobReference(ID);
                cloudBlockBlob.DownloadToStream(memStreamDownloaded);
                // MemoryStream memStream = new MemoryStream(memStreamDownloaded.ToArray());
            }
            else
            {
               
                DocumentDataContract _docDC = new DocumentDataContract();
                _docDC.FolderName = "Invoice";
                _docDC.FileName = "DrawFee.pdf";

                _docDC.DocumentStorageID = ID;
                memStreamDownloaded = await new BoxHelper().DownloadFile(_docDC);
                MemoryStream memStream = new MemoryStream(memStreamDownloaded.ToArray());

            }
            var objMailMessgae = new MailMessage();

            objMailMessgae.From = new MailAddress("no-reply@m61systems.com", "M61 Support");

            objMailMessgae.To.Add("pushpsing@gmail.com");

            objMailMessgae.Body = "Hello How are You";
            objMailMessgae.Subject = "Pushp";
            objMailMessgae.IsBodyHtml = true;
            memStreamDownloaded.Position = 0;
            objMailMessgae.Attachments.Add(new System.Net.Mail.Attachment(memStreamDownloaded, "Drawfee.pdf", "pdf/application"));
            EmailSettings emailsetting = new EmailSettings();
            emailsetting.Host = "smtp.gmail.com";
            emailsetting.UserName = "no-reply@m61systems.com";
            emailsetting.Password = "F1ght0n#";
            emailsetting.Port = "587";
            var smtp = new SmtpClient();
            smtp.Host = "smtp.gmail.com";
            smtp.EnableSsl = true;
            NetworkCredential NetworkCred = new NetworkCredential(emailsetting.UserName, emailsetting.Password);
            smtp.UseDefaultCredentials = true;
            smtp.Credentials = NetworkCred;
            smtp.Port = Convert.ToInt32(emailsetting.Port);

            ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            { return true; };
            smtp.Send(objMailMessgae);

        }


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







        #region Commented area
        //[Route("api/ImportExportController/GetBackShopData")]
        //public DataTable GetBackShopData()
        //{

        //    string connString = BackshopConnString;// @"your connection string here";
        //                                           //string query = "select NoteName, PaymentFreqCd, NoteId ,FundingDate ,FirstPIPaymentDate ,StatedMaturityDate ,OrigLoanAmount ,"+
        //                                           //    "OriginationFee ,AmortIOPeriod ,AmortizationTerm ,DeterminationDate,DeterminationMethodDay ,RoundingType ,RoundingDenominator "+
        //                                           //    " from tblControlMaster cm inner join tblNote n on n.ControlId_F = cm.ControlId left join tblNoteARM arm on n.NoteId = arm.NoteId_F inner join tblzCdPaymentFreq pf on n.PaymentFreqCd_F = pf.PaymentFreqCd where cm.dealname = 'Alden Park' ";

        //    string query = "select  " +
        //    "NoteName  as Name, " +
        //    "PaymentFreqDesc as PayFrequency, " +
        //    "NoteId  as ClientNoteID, " +
        //    "FundingDate  as ClosingDate, " +
        //    "FirstPIPaymentDate  as FirstPaymentDate, " +
        //    "StatedMaturityDate  as SelectedMaturityDateScenario, " +
        //    "OrigLoanAmount  as InitialFundingAmount, " +
        //    "OriginationFee  as OriginationFee, " +
        //    "AmortIOPeriod  as IOTerm, " +
        //    "AmortizationTerm  as AmortTerm, " +
        //    "DeterminationDate as DeterminationDateLeadDays, " +
        //    "DeterminationMethodDay  as DeterminationDateReferenceDayoftheMonth, " +
        //    "RoundingType  as RoundingMethod, " +
        //    "RoundingDenominator   as IndexRoundingRule " +

        //    "from tblControlMaster cm  " +
        //    "inner join tblNote n on n.ControlId_F = cm.ControlId " +
        //    "left join tblNoteARM arm on n.NoteId = arm.NoteId_F  " +
        //    "inner join tblzCdPaymentFreq pf on n.PaymentFreqCd_F = pf.PaymentFreqCd  " +
        //    "where cm.dealname = 'Alden Park' ";


        //    SqlConnection conn = new SqlConnection(connString);
        //    DataTable dataTable = new DataTable();
        //    try
        //    {
        //        SqlCommand cmd = new SqlCommand(query, conn);
        //        conn.Open();

        //        // create data adapter
        //        SqlDataAdapter da = new SqlDataAdapter(cmd);
        //        // this will query your database and return the result to your datatable
        //        da.Fill(dataTable);

        //        List<IN_UnderwritingNoteDataContract> lstlandingNote = new List<IN_UnderwritingNoteDataContract>();


        //        //lstlandingNote = (from DataRow dr in dataTable.Rows
        //        //                  select new IN_UnderwritingNoteDataContract()
        //        //                  {

        //        //                      Name = dataTable.Columns["Name"].ToString(),
        //        //                      PayFrequency = dr.Field<int?>("PayFrequency"),
        //        //                      ClientNoteID = dr.Field<string>("ClientNoteID"),
        //        //                      ClosingDate = dr.Field<DateTime?>("ClosingDate"),
        //        //                      FirstPaymentDate = dr.Field<DateTime?>("FirstPaymentDate"),
        //        //                      SelectedMaturityDateScenario = dr.Field<int?>("SelectedMaturityDateScenario"),
        //        //                      InitialFundingAmount = dr.Field<decimal?>("InitialFundingAmount"),
        //        //                      OriginationFee = dr.Field<decimal?>("OriginationFee"),
        //        //                      IOTerm = dr.Field<int?>("IOTerm"),
        //        //                      AmortTerm = dr.Field<int?>("AmortTerm"),
        //        //                      DeterminationDateLeadDays = dr.Field<int?>("DeterminationDateLeadDays"),
        //        //                      DeterminationDateReferenceDayoftheMonth = dr.Field<int?>("DeterminationDateReferenceDayoftheMonth"),
        //        //                      RoundingMethod = dr.Field<int?>("RoundingMethod"),
        //        //                      IndexRoundingRule = dr.Field<int?>("IndexRoundingRule")

        //        //                  }).ToList();

        //        for (int i = 0; i < dataTable.Rows.Count; i++)
        //        {
        //            IN_UnderwritingNoteDataContract landingNote = new IN_UnderwritingNoteDataContract();

        //            landingNote.Name = dataTable.Rows[i]["Name"].ToString();
        //            if (dataTable.Rows[i]["PayFrequency"].ToString() == "") landingNote.PayFrequency = null; else landingNote.PayFrequency = dataTable.Rows[i]["PayFrequency"].ToString();
        //            landingNote.ClientNoteID = dataTable.Rows[i]["ClientNoteID"].ToString();
        //            landingNote.ClosingDate = Convert.ToDateTime(dataTable.Rows[i]["ClosingDate"]);
        //            landingNote.FirstPaymentDate = Convert.ToDateTime(dataTable.Rows[i]["FirstPaymentDate"]);
        //            if (dataTable.Rows[i]["SelectedMaturityDateScenario"].ToString() == "") landingNote.SelectedMaturityDateScenario = null; else landingNote.SelectedMaturityDateScenario = Convert.ToDateTime(dataTable.Rows[i]["SelectedMaturityDateScenario"]);
        //            if (dataTable.Rows[i]["InitialFundingAmount"].ToString() == "") landingNote.InitialFundingAmount = null; else landingNote.InitialFundingAmount = Convert.ToDecimal(dataTable.Rows[i]["InitialFundingAmount"]);
        //            if (dataTable.Rows[i]["OriginationFee"].ToString() == "") landingNote.OriginationFee = null; else landingNote.OriginationFee = Convert.ToDecimal(dataTable.Rows[i]["OriginationFee"]);
        //            if (dataTable.Rows[i]["IOTerm"].ToString() == "") landingNote.IOTerm = null; else landingNote.IOTerm = Convert.ToInt32(dataTable.Rows[i]["IOTerm"]);
        //            if (dataTable.Rows[i]["AmortTerm"].ToString() == "") landingNote.AmortTerm = null; else landingNote.AmortTerm = Convert.ToInt32(dataTable.Rows[i]["AmortTerm"]);
        //            if (dataTable.Rows[i]["DeterminationDateLeadDays"].ToString() == "") landingNote.DeterminationDateLeadDays = null; else landingNote.DeterminationDateLeadDays = Convert.ToInt32(dataTable.Rows[i]["DeterminationDateLeadDays"]);
        //            if (dataTable.Rows[i]["DeterminationDateReferenceDayoftheMonth"].ToString() == "") landingNote.DeterminationDateReferenceDayoftheMonth = null; else landingNote.DeterminationDateReferenceDayoftheMonth = Convert.ToInt32(dataTable.Rows[i]["DeterminationDateReferenceDayoftheMonth"]);

        //            if (dataTable.Rows[i]["RoundingMethod"].ToString() == "") landingNote.RoundingMethod = null; else landingNote.RoundingMethod = dataTable.Rows[i]["RoundingMethod"].ToString();
        //            if (dataTable.Rows[i]["IndexRoundingRule"].ToString() == "") landingNote.IndexRoundingRule = null; else landingNote.IndexRoundingRule = Convert.ToInt32(dataTable.Rows[i]["IndexRoundingRule"]);

        //            lstlandingNote.Add(landingNote);
        //        }

        //        da.Dispose();
        //    }
        //    catch (Exception ex)
        //    {
        //        string a = ex.Message;
        //    }
        //    finally
        //    {
        //        conn.Close();
        //    }

        //    return dataTable;
        //}


        //public void InsertBackShopDataIntoLanding(DataTable dt)
        //{
        //    try
        //    {
        //        string connectionString = @"Data Source=.\SQLEXPRESS;AttachDbFilename=|DataDirectory|\Customers.mdf;Integrated Security=True;User Instance=True";
        //        using (SqlConnection connection = new SqlConnection(connectionString))
        //        using (SqlCommand command = connection.CreateCommand())
        //        {
        //            command.CommandText = "INSERT INTO [IO].[IN_UnderwritingNote]([Name],[PayFrequency],[ClientNoteID],[ClosingDate],[FirstPaymentDate],[SelectedMaturityDateScenario],[InitialFundingAmount],[OriginationFee],[IOTerm],[AmortTerm],[DeterminationDateLeadDays],[DeterminationDateReferenceDayoftheMonth],[RoundingMethod],[IndexRoundingRule],[CreatedBy],[CreatedDate],[UpdatedBy] ,[UpdatedDate]) " +
        //                " VALUES (@projectName, @biddingDueDate, @status, @projectStartDate, @projectStartDate, @assignedTo, @pointsWorth, @staffCredits)";

        //            connection.Open();
        //            command.ExecuteNonQuery();
        //        }
        //    }
        //    catch (SqlException ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }
        //}
        #endregion


    }
}
