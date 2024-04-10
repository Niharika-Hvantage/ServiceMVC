using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Configuration;

namespace CRES.Services.Infrastructure
{
    public class BoxStorageMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
    {
        private readonly string[] _supportedMimeTypes = { "image/png", "image/jpeg", "image/jpg", "csv/csv", "xlsx/xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/pdf", "application/msword" };


        public BoxStorageMultipartFormDataStreamProvider() : base("Box")
        {
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
            fileName = headers.ContentDisposition.FileName.Replace("\"", "").ToLower().Replace(fileExtension, "_" + System.DateTime.Now.ToString("yyyy-MM-dd-mm-ss") + fileExtension);
            //string strpath = System.IO.Path.GetExtension(headers.ContentDisposition.FileName);

            this.FileData.Add(new MultipartFileData(headers, fileName));
            return new MemoryStream();
        }
    }
}