using CRES.DataContract;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using CRES.Services.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using CRES.BusinessLogic;
using System.Xml.Linq;
using CRES.Utilities;
using Microsoft.Identity.Client;
using System.Security;

namespace CRES.Services.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class PowerBIEmbeddedController : ControllerBase
    {
        private static string Username = "";
        private static string Password = "";
        private static string AuthorityUrl = "";
        private static string ResourceUrl = "";
        private static string ClientId = "";
        private static string ApiUrl = "";
        private static string GroupId = "";
        private static string ReportId = "";
        double _defaultExpirationSeconds;
        private string _token = string.Empty;
        private readonly string TenantID = string.Empty;

        public PowerBIEmbeddedController()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
            var root = builder.Build();
            var pbiRoot = root.GetSection("PowerBIDetail");

            Username = pbiRoot.GetSection("pbiUsername").Value;
            //Password = System.Text.RegularExpressions.Regex.Unescape(pbiRoot.GetSection("pbiPassword").Value);
            Password = System.Text.RegularExpressions.Regex.Unescape(GetPassword("PowerBIPassword"));
            AuthorityUrl = pbiRoot.GetSection("pbiauthorityUrl").Value;
            ResourceUrl = pbiRoot.GetSection("pbiresourceUrl").Value;
            ClientId = pbiRoot.GetSection("pbiclientId").Value
                ;
            ApiUrl = pbiRoot.GetSection("pbiapiUrl").Value;
            GroupId = pbiRoot.GetSection("pbigroupId").Value;
            ReportId = pbiRoot.GetSection("pbireportId").Value;
            this._defaultExpirationSeconds = Convert.ToDouble(pbiRoot.GetSection("DefaultExpirationSeconds").Value);
            TenantID = pbiRoot.GetSection("pbiTenantId").Value;
        }


        [HttpGet]
        //[Services.Controllers.IsAuthenticate]
        [Route("api/pbireport/getallreports")]
        public async Task<IActionResult> GetAllReports()
        {
            GenericResult _authenticationResult = null;
            List<ReportDataContract> lstreports = new List<ReportDataContract>();
            List<ReportDataContract> lstreportsNew = new List<ReportDataContract>();
            try
            {
                IActionResult rs = await GetPowerBIReport("");
                lstreports = (List<ReportDataContract>)((Microsoft.AspNetCore.Mvc.ObjectResult)rs).Value;
                lstreports.ForEach(i => i.ReportType = "PowerBI");
                //foreach (var item in rslt)
                //{
                //    ReportDataContract _reportDC = new ReportDataContract();
                //    _reportDC.Id = item.Id;
                //    _reportDC.Name = item.Name;
                //    _reportDC.EmbedUrl = item.EmbedUrl;
                //    _reportDC.WebUrl = item.WebUrl;
                //    _reportDC.ReportType = "PowerBI";
                //    lstreports.Add(_reportDC);
                //}
                lstreports = lstreports.OrderBy(i => i.Name).ToList();
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.AccountingReport.ToString(), "Error in loading PowerBI reports", "", "", ex.TargetSite.Name.ToString(), "", ex);
            }

            //GenericResult _authenticationResult = null;
            //List<ReportDataContract> lstreports = new List<ReportDataContract>();
            //try
            //{

            //    await getToken();

            //    // Create a Power BI Client object. It will be used to call Power BI APIs.
            //    using (var client = this.CreatePowerBIClient())
            //    {
            //        //Get a list of reports.
            //        var reportsResponse = await client.Reports.GetReportsInGroupAsync(new Guid(GroupId));

            //        var viewModel = new ReportsViewModel
            //        {
            //            pbiReports = reportsResponse.Value.ToList()
            //        };


            //        foreach (var item in viewModel.pbiReports)
            //        {
            //            ReportDataContract _reportDC = new ReportDataContract();
            //            _reportDC.Id = item.Id.ToString();
            //            _reportDC.Name = item.Name;
            //            _reportDC.EmbedUrl = item.EmbedUrl;
            //            _reportDC.WebUrl = item.WebUrl;
            //            _reportDC.ReportType = "PowerBI";
            //            lstreports.Add(_reportDC);
            //        }

            //    }
            //}
            //catch { }
            try
            {
                List<ReportFileDataContract> _lstAcoreReportFiles = new List<ReportFileDataContract>();
                var headerUserID = new Guid();

                if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                {
                    headerUserID = new Guid(Request.Headers["TokenUId"]);
                }
                AccountingReportLogic accountingReportLogic = new AccountingReportLogic();
                int? totalCount = 0;
                _lstAcoreReportFiles = accountingReportLogic.GetAllReportFiles(headerUserID, 10000, 1, out totalCount);

                foreach (var item in _lstAcoreReportFiles)
                {
                    ReportDataContract _reportDC = new ReportDataContract();
                    _reportDC.ReportFileGUID = item.ReportFileGUID;
                    _reportDC.Name = item.ReportFileName;
                    _reportDC.DefaultAttributes = item.DefaultAttributes;
                    _reportDC.ReportType = "Acore";
                    _reportDC.IsAllowInput = item.IsAllowInput;
                    lstreports.Add(_reportDC);
                }
            }
            catch { }

            try
            {
                if (lstreports != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "GetAllReports succeeded",
                        TotalCount = lstreports.Count,
                        lstReport = lstreports
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "GetAllReports failed"
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
        [Route("api/pbireport/GetReportByID")]
        public async Task<IActionResult> ReportIdName(string ID)
        {
            var result = new EmbedConfig();
            IActionResult rs = await GetPowerBIReport(ID);
            result = (EmbedConfig)((Microsoft.AspNetCore.Mvc.ObjectResult)rs).Value;
            return Ok(result);
        }



        [HttpGet]
        //[Services.Controllers.IsAuthenticate]
        [Route("api/pbireport/getcrespowerbiembeddedstatus")]
        public IActionResult GetCRESPowerBIEmbeddedStatus()
        {
            GenericResult _authenticationResult = null;

            _authenticationResult = new GenericResult()
            {
                Succeeded = true,
                Message = "Power BI is ON",
                Status = "False"
            };
            try
            {
                //if (_authenticationResult != null)
                {
                    /////////////////////
                    try
                    {
                        //string res = RetrieveAzRunbookStatus("hu1A2s6GFEC%2bsFDXNKd9ZiP%2fBDFPHpEacJbxrCM8B2M%3d");
                        string uri = "https://s24events.azure-automation.net/webhooks?token=9BwHk40U0Q8YIOIwep%2b%2f9Lww292D4t%2b2B1bwAtR1H2c%3d";

                        WebRequest request = WebRequest.Create(uri);
                        //HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
                        string data = string.Empty;
                        request.Method = "POST";
                        request.ContentType = "text/plain;charset=utf-8";

                        System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                        byte[] bytes = encoding.GetBytes(data);

                        request.ContentLength = bytes.Length;

                        using (Stream requestStream = request.GetRequestStream())
                        {
                            requestStream.Write(bytes, 0, bytes.Length);
                        }

                        //WebRequest request1 = WebRequest.Create(uri);
                        //WebResponse response = request.GetResponseAsync().Result;
                        //XDocument xDoc = XDocument.Load(response.GetResponseStream());

                        //request.BeginGetResponse((x) =>
                        {
                            using (WebResponse response = (WebResponse)request.GetResponseAsync().Result)
                            {
                                using (Stream stream = response.GetResponseStream())
                                {
                                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                                    String responseString = reader.ReadToEnd();
                                    //==MessageBox.Show("Webhook Triggered at " + System.DateTime.Now + " \n Job Details : " + responseString);

                                    _authenticationResult = new GenericResult()
                                    {
                                        Succeeded = true,
                                        Message = "Power BI is ON",
                                        Status = responseString
                                    };
                                }
                            }
                        }
                        //, null);

                        //request.BeginGetResponse((x) =>
                        //{
                        //    using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(x))
                        //    {
                        //        using (Stream stream = response.GetResponseStream())
                        //        {
                        //            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                        //            String responseString = reader.ReadToEnd();
                        //            //==MessageBox.Show("Webhook Triggered at " + System.DateTime.Now + " \n Job Details : " + responseString);

                        //            _authenticationResult = new GenericResult()
                        //            {
                        //                Succeeded = true,
                        //                Message = "Power BI is ON",
                        //                Status = responseString
                        //            };
                        //        }
                        //    }
                        //}, null);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    /////////////////

                }
                //else
                //{
                //    _authenticationResult = new GenericResult()
                //    {
                //        Succeeded = false,
                //        Message = "Power BI is OFF",
                //        Status = "False"
                //    };
                //}
            }
            catch (Exception ex)
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            //JavaScriptSerializer serializer = new JavaScriptSerializer();
            //dynamic item = serializer.Deserialize<object>(_authenticationResult.Status);

            //dynamic item  = JsonConvert.DeserializeObject<object>(_authenticationResult.Status);
            //string strJobIds = (((object[])(item["JobIds"]))[0]).ToString(); //item["JobIds"].ToString();

            string strJobIds = JObject.Parse(_authenticationResult.Status)["JobIds"][0].ToString();

            string Automation_clientId = "e4494c33-946f-4a2b-a4cd-2d36254de006";
            string Automation_tenantId = "ee08cd6b-8aba-4d0c-8c80-543baf6a3347";
            string Automation_clientKey = "d*1W.SWN]Hr.Ydr9GVpLutWeMk864cJ8"; //"LjBe+RTMPsAZA6UukJvV+UjsqEBQ33tLgFegP19EV613hry6ZUg/BNJNACAmFysR+3Us0UNP5V6Uve2CAKp9eg==";
            string Automation_Token = GetAccessToken(Automation_tenantId, Automation_clientId, Automation_clientKey);

            int milliseconds = 30000; //30 second
            Thread.Sleep(milliseconds);
            string strStatus = RetrieveAzRunbookStatus(Automation_Token, strJobIds);

            if (strStatus == "")
            {
                milliseconds = 30000; //30 second             
                Thread.Sleep(milliseconds);
                strStatus = RetrieveAzRunbookStatus(Automation_Token, strJobIds);
            }

            if (strStatus == "")
            {
                milliseconds = 30000; //30 second             
                Thread.Sleep(milliseconds);
                strStatus = RetrieveAzRunbookStatus(Automation_Token, strJobIds);
            }

            if (strStatus == "")
            {
                milliseconds = 30000; //30 second             
                Thread.Sleep(milliseconds);
                strStatus = RetrieveAzRunbookStatus(Automation_Token, strJobIds);
            }

            if (strStatus == "")
            {
                milliseconds = 30000; //30 second             
                Thread.Sleep(milliseconds);
                strStatus = RetrieveAzRunbookStatus(Automation_Token, strJobIds);
            }

            if (strStatus == "")
            {
                milliseconds = 30000; //30 second             
                Thread.Sleep(milliseconds);
                strStatus = RetrieveAzRunbookStatus(Automation_Token, strJobIds);
            }

            if (strStatus == "")
            {
                milliseconds = 30000; //30 second             
                Thread.Sleep(milliseconds);
                strStatus = RetrieveAzRunbookStatus(Automation_Token, strJobIds);
            }

            _authenticationResult.Status = strStatus;
            return Ok(_authenticationResult);
        }

        private static string GetAccessToken(string tenantId, string clientId, string clientKey)
        {
            Console.WriteLine("Begin GetAccessToken");
            string authContextURL = "https://login.windows.net/" + tenantId;
            var authenticationContext = new AuthenticationContext(authContextURL);
            var credential = new Microsoft.IdentityModel.Clients.ActiveDirectory.ClientCredential(clientId, clientKey);
            var result = authenticationContext.AcquireTokenAsync("https://management.azure.com/", credential);
            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }
            string token = result.Result.AccessToken;
            Console.Write(token);
            return token;

        }

        private static string RetrieveAzRunbookStatus(string token, string strJobIds)
        {
            string strSubscriptionId = "badf2337-12a5-4c87-892c-ad0c7ed614d0";
            string strResourceGroupName = "CRES_PowerBI";
            string strAutomationAccountName = "CRES4-AzureAutomation";
            string strJobName = "GetCRESPowerBIEmbeddedStatus";

            //https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Automation/automationAccounts/{automationAccountName}/jobs/{jobName}/output?api-version=2017-05-15-preview
            string URL = "https://management.azure.com/subscriptions/" + strSubscriptionId + "/resourceGroups/" + strResourceGroupName + "/providers/Microsoft.Automation/automationAccounts/" + strAutomationAccountName + "/jobs/" + strJobIds + "/output?api-version=2017-05-15-preview";
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            HttpResponseMessage httpResponseMessage = client.GetAsync(URL).Result;

            string response;
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                response = httpResponseMessage.Content.ReadAsStringAsync().Result;

            }
            else
            {
                response = httpResponseMessage.Content.ReadAsStringAsync().Result;
                // var mylist = JsonConvert.DeserializeObject<ResponseClass>(response);
            }
            Regex regex = new Regex("\r\n");
            string Finalresponse = regex.Replace(response, "");

            return Finalresponse.Trim();

        }

        [HttpGet]
        //[Services.Controllers.IsAuthenticate]
        [Route("api/pbireport/updatecrespowerbiembeddedstatus")]
        public IActionResult UpdateCRESPowerBIEmbeddedStatus()
        {
            GenericResult _authenticationResult = null;
            try
            {
                //if (_authenticationResult != null)
                {
                    /////////////////////
                    try
                    {
                        //string res = RetrieveAzRunbookStatus("hu1A2s6GFEC%2bsFDXNKd9ZiP%2fBDFPHpEacJbxrCM8B2M%3d");
                        string uri = "https://s24events.azure-automation.net/webhooks?token=oLWtW8SHOZxbmbPWgENw6Ryr1dBcc9WnaXrU%2fxatcFg%3d";

                        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
                        string data = string.Empty;
                        request.Method = "POST";
                        request.ContentType = "text/plain;charset=utf-8";

                        System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                        byte[] bytes = encoding.GetBytes(data);

                        request.ContentLength = bytes.Length;

                        using (Stream requestStream = request.GetRequestStream())
                        {
                            requestStream.Write(bytes, 0, bytes.Length);
                        }

                        request.BeginGetResponse((x) =>
                        {
                            using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(x))
                            {
                                using (Stream stream = response.GetResponseStream())
                                {
                                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                                    String responseString = reader.ReadToEnd();
                                    //==MessageBox.Show("Webhook Triggered at " + System.DateTime.Now + " \n Job Details : " + responseString);

                                    _authenticationResult = new GenericResult()
                                    {
                                        Succeeded = true,
                                        Message = "Power BI is ON",
                                        Status = responseString
                                    };
                                }
                            }
                        }, null);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    /////////////////

                }
                //else
                //{
                //    _authenticationResult = new GenericResult()
                //    {
                //        Succeeded = false,
                //        Message = "Power BI is OFF",
                //        Status = "False"
                //    };
                //}
            }
            catch (Exception ex)
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            //JavaScriptSerializer serializer = new JavaScriptSerializer();
            //dynamic item = serializer.Deserialize<object>(_authenticationResult.Status);

            dynamic item = JsonConvert.DeserializeObject<object>(_authenticationResult.Status);
            string strJobIds = (((object[])(item["JobIds"]))[0]).ToString(); //item["JobIds"].ToString();

            string Automation_clientId = "e4494c33-946f-4a2b-a4cd-2d36254de006";
            string Automation_tenantId = "ee08cd6b-8aba-4d0c-8c80-543baf6a3347";
            string Automation_clientKey = "d*1W.SWN]Hr.Ydr9GVpLutWeMk864cJ8"; //"LjBe+RTMPsAZA6UukJvV+UjsqEBQ33tLgFegP19EV613hry6ZUg/BNJNACAmFysR+3Us0UNP5V6Uve2CAKp9eg==";
            string Automation_Token = GetAccessToken(Automation_tenantId, Automation_clientId, Automation_clientKey);

            int milliseconds = 30000; //60 second
            Thread.Sleep(milliseconds);

            string strStatus = RetrieveAzRunbookStatus(Automation_Token, strJobIds);

            if (strStatus == "" || strStatus.ToLower() == "resuming")
            {
                milliseconds = 30000; //30 second             
                Thread.Sleep(milliseconds);
                strStatus = RetrieveAzRunbookStatus(Automation_Token, strJobIds);
            }

            if (strStatus == "" || strStatus.ToLower() == "resuming")
            {
                milliseconds = 30000; //30 second             
                Thread.Sleep(milliseconds);
                strStatus = RetrieveAzRunbookStatus(Automation_Token, strJobIds);
            }

            if (strStatus == "" || strStatus.ToLower() == "resuming")
            {
                milliseconds = 30000; //30 second             
                Thread.Sleep(milliseconds);
                strStatus = RetrieveAzRunbookStatus(Automation_Token, strJobIds);
            }

            if (strStatus == "" || strStatus.ToLower() == "resuming")
            {
                milliseconds = 30000; //30 second             
                Thread.Sleep(milliseconds);
                strStatus = RetrieveAzRunbookStatus(Automation_Token, strJobIds);
            }

            if (strStatus == "" || strStatus.ToLower() == "resuming")
            {
                milliseconds = 30000; //30 second             
                Thread.Sleep(milliseconds);
                strStatus = RetrieveAzRunbookStatus(Automation_Token, strJobIds);
            }

            if (strStatus == "" || strStatus.ToLower() == "resuming")
            {
                milliseconds = 30000; //30 second             
                Thread.Sleep(milliseconds);
                strStatus = RetrieveAzRunbookStatus(Automation_Token, strJobIds);
            }

            _authenticationResult.Status = strStatus;
            return Ok(_authenticationResult);
        }


        private async Task getToken()
        {
            //if (!string.IsNullOrEmpty(_token) && ValidateToken(_token))
            //    return;

            HttpClient cclient = new HttpClient();
            string tokenEndpoint = "https://login.microsoftonline.com/" + TenantID + "/oauth2/token";
            var body = "resource=" + ResourceUrl + "&client_id=" + ClientId + "&grant_type=password&username=" + Username + "&password=" + Password;
            var stringContent = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

            var result = await cclient.PostAsync(tokenEndpoint, stringContent).ContinueWith<string>((response) =>
            {
                return response.Result.Content.ReadAsStringAsync().Result;
            });

            JObject jobject = JObject.Parse(result);

            _token = jobject["access_token"].Value<string>();
        }

        private IPowerBIClient CreatePowerBIClient()
        {
            var tokenCredentials = new TokenCredentials(_token, "Bearer");
            return new PowerBIClient(new Uri(ApiUrl), tokenCredentials);
        }
        public async Task<IActionResult> GetPowerBIReport(string ReportID)
        {
            Guid groupID = new Guid(GroupId);
            try
            {
                IPublicClientApplication clientApp = PublicClientApplicationBuilder.Create(ClientId).WithAuthority(AuthorityUrl).Build();
                var userAccounts = clientApp.GetAccountsAsync().Result;

                string[] scope = ResourceUrl.Split(';');
                SecureString password = new SecureString();
                foreach (var key in Password)
                {
                    password.AppendChar(key);
                }
                var tokenresult = clientApp.AcquireTokenByUsernamePassword(scope, Username, password).ExecuteAsync().Result;

                var tokenCredentials = new TokenCredentials(tokenresult.AccessToken, "Bearer");
                var powerbiclient = new PowerBIClient(new Uri(ApiUrl), tokenCredentials);

                try
                {
                    if (string.IsNullOrEmpty(ReportID))
                    {
                        List<ReportDataContract> lstreports = new List<ReportDataContract>();
                        using (var client = powerbiclient)
                        {
                            var reportsResponse = await client.Reports.GetReportsInGroupAsync(groupID);

                            var viewModel = new ReportsViewModel
                            {
                                pbiReports = reportsResponse.Value.ToList()
                            };


                            foreach (var item in viewModel.pbiReports)
                            {
                                ReportDataContract _reportDC = new ReportDataContract();
                                _reportDC.Id = item.Id.ToString();
                                _reportDC.Name = item.Name;
                                _reportDC.EmbedUrl = item.EmbedUrl;
                                _reportDC.WebUrl = item.WebUrl;
                                lstreports.Add(_reportDC);
                            }

                            return new OkObjectResult(lstreports);
                        }
                    }
                    else
                    {
                        var result = new EmbedConfig();
                        using (var client = powerbiclient)
                        {
                            var reportsResponse = await client.Reports.GetReportsInGroupAsync(groupID);
                            var report = reportsResponse.Value.FirstOrDefault(r => r.Id == new Guid(ReportID));

                            if (report == null)
                            {
                                return new BadRequestObjectResult(" Group has no reports.");
                            }
                            var datasets = client.Datasets.GetDatasetInGroup(groupID, report.DatasetId.ToString());
                            result.IsEffectiveIdentityRequired = datasets.IsEffectiveIdentityRequired;
                            result.IsEffectiveIdentityRolesRequired = datasets.IsEffectiveIdentityRolesRequired;
                            GenerateTokenRequest generateTokenRequestParameters;
                            List<EffectiveIdentity> filters = new List<EffectiveIdentity>();
                            //filters.Add(new EffectiveIdentity
                            //{
                            //    /* Setting RLS*/
                            //    Datasets = new List<string> { report.DatasetId },
                            //});

                            // Generate Embed Token with effective identities.
                            generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "view");
                            //generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "view");

                            var tokenResponse = await client.Reports.GenerateTokenInGroupAsync(groupID, report.Id, generateTokenRequestParameters);

                            if (tokenResponse == null)
                            {
                                return new BadRequestObjectResult(" - Failed to generate embed token.");
                            }

                            // Generate Embed Configuration.
                            result.EmbedToken = tokenResponse;
                            result.EmbedUrl = report.EmbedUrl;
                            result.Id = report.Id.ToString();
                            result.ReportName = report.Name;

                            return new OkObjectResult(result);

                        }
                    }
                }
                catch (Exception ex)
                {
                    return new BadRequestObjectResult(ex.Message);
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }

        }

        [HttpGet]
        //[Services.Controllers.IsAuthenticate]
        [Route("api/pbireport/updatecrespowerbidataset")]
        public IActionResult UpdatePowerBIReportDataset(string ID)
        {
            GenericResult _authenticationResult = null;
            Guid groupID = new Guid(GroupId);
            try
            {
                IPublicClientApplication clientApp = PublicClientApplicationBuilder.Create(ClientId).WithAuthority(AuthorityUrl).Build();
                var userAccounts = clientApp.GetAccountsAsync().Result;

                string[] scope = ResourceUrl.Split(';');
                SecureString password = new SecureString();
                foreach (var key in Password)
                {
                    password.AppendChar(key);
                }
                var tokenresult = clientApp.AcquireTokenByUsernamePassword(scope, Username, password).ExecuteAsync().Result;

                //var tokenCredentials = new TokenCredentials(tokenresult.AccessToken, "Bearer");
                //var powerbiclient = new PowerBIClient(new Uri(ApiUrl), tokenCredentials);

                try
                {
                    string inputjsonstring = JsonConvert.SerializeObject("{ notifyOption: ''}");
                    string URL = "https://api.powerbi.com/v1.0/myorg/datasets/" + ID + "/refreshes";  //520a5f04-0f3f-4db5-af5d-cef13a10808d
                    HttpClient client = new HttpClient();

                    using (var content = new StringContent(JsonConvert.SerializeObject(inputjsonstring), System.Text.Encoding.UTF8, "application/json"))
                    {
                        client.DefaultRequestHeaders.Remove("Authorization");
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + tokenresult.AccessToken);
                        HttpResponseMessage httpResponseMessage = client.PostAsync(URL, content).Result;
                    }

                }
                catch (Exception ex)
                {
                    return new BadRequestObjectResult(ex.Message);
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
            _authenticationResult = new GenericResult()
            {
                Succeeded = true,
                Message = "Report refreshed successfully.Please close this page and reload the report after 5 minutes."
            };
            return Ok(_authenticationResult);
            //return Ok("Report refreshed successfully.Please close this page and reload the report after 5 minutes.");
        }

        public string GetPassword(string key)
        {
            string pwd = "";
            AppConfigLogic appLogic = new AppConfigLogic();
            List<AppConfigDataContract> lstAppConfig = new List<AppConfigDataContract>();
            lstAppConfig = appLogic.GetAppConfigByKey(Guid.NewGuid(), key);
            if (lstAppConfig.Count() > 0)
                pwd = lstAppConfig.FirstOrDefault().Value;
            return pwd;
        }
    }
}