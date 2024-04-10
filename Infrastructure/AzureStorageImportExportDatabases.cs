using CRES.BusinessLogic;
using CRES.BusinessLogic;
using CRES.DataContract;
using CRES.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Threading;
using System.Web;

namespace CRES.Services
{
    public class AzureStorageImportExportDatabases
    {
        string tenantId = "b8267886-f0c8-4160-ab6f-6e97968fdc90";
        string clientId = "3c50164d-80e6-40c8-bab3-561f5f740027"; // Application ID
        string clientSecret = "V06yv6TPVHkNR4+AJWWgDQjUg2gLga4RN3t8DTWbKYs=";
        string YourSubscriptionId = "021c83fc-1e2a-4da6-92dd-8e1d74c0e1cb";
        string YourResourceGroupName = "DevEnvironment";
        string YourServerName = "b0xesubcki.database.windows.net";
        string YourDatabaseName = "CRES4_Test";
        //string StorageAccessKey = "?sv=2018-03-28&ss=bfqt&srt=sco&sp=rwdlacup&se=2019-01-09T21:50:08Z&st=2019-01-09T13:50:08Z&spr=https&sig=B7SX%2Bn5Nx%2FEqHyHQOqypN6lUWr2xOs5YmXOpEi%2BbcVk%3D";
        string StorageAccessKey = "cjHDvVrAJFOuqV/6JVAJnrOCym9B0JW0jAMuo8pZASelw4wYwQ3sQCmCiKwhqvQIaQ+V9d8x4qH9h6CzVQwvuA==";
        string YourBlobStorageKey = "cjHDvVrAJFOuqV/6JVAJnrOCym9B0JW0jAMuo8pZASelw4wYwQ3sQCmCiKwhqvQIaQ+V9d8x4qH9h6CzVQwvuA==";
        string YourStorageUri = "https://cres5appbackup.blob.core.windows.net/dbcontainer/";
        string YourSqlServerUsername = "CREAdmin";
        string YourSqlServerPassword = "Indore2016#";
        private string connstring = "";
        SqlConnection connection = new SqlConnection();

        public AzureStorageImportExportDatabases()
        {
            GetConfigSetting();
            connstring = Sectionroot.GetSection("storage:container:connectionstring").Value;

        }

        public string GetAccessToken() //(string tenantId, string clientId, string clientSecret)
        {

            string authContextURL = "https://login.windows.net/" + tenantId;
            var authenticationContext = new AuthenticationContext(authContextURL);
            var credential = new ClientCredential(clientId: clientId, clientSecret: clientSecret);
            var result = authenticationContext.AcquireTokenAsync(resource: "https://management.azure.com/", clientCredential: credential).Result;

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            string token = "Bearer " + result.AccessToken;
            return token;
        }

        public bool IsDatabaseImportExportCompleted(string authorisationToken, string url)
        {
            var status = GetDatabaseImportExportStatus(authorisationToken, url);

            while (status.Contains("Running"))
            {
                Thread.Sleep(20000);
                status = GetDatabaseImportExportStatus(authorisationToken, url);
            }

            if (status == "Completed")
                return true;

            return false;
        }

        public string GetDatabaseImportExportStatus(string authorisationToken, string url)
        {

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", authorisationToken);
                var response = client.GetStringAsync(url).Result;
                JObject jsonObject = JObject.Parse(response);

                var status = (string)jsonObject.SelectToken("status");      //read status while running
                status = status ?? (string)jsonObject.SelectToken("properties.status");     //read status once completed
                var database = (string)jsonObject.SelectToken("properties.databaseName");

                return status;
            }
        }

        public string BackupDatabaseToBlob(PeriodicDataContract _periodicDC)
        {
            connection.ConnectionString = connstring;

            string result = "Success";
            string dbBackupName = connection.Database + "_" + Convert.ToDateTime(_periodicDC.EndDate).ToString("MMddyyyy") + "_" + DateTime.Now.ToString("MMddyyyyHHmmss") + ".bacpac";
            try
            {
                var BlobStorageKey = Sectionroot.GetSection("blobkey").Value;
                var StorageUri = Sectionroot.GetSection("bloburl").Value;
                var ServerName = Sectionroot.GetSection("sqlservername").Value;
                var DatabaseName = Sectionroot.GetSection("sqldatabasename").Value;
                var SqlServerUsername = Sectionroot.GetSection("sqlusername").Value;
                var SqlServerPassword = Sectionroot.GetSection("sqlpassword").Value;
                var DACWebServiceURL = Sectionroot.GetSection("exportwebserviceurl").Value;


                var request = WebRequest.Create(DACWebServiceURL);

                request.Method = "POST";

                System.IO.Stream dataStream = request.GetRequestStream();

                StorageUri = StorageUri + dbBackupName;

                //send crediantials in xml body
                string body = "<ExportInput xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.SqlServer.Management.Dac.ServiceTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                    "<BlobCredentials i:type=\"BlobStorageAccessKeyCredentials\">" +
                    "<Uri>" + StorageUri + "</Uri>" +
                    "<StorageAccessKey>" + BlobStorageKey + "</StorageAccessKey>" +
                    "</BlobCredentials>" +
                    "<ConnectionInfo>" +
                    "<DatabaseName>" + DatabaseName + "</DatabaseName>" +
                    "<Password>" + SqlServerPassword + "</Password>" +
                    "<ServerName>" + ServerName + "</ServerName>" +
                    "<UserName>" + SqlServerUsername + "</UserName>" +
                    "</ConnectionInfo>" +
                    "</ExportInput>";

                System.Text.UTF8Encoding utf8 = new System.Text.UTF8Encoding();
                byte[] buffer = utf8.GetBytes(body);
                dataStream.Write(buffer, 0, buffer.Length);

                dataStream.Close();
                request.ContentType = "application/xml";


                Logger.Write("Creating database backup " + dbBackupName, MessageLevel.Info);
                // The HTTP response contains the job number, a Guid serialized as XML
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {

                    if (response.StatusCode != HttpStatusCode.OK)
                        result = "Failed";
                    else
                    {
                        Logger.Write("updating periodic close AzureBlobLink", MessageLevel.Info);
                        //update AzureBlobLink(backup db name)
                        _periodicDC.AzureBlobLink = dbBackupName;
                        PeriodicLogic _periodicLogic = new PeriodicLogic();
                        _periodicLogic.UpdatePeriodicCloseAzureBlobLink(_periodicDC);
                        Logger.Write("Updated periodic close AzureBlobLink", MessageLevel.Info);
                    }

                    //System.Text.Encoding encoding = System.Text.Encoding.GetEncoding(1252);
                    //using (var responseStream = new System.IO.StreamReader(response.GetResponseStream(), encoding))
                    //{
                    //    using (System.Xml.XmlDictionaryReader reader = System.Xml.XmlDictionaryReader.CreateTextReader(responseStream.BaseStream, new System.Xml.XmlDictionaryReaderQuotas()))
                    //    {
                    //        DataContractSerializer serializer = new DataContractSerializer(typeof(Guid));
                    //        var test = serializer.ReadObject(reader, true);
                    //    }
                    //}
                }
            }
            catch (Exception ex)
            {
                result = ex.Message;
                Logger.Write("Error creating database backup " + dbBackupName + " : " + ex.Message, MessageLevel.Info);
                _periodicDC.Message = ex.Message;
                //#Remaining#
                //EmailNotification _emailNotification = new EmailNotification();
                //_emailNotification.SendPeriodicCloseExportFailNotification(_periodicDC);
            }
            return result;
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