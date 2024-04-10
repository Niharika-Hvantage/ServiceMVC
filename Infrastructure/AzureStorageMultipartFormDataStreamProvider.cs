using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.Extensions.Configuration;

namespace CRES.Services
{
    public class AzureStorageMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
    {
        private readonly CloudBlobContainer _blobContainer;
        private readonly string[] _supportedMimeTypes = { "image/png", "image/jpeg", "image/jpg", "csv/csv", "xlsx/xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.wordprocessingml.document","application/pdf", "application/msword" };


        public AzureStorageMultipartFormDataStreamProvider(CloudBlobContainer blobContainer) : base("azure")
        {
            _blobContainer = blobContainer;
        }

        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (headers == null) throw new ArgumentNullException(nameof(headers));

            if (!_supportedMimeTypes.Contains(headers.ContentType.ToString().ToLower()))
            {
                throw new NotSupportedException("Only jpeg,png,csv,xlsx,pdf,doc are supported");
            }

            //get file extension
            var fileName = headers.ContentDisposition.FileName.Replace("\"", "").ToLower();
            int index = fileName.LastIndexOf('.');
            //string onyName = fileName.Substring(0, index);
            string fileExtension = "." + fileName.Substring(index + 1);

            // Generate a new filename for every new blob
            fileName = headers.ContentDisposition.FileName.Replace("\"", "").ToLower().Replace(fileExtension, "_" + System.DateTime.Now.ToString("yyyy-MM-dd-mm-ss")+ fileExtension);
            //string strpath = System.IO.Path.GetExtension(headers.ContentDisposition.FileName);
            

            CloudBlockBlob blob = _blobContainer.GetBlockBlobReference(fileName);

            if (headers.ContentType != null)
            {
                // Set appropriate content type for your uploaded file
                blob.Properties.ContentType = headers.ContentType.MediaType;
            }

            this.FileData.Add(new MultipartFileData(headers, blob.Name));

            return blob.OpenWrite();
        }      

    }
}

internal class BlobUtilities
{
    public static CloudBlobClient GetBlobClient
    {
        get
        {

            IConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
            var root = builder.Build();
            var accountName = root.GetSection("Application").GetSection("storage:account:name").Value;
            var accountKey = root.GetSection("Application").GetSection("storage:account:key").Value;

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName="+accountName+";"
             + "AccountKey="+ accountKey + "");
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            return blobClient;
        }
    }
}