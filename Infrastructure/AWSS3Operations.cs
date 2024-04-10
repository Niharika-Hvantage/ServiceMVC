using System;
using System.Collections.Generic;
using System.IO;
using Amazon;
using Amazon.S3;
using Amazon.S3.IO;
using Amazon.S3.Model;
using Amazon.Runtime;
using Amazon.CognitoIdentity;
using Amazon.EC2;
using Ionic.Zip;
using Newtonsoft.Json;
using Amazon.Lambda.Model;
using Amazon.Lambda;
using System.Threading.Tasks;

namespace CRES.Services.Infrastructure
{
    public class AWSS3Operations
    {
        //private readonly AmazonS3Client _s3Client = new AmazonS3Client(AWSConfig.GetCredentials(), RegionEndpoint.USEast1);

        private const string _bucketName = "script-engine";

        private IAmazonS3 GetAmazonS3Client()
        {
            return new AmazonS3Client(AWSConfig.GetCredentials(), AWSConfig.GetRegion());
        }

        /// <summary>
        ///     Write an obejct to the given AWS Bucket
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="keyName"></param>
        /// <param name="contentBody"></param>
        /// <param name="contentType"></param>
        public void WriteAnObject(string keyName, string contentBody)
        {
            try
            {
                // Put request including metadata along with the content
                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = keyName,
                    ContentBody = contentBody,
                    //ContentType = contentType
                };

                // Include Metadata, Date object added
                //putRequest.Metadata.Add("DateAdded", DateTime.Now.ToString());
                using (IAmazonS3 _s3Client = GetAmazonS3Client())
                {
                    var response = _s3Client.PutObject(putRequest);
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                // Catch AWS exception.
                if (amazonS3Exception.ErrorCode != null && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    //LoggerService.Error(string.Format("AWS access error. AWS ErrorCode: {0} AWS ErrorType: {1}",
                    //    amazonS3Exception.ErrorCode,
                    //    amazonS3Exception.ErrorType));
                }
                else
                {
                    //LoggerService.Error(
                    //    string.Format("AWS ListObjects unknown fatal error. AWS ErrorCode: {0} AWS ErrorType: {1}",
                    //        amazonS3Exception.ErrorCode,
                    //        amazonS3Exception.ErrorType));
                }
            }
        }
        /// <summary>
        ///     List the objects in a bucket
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="prefix"></param>
        public List<string> ListObjects(string folderName)
        {
            var items = new List<string>();
            try
            {
                var request = new ListObjectsRequest
                {
                    BucketName = _bucketName,
                    Prefix = folderName,
                    //MaxKeys = 2
                };
                ListObjectsResponse response;
                using (IAmazonS3 _s3Client = GetAmazonS3Client())
                {
                    response = _s3Client.ListObjects(request);
                }

                foreach (var entry in response.S3Objects)
                {
                    items.Add(entry.Key);
                    // Write to a view or return a list
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                // Catch AWS exception.
                if (amazonS3Exception.ErrorCode != null && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    //LoggerService.Error(string.Format("AWS access error. AWS ErrorCode: {0} AWS ErrorType: {1}",
                    //    amazonS3Exception.ErrorCode,
                    //    amazonS3Exception.ErrorType));
                }
                else
                {
                    //LoggerService.Error(
                    //    string.Format("AWS ListObjects fatal error. aws errorcode: {0} aws errortype: {1}",
                    //        amazonS3Exception.ErrorCode,
                    //        amazonS3Exception.ErrorType));
                }
            }
            return items;
        }
        /// <summary>
        ///     AWS Get an object
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="keyName">Friendly Name</param>
        public void GetObject(string bucketName, string keyName)
        {
            try
            {
                using (IAmazonS3 _s3Client = GetAmazonS3Client())
                {
                    GetObjectRequest request = new GetObjectRequest
                    {
                        BucketName = bucketName,
                        Key = keyName
                    };
                    using (GetObjectResponse response = _s3Client.GetObject(request))
                    {
                        string dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), keyName);
                        if (!File.Exists(dest))
                        {
                            response.WriteResponseStreamToFile(dest);
                        }


                    }
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                // Catch AWS exception.
                if (amazonS3Exception.ErrorCode != null && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    //LoggerService.Error(string.Format("AWS access error. AWS ErrorCode: {0} AWS ErrorType: {1}",
                    //    amazonS3Exception.ErrorCode,
                    //    amazonS3Exception.ErrorType));
                }
                else
                {
                    //LoggerService.Error(
                    //    string.Format("AWS GetObject attempt fatal error. aws errorcode: {0} aws errortype: {1}",
                    //        amazonS3Exception.ErrorCode,
                    //        amazonS3Exception.ErrorType));
                }
            }
        }

        /// <summary>
        ///     AWS Read an object
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public string ReadDataObject(string bucketName, string keyName)
        {
            var responseBody = "";
            using (IAmazonS3 _s3Client = GetAmazonS3Client())
            {
                var request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = keyName
                };
                using (var response = _s3Client.GetObject(request))
                using (var responseStream = response.ResponseStream)
                using (var reader = new StreamReader(responseStream))
                {
                    var title = response.Metadata["x-amz-meta-title"];
                    responseBody = reader.ReadToEnd();
                }
            }
            return responseBody;
        }


        /// <summary>
        ///     get Folder from bucket
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="sourcekeyName"></param>
        /// <param name="destinationKeyName"></param>
        /// <returns></returns>
        public List<string> getListFolder()
        {
            try
            {
                var items = new List<string>();
                S3DirectoryInfo[] response;
                using (IAmazonS3 _s3Client = GetAmazonS3Client())
                {
                    S3DirectoryInfo source = new S3DirectoryInfo(_s3Client, _bucketName);
                    response = source.GetDirectories();
                }

                foreach (var folder in response)
                    items.Add(folder.Name);

                return items;
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {

                }
                else
                {

                }
                return null;
            }

        }


        /// <summary>
        ///     get list of file by Folder from bucket
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="folderName"></param>
        /// <returns>list of files</returns>
        public List<string> getListFileByFolder(string bucketName, string folderName)
        {
            try
            {
                var items = new List<string>();
                S3FileInfo[] response;
                using (IAmazonS3 _s3Client = GetAmazonS3Client())
                {
                    S3DirectoryInfo source = new S3DirectoryInfo(_s3Client, bucketName, folderName);
                    response = source.GetFiles();
                    
                }

                foreach (var entry in response)
                {
                    items.Add(entry.Name);
                    // Write to a view or return a list
                }

                return items;
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                }
                else
                {
                }
                return null;
            }

        }




        /// <summary>
        ///     AWS Copy to
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="sourcekeyName"></param>
        /// <param name="destinationKeyName"></param>
        /// <returns></returns>
        public S3DirectoryInfo CopyTo(string sourcekeyName, string destinationKeyName)
        {
            try
            {
                using (IAmazonS3 _s3Client = GetAmazonS3Client())
                {
                    S3DirectoryInfo source = new S3DirectoryInfo(_s3Client, _bucketName, sourcekeyName);
                    S3DirectoryInfo target = new S3DirectoryInfo(_s3Client, _bucketName, destinationKeyName);
                    if (!target.Exists)
                    {
                        target.Create();
                    }
                    var result = source.CopyTo(target);
                    return result;
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {

                }
                else
                {

                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        /// <summary>
        ///     AWS Read an object
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public MemoryStream ReadDataObjectMemory(string keyName)
        {
            using (IAmazonS3 _s3Client = GetAmazonS3Client())
            {
                var request = new GetObjectRequest
                {
                    BucketName =  _bucketName,
                    Key = keyName
                };
                using (var response = _s3Client.GetObject(request))
                {
                    MemoryStream stream = new MemoryStream();
                    response.ResponseStream.CopyTo(stream);
                    return stream;
                }

            }
        }



        /// <summary>
        ///     Finds an objects existance.
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="keyName"></param>
        //public bool FindObject(string bucketName, string keyName)
        //{
        //    var response = false;
        //    try
        //    {
        //        var s3FileInfo = new S3FileInfo(_s3Client, bucketName, keyName);
        //        if (s3FileInfo.Exists)
        //        {
        //            response = true;
        //        }
        //    }
        //    catch (AmazonS3Exception amazonS3Exception)
        //    {
        //        // Catch AWS exception.
        //        if (amazonS3Exception.ErrorCode != null && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
        //        {
        //            //LoggerService.Error(string.Format("AWS access error. AWS ErrorCode: {0} AWS ErrorType: {1}",
        //            //    amazonS3Exception.ErrorCode,
        //            //    amazonS3Exception.ErrorType));
        //        }
        //        else
        //        {
        //            //LoggerService.Error(
        //            //    string.Format("AWS ListObjects unknown fatal error. AWS ErrorCode: {0} AWS ErrorType: {1}",
        //            //        amazonS3Exception.ErrorCode,
        //            //        amazonS3Exception.ErrorType));
        //        }
        //    }
        //    return response;
        //}

        public string zip(string folderName)
        {
            string zipFilename = "",destZipName = "";

            try
            {

                //zipFilename = System.Web.Hosting.HostingEnvironment.MapPath("/myZip.zip");
                zipFilename = "need to get path with asp.net core 2.2";
                //var zipFilename = System.Web.Hosting.HostingEnvironment.MapPath("/myZip" + DateTime.Now.ToString("MMddyyyyhhmms") + ".zip");
                using (IAmazonS3 _s3Client = GetAmazonS3Client())
                {
                    S3DirectoryInfo rootDir = new S3DirectoryInfo(_s3Client, _bucketName, folderName);
                    using (var zip = new ZipFile())
                    {
                        zip.Name = zipFilename;
                        AWSConfig.addFiles(zip, rootDir, "");
                    }
                    // Move local zip file to S3
                    //var fileInfo = rootDir.GetFile("data" + DateTime.Now.ToString("MMddyyyyhhmms") + ".zip");
                    destZipName = DateTime.Now.ToString("MMddyyyyhhmms") + ".zip";
                    var fileInfo = rootDir.GetFile(destZipName);
                    fileInfo.MoveFromLocal(zipFilename);
                    return destZipName;
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            
        }

        public Boolean CreateFunction(string folderName, string zipfile)
        {
            try
            {
                using (AmazonLambdaClient client = new AmazonLambdaClient(AWSConfig.GetCredentials(), AWSConfig.GetRegion()))
                {
                    CreateFunctionRequest cfr = new CreateFunctionRequest
                    {
                        FunctionName = folderName,
                        Handler = "FastCREDeal.Main",
                        MemorySize = 128,
                        Timeout =20,
                        Publish = true,
                        Description = "",
                        Code = new FunctionCode() { S3Bucket = _bucketName, S3Key = zipfile },
                        Role = "arn:aws:iam::697842051559:role/service-role/dev-lambda-execution",
                        Runtime = "python3.6",

                    };

                    CreateFunctionResponse response = client.CreateFunction(cfr);
                    if (response.HttpStatusCode == System.Net.HttpStatusCode.Created) return true;
                    else return false;
                }
            }
            catch (Exception ex)
            {

                return false;
            }

        }


        public Boolean UpdateFunction( string folderName, string zipfile)
        {
            try
            {
                AmazonLambdaClient client = new AmazonLambdaClient(AWSConfig.GetCredentials(), AWSConfig.GetRegion());

                UpdateFunctionCodeRequest ucr = new UpdateFunctionCodeRequest
                {
                    FunctionName = folderName,
                    Publish = true,
                    S3Bucket = _bucketName,
                    S3Key = zipfile,
                };

                UpdateFunctionCodeResponse response = client.UpdateFunctionCode(ucr);
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK) return true;
                else return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public string InvokeFunction(string folderName, string file,string payLoad)
        {
            try
            {
                
                AmazonLambdaClient client = new AmazonLambdaClient(AWSConfig.GetCredentials(), AWSConfig.GetRegion());

                InvokeRequest ir = new InvokeRequest();
                ir.FunctionName = folderName;
                ir.InvocationType = InvocationType.RequestResponse;
                ir.Payload = payLoad;

                
                if (string.IsNullOrEmpty(payLoad))
                {
                    ir = new InvokeRequest();
                    ir.FunctionName = folderName;
                    ir.InvocationType = InvocationType.RequestResponse;
                }

                InvokeResponse response = client.Invoke(ir);
                var sr = new StreamReader(response.Payload);
                JsonReader reader = new JsonTextReader(sr);

                var serilizer = new JsonSerializer();
                var op = serilizer.Deserialize(reader);
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    if (response.FunctionError != null)
                    {
                        //return Newtonsoft.Json.Linq.JObject.Parse(op.ToString())["errorMessage"].ToString();
                        return "";
                    }
                    return op.ToString();
                }
                return "";
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public Task DeleteAllZipFile(string folderName)
        {
            return Task.Run(() =>
            {
                try
                {
                    S3FileInfo[] response;
                    using (IAmazonS3 _s3Client = GetAmazonS3Client())
                    {
                        S3DirectoryInfo source = new S3DirectoryInfo(_s3Client, _bucketName, folderName);
                        response = source.GetFiles();

                        foreach (var entry in response)
                        {
                            if (entry.Extension == "zip")
                            {
                                entry.Delete();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            });

        }

        public string GetZipFileByFolder(string folderName)
        {
            try
            {
                string zipFileName = "";
                S3FileInfo[] response;
                using (IAmazonS3 _s3Client = GetAmazonS3Client())
                {
                    S3DirectoryInfo source = new S3DirectoryInfo(_s3Client, _bucketName, folderName);
                    response = source.GetFiles();
                }

                foreach (var entry in response)
                {
                    if (entry.Extension == "zip")
                    {
                        zipFileName = folderName+"/"+entry.Name;
                    }
                }

                return zipFileName;
            }
            catch (Exception ex)
            {
                return "";
            }

        }

    }

    public sealed class AWSConfig
    {
        public static void addFiles(ZipFile zip, S3DirectoryInfo dirInfo, string archiveDirectory)
        {

            foreach (var childDirs in dirInfo.GetDirectories())
            {
                var entry = zip.AddDirectoryByName(childDirs.Name);
                addFiles(zip, childDirs, archiveDirectory + entry.FileName);
            }

            foreach (var file in dirInfo.GetFiles())
            {
                using (var stream = file.OpenRead())
                {
                    zip.AddEntry(archiveDirectory + file.Name, stream);

                    // Save after adding the file because to force the 
                    // immediate read from the S3 Stream since 
                    // we don't want to keep that stream open.
                    zip.Save();
                }
            }
        }

        public static BasicAWSCredentials GetCredentials()
        {

            //Amazon.CognitoIdentity.CognitoAWSCredentials credentials = new Amazon.CognitoIdentity.CognitoAWSCredentials("us-east-1:c1d6db49-ad35-4ee7-90e7-4d2c1680ff9a", AWSConfig.GetRegion());
            //return credentials;
            var key = "AKIAJMRB6FF5LA4CJHEQ";
            var secret = "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";
            return new  BasicAWSCredentials(key, secret);
        }
        public static RegionEndpoint GetRegion()
        {
            // Your amazon region
            var region = RegionEndpoint.USEast1;
            return region;
        }
    }
}