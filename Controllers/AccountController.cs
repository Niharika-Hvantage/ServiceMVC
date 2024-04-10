using System;
using System.Collections.Generic;
using System.Linq;
using CRES.DataContract;
using CRES.BusinessLogic;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IO;
using CRES.Utilities;
using System.Data;
using Microsoft.Graph;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Graph.Auth;

namespace CRES.ServicesNew.Controllers
{

    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    //[EnableCors(origins: "*", headers: "*", methods: "*")]

    public class AccountController : ControllerBase
    {

        public IEmailNotification _iEmailNotification;
        public AccountController(IEmailNotification iemailNotification)
        {
            _iEmailNotification = iemailNotification;
        }

        [HttpGet]
       // [Services.Controllers.IsAuthenticate]
        [Route("api/Account/GetTest")]
        public IActionResult GetTest()
        {
            GenericResult _authenticationResult = null;
            _authenticationResult = new GenericResult()
            {
                Succeeded = true,
                Message = "Authentication succeeded",
            };
            //return Request.CreateResponse(HttpStatusCode.OK, _authenticationResult); ;

            //string strResult = Encryptor.hashSH1("$%2glqLY14KN");
            return Ok(_authenticationResult);
        }


        [HttpGet]
        [Services.Controllers.IsAuthenticate]       
        [Route("api/Account/checkifuserislogedin")]
        public IActionResult CheckifUserIsLogedIN()
        {
           
            bool islogediN = false;
            GenericResult _authenticationResult = null;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            if (headerUserID != "")
            {
                islogediN = true;                
            }
            else
            {
                islogediN = false;            
            }

            try
            {
                if (islogediN==true)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded"
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
            }
            return Ok(_authenticationResult);
        }

        /* Common methods */
        public string TokenGenerator(UserDataContract userDC)
        {

            IConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddJsonFile(Path.Combine(System.IO.Directory.GetCurrentDirectory(), "appsettings.json"));
            var root = builder.Build();
            var ServerName = root.GetSection("Application").GetSection("ServerName").Value;

            return Encryptor.MD5Hash(ServerName.ToString() +
                userDC.UserID + userDC.Password + userDC.UserToken);
        }

        //GetUserCredentialByUserID
        public UserDataContract GetUserCredentialByUserID(Guid UserID, Guid? DelegatedUserID)
        {
            UserLogic userlogic = new UserLogic();
            UserDataContract userDC = new UserDataContract();
            userDC = userlogic.GetUserCredentialByUserID(UserID, DelegatedUserID);
            if (userDC.Login != null && userDC.Password != null && userDC.Password != "")
            {
                userDC = userlogic.ValidateUser(userDC.Login, userDC.Password);
            }
            return userDC;
        }



        [HttpPost]
        [Route("api/account/authenticate")]
        public IActionResult Login([FromBody] UserDataContract _userDataContract)
        {
            GenericResult _authenticationResult = null;
            UserDataContract _userdatacontract = new UserDataContract();
            string EncruptPassword = Encryptor.MD5Hash(_userDataContract.Password);

            CRES.BusinessLogic.UserLogic userlogic = new CRES.BusinessLogic.UserLogic();
            _userdatacontract = userlogic.ValidateUser(_userDataContract.Login, EncruptPassword);//_userDataContract.Password
            try
            {
                if (_userdatacontract != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        Token = TokenGenerator(_userdatacontract),
                        UserData = _userdatacontract
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        //Message = "Authentication failed"
                        Message = "Username and password don't match to our records."
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in Login", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_authenticationResult); ;

        }

        [HttpPost]
        [Route("api/account/logout")]
        public IActionResult Logout([FromBody] UserDataContract _userDataContract)
        {
            GenericResult _authenticationResult = null;
            // Encryptor.MD5Hash(_userDataContract.Password);
            try
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Logged out succeeded",
                    Token = ""
                };

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in logout", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_authenticationResult); ;

        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/getuserperission")]
        public IActionResult GetUserPerissions()
        {
            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            CRES.BusinessLogic.UserPermissionLogic upl = new CRES.BusinessLogic.UserPermissionLogic();
            //to get user 
            List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "master");
            try
            {
                if (permissionlist != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
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
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in getuserperission", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
                builder.AddJsonFile(Path.Combine(System.IO.Directory.GetCurrentDirectory(), "appsettings.json"));
                var root = builder.Build();
                Sectionroot = root.GetSection("Application");
            }
        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/getsystemconfigkeys")]
        public IActionResult GetSystemConfigKeys()
        {
            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            List<SystemConfigKeys> systemConfigKeyslist = new List<SystemConfigKeys>();
            GetConfigSetting();

            SystemConfigKeys _systemConfigKeys = new SystemConfigKeys();
            _systemConfigKeys.Key = "dialogflowbaseurl";
            _systemConfigKeys.Value = Sectionroot.GetSection("dialogflowbaseurl").Value;

            systemConfigKeyslist.Add(_systemConfigKeys);

            _systemConfigKeys = new SystemConfigKeys();
            _systemConfigKeys.Key = "API_Key";
            _systemConfigKeys.Value = Sectionroot.GetSection("API_Key").Value;

            systemConfigKeyslist.Add(_systemConfigKeys);

            try
            {
                if (systemConfigKeyslist != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Config list fetch succeeded",
                        SystemConfigKeysList = systemConfigKeyslist

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
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in getsystemconfigkeys", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }


        [HttpGet]
        [Route("api/account/GetEncrptString")]
        public string GetEncrptString(string Input)
        {
            string encrptString = "";
            encrptString = Encryptor.MD5Hash(Input);
            return encrptString;
        }

        //    [Services.Controllers.DeflateCompression]
        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/getdashboardbyuserid")]
        public IActionResult GetDashBoardByUserID()

        {
            GenericResult _genericResult = null;
            DashBoardLogic dashboardlogic = new DashBoardLogic();
            List<DashBoardDataContract> _dashBoard = new List<DashBoardDataContract>();
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            _dashBoard = dashboardlogic.GetDashBoardByUserID(headerUserID);
            try
            {
                if (_dashBoard != null)
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Records found",
                        lstdashBoard = _dashBoard
                    };
                }
                else
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "No record found"
                    };
                }

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetDashBoardByUserID", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);
        }


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/ChangePassword")]
        public IActionResult ChangePassword([FromBody] UserDataContract _userDataContract)
        {
            GenericResult _authenticationResult = null;
            UserDataContract _userdatacontract = new UserDataContract();

            string EncruptOldPassword = Encryptor.MD5Hash(_userDataContract.OldPassword);
            string EncruptNewPassword = Encryptor.MD5Hash(_userDataContract.NewPassword);

            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            UserLogic userlogic = new UserLogic();
            bool result = userlogic.ChangePassword(new Guid(headerUserID), EncruptOldPassword, EncruptNewPassword);

            try
            {
                _userdatacontract = GetUserCredentialByUserID(new Guid(headerUserID), new Guid());
                if (result)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Password Changed successfully",
                        Token = TokenGenerator(_userdatacontract),
                        UserData = _userdatacontract
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed",
                        Token = TokenGenerator(_userdatacontract)
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in ChangePassword", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_authenticationResult); ;

        }

        [HttpPost]
        [Route("api/account/resetpassword")]
        public IActionResult ResetPasswordByAuthenticationKey([FromBody] UserDataContract _userDataContract)
        {
            GenericResult _authenticationResult = null;
            UserDataContract _userdatacontract = new UserDataContract();

            string authenticationKey = _userDataContract.AuthenticationKey; //Encryptor.MD5Hash(_userDataContract.OldPassword);
            string EncruptNewPassword = Encryptor.MD5Hash(_userDataContract.NewPassword);

            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;


            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            UserLogic userlogic = new UserLogic();
            bool result = userlogic.ResetPasswordByAuthenticationKey(authenticationKey, EncruptNewPassword);

            try
            {
                if (result)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Password Reset successfully",

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
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in ResetPasswordByAuthenticationKey", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_authenticationResult); ;

        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/UpdateUserCredentialByUserID")]
        public IActionResult UpdateUserCredentialByUserID([FromBody] UserDataContract _userDataContract)
        {
            GenericResult _authenticationResult = null;
            UserDataContract _userdatacontract = new UserDataContract();

            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            UserLogic userlogic = new UserLogic();
            int result = userlogic.UpdateUserCredentialByUserID(_userDataContract);

            try
            {
                if (result == 1)
                {
                    _userdatacontract = GetUserCredentialByUserID(new Guid(headerUserID), new Guid());
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Changes were saved successfully.",
                        UserData = _userdatacontract
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed",
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in UpdateUserCredentialByUserID", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_authenticationResult); ;

        }

        [HttpPost]
        [Route("api/account/loginfromazure")]
        public IActionResult loginfromazure([FromBody] string email)
        {

            GenericResult _authenticationResult = null;
            UserDataContract _userdatacontract = new UserDataContract();
            UserLogic userlogic = new UserLogic();

            if (email != "undefined")
            {
                _userdatacontract = userlogic.getUserpwdfromemail(email);

                try
                {
                    if (_userdatacontract.Login != null && _userdatacontract.Password != null)
                    {
                        _userdatacontract = userlogic.ValidateUser(_userdatacontract.Login, _userdatacontract.Password);//_userDataContract.Password
                        if (_userdatacontract != null)
                        {
                            _authenticationResult = new GenericResult()
                            {
                                Succeeded = true,
                                Message = "Authentication succeeded",
                                Token = TokenGenerator(_userdatacontract),
                                UserData = _userdatacontract
                            };
                        }
                        else
                        {
                            _authenticationResult = new GenericResult()
                            {
                                Succeeded = false,
                                Message = "Authentication failed:User is not authorized in the application."

                            };
                        }
                    }
                    else
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = "Authentication failed:User is not authorized in the application."

                        };
                    }
                }
                catch (Exception ex)
                {
                    LoggerLogic Log = new LoggerLogic();
                    Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in loginfromazure", "", "", ex.TargetSite.Name.ToString(), "", ex);

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = ex.Message
                    };
                }

            }
            else
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                };
            }

            return Ok(_authenticationResult);


        }

        public IEnumerable<string> Get()
        {

            return new string[] { "value1", "value2" };
        }

        [HttpPost]
        [Route("api/account/getuserdefaultsettingbyuserid")]
        public IActionResult GetUserDefaultSettingByUserID([FromBody] UserDefaultSettingDataContract _userdefaultsettingdc)
        {
            GenericResult _genericResult = null;

            UserLogic _userlogic = new UserLogic();
            List<UserDefaultSettingDataContract> _UserDefSetting = new List<UserDefaultSettingDataContract>();


            _UserDefSetting = _userlogic.GetUserDefaultSettingByUserID(new Guid(_userdefaultsettingdc.UserID.ToString()));


            try
            {
                if (_UserDefSetting != null)
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "record found",
                        UserDefaultSetting = _UserDefSetting
                    };
                }
                else
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "No record found"
                    };
                }

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetUserDefaultSettingByUserID", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);

        }


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/insertupdateuuserdefaultsetting")]
        public IActionResult InsertUpdateUserDefaultSetting([FromBody] List<UserDefaultSettingDataContract> _userdefaultsettingdclist)
        {
            GenericResult _authenticationResult = null;

            UserLogic userlogic = new UserLogic();
            bool result = true;
            foreach (UserDefaultSettingDataContract _userdefaultsettingdc in _userdefaultsettingdclist)
            {
                result = userlogic.InsertUpdateUserDefaultSetting(_userdefaultsettingdc.UserID, _userdefaultsettingdc.TypeText, _userdefaultsettingdc.Value);
            }
            try
            {
                if (result)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "updated successfully",

                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Updation failed",

                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in InsertUpdateUserDefaultSetting", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }



            return Ok(_authenticationResult); ;

        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/GetAllUsers")]
        public async Task<IActionResult> GetAllUsersAsync()
        {
            GenericResult _genericResult = null;

            UserLogic _userlogic = new UserLogic();
            List<UserDataContract> _userdatacontractlst = new List<UserDataContract>();


            _userdatacontractlst = _userlogic.GetAllUsers();

           // var adduserinAzureAD=await AddUserInAzureAD();  one time for add use in azureAD
            try
            {
                if (_userdatacontractlst != null && _userdatacontractlst.Count > 0)
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "record found",
                        UserList = _userdatacontractlst
                    };
                }
                else
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "No record found",
                        UserList = null
                    };
                }

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetAllUsers", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);

        }


        //[HttpPost]
        //[Services.Controllers.IsAuthenticate]
        //[Route("api/account/AddUpdateUserAzureAD")]
        ////  public IActionResult AddUpdateUserAD([FromBody] List<UserDataContract> _userDataContractlst)
        //public async Task<IActionResult> AddUpdateUserAD(GraphServiceClient graphClient, List<UserDataContract> _userDataContractlst)
        //{

        //    GenericResult _authenticationResult = null;
        //    try
        //    {
        //     //  GraphServiceClient graphClient = new GraphServiceClient(authProvider);



        //        User newUser = new User();
        //        // EmailNotification emailnotify = new EmailNotification();
        //        UserLogic userlogic = new UserLogic();

        //        foreach (UserDataContract user in _userDataContractlst)
        //        {
        //            newUser.AccountEnabled = true;
        //            newUser.DisplayName = user.FirstName + " " + user.LastName;
        //            newUser.GivenName = user.FirstName + " " + user.LastName;
        //            newUser.JobTitle = "Developer";
        //            newUser.Mail = user.Email;
        //            newUser.OfficeLocation = "Indore";
        //            newUser.PreferredLanguage = "English";
        //            newUser.Surname = user.LastName;
        //        }

        //        await graphClient.Users
        //                 .Request()
        //                             .AddAsync(newUser);

        //        //foreach (UserDataContract user in _userDataContractlst)
        //        //{
        //        //    user.FirstName = CommonHelper.ToTitleCase(user.FirstName);
        //        //    user.LastName = CommonHelper.ToTitleCase(Convert.ToString(user.LastName));
        //        //    if (user.UserID == new Guid("00000000-0000-0000-0000-000000000000"))
        //        //    {
        //        //        user.NewPassword = Encryptor.GenerateStrongPassword();
        //        //        user.Password = Encryptor.MD5Hash(user.NewPassword);
        //        //    }
        //        //}
        //        int result = userlogic.AddUpdateUser(_userDataContractlst);
        //        var newuserlist = _userDataContractlst.Where(x => x.UserID == new Guid("00000000-0000-0000-0000-000000000000")).ToList();


        //    }
        //    catch (Exception ex)
        //    {
        //        LoggerLogic Log = new LoggerLogic();
        //        Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in AddUpdateUser", "", "", ex.TargetSite.Name.ToString(), "", ex);

        //        _authenticationResult = new GenericResult()
        //        {
        //            Succeeded = false,
        //            Message = ex.Message
        //        };
        //    }

        //    return Ok(_authenticationResult); ;

        //}

        public async Task<Boolean> AddUpdateUserAD(UserDataContract _userDataContract) //  List<UserDataContract> _userDataContractlst // GraphServiceClient graphClient,
        {
            GetConfigSetting();

            ConfidentialClientApplicationOptions _applicationOptions = new ConfidentialClientApplicationOptions
            {
                ClientId = Sectionroot.GetSection("AzureAD_ClientId").Value,
                TenantId = Sectionroot.GetSection("AzureAD_TenantId").Value,
                ClientSecret = Sectionroot.GetSection("AzureAD_ClientSecret").Value
            };

            // Build a client application.
            IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                             .CreateWithApplicationOptions(_applicationOptions)
                         .Build();


            // Create an authentication provider by passing in a client application and graph scopes.
            ClientCredentialProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);


            // Create a new instance of GraphServiceClient with the authentication provider.
            GraphServiceClient graphClient = new GraphServiceClient(authProvider);

           
            try
            {

                var user = (await graphClient.Users
                 .Request()
                 .Filter($"mail eq '{_userDataContract.Email}'")
                 .GetAsync()).FirstOrDefault();
                //Console.WriteLine($"{_userDataContract.Email} {user != null} {user?.Id} [{user?.DisplayName}] [{user?.Mail}]");
               
                if (user == null)
                {
                    {

                        var invite = await graphClient.Invitations
                                         .Request().AddAsync(new Invitation
                                         {
                                             InvitedUserDisplayName = _userDataContract.FirstName + " " + _userDataContract.LastName,
                                             InvitedUserEmailAddress = _userDataContract.Email,
                                             SendInvitationMessage = true,
                                             InviteRedirectUrl = Sectionroot.GetSection("AzureAD_RedirectUrl").Value,
                                             InvitedUserMessageInfo = true ? new InvitedUserMessageInfo
                                             {
                                                 CustomizedMessageBody = "We are inviting you to access M61 Systems.",
                                             } : null
                                             
                                         });


                        
                    }
                   

                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in AddUpdateUser in AzureAD", "", "", ex.TargetSite.Name.ToString(), "", ex);

            }

            return true;

        }



        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/AddUpdateUser")]
        public async Task<IActionResult> AddUpdateUserAsync([FromBody] List<UserDataContract> _userDataContractlst)
        {
            GenericResult _authenticationResult = null;

            // EmailNotification emailnotify = new EmailNotification();
            UserLogic userlogic = new UserLogic();

            foreach (UserDataContract user in _userDataContractlst)
            {
                user.FirstName = CommonHelper.ToTitleCase(user.FirstName);
                user.LastName = CommonHelper.ToTitleCase(Convert.ToString(user.LastName));
                if (user.UserID == new Guid("00000000-0000-0000-0000-000000000000"))
                {
                    user.NewPassword = Encryptor.GenerateStrongPassword();
                    user.Password = Encryptor.MD5Hash(user.NewPassword);
                }
            }
            int result = userlogic.AddUpdateUser(_userDataContractlst);
            var newuserlist = _userDataContractlst.Where(x => x.UserID == new Guid("00000000-0000-0000-0000-000000000000")).ToList();
            try
            {
                if (result == 1)
                {
                    foreach (UserDataContract user in _userDataContractlst)
                    {
                        //Add User In Azure Active Directory;
                        await AddUpdateUserAD(user);
                    }
                    //For Send Email to created User
                    if (newuserlist != null)
                    {
                        foreach (UserDataContract user in newuserlist)
                        {
                            _iEmailNotification.SendNewUserNotification(user.FirstName, user.SuperAdminName, user.Login, user.Email, user.NewPassword);

                        }
                    }
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Changes were saved successfully."

                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication failed",
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in AddUpdateUser", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_authenticationResult); ;

        }


        /// <summary>
        ///  only for one time for add user in azure AD
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/account/AddUserInAzureAD")]
        public async Task<IActionResult> AddUserInAzureAD()
        {
            GenericResult _authenticationResult = null;

            // EmailNotification emailnotify = new EmailNotification();
            UserLogic userlogic = new UserLogic();
            List<UserDataContract> _userdatacontractlst = new List<UserDataContract>();


            _userdatacontractlst = userlogic.GetUsersforAzureAD();
          
            try
            {
                if (_userdatacontractlst.Count>0)
                {
                    foreach (UserDataContract user in _userdatacontractlst)
                    {
                        //Add User In Azure Active Directory;
                       
                            await AddUpdateUserAD(user);
                       
                    }
                    //For Send Email to created User
                    //if (newuserlist != null)
                    //{
                    //    foreach (UserDataContract user in newuserlist)
                    //    {
                    //        _iEmailNotification.SendNewUserNotification(user.FirstName, user.SuperAdminName, user.Login, user.Email, user.NewPassword);

                    //    }
                    //}
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Email Send Successfully."

                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication failed",
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in AddUpdateUser", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_authenticationResult); ;

        }


        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/getallLookup")]
        public IActionResult GetAllLookup(string ID)
        {
            GenericResult _authenticationResult = null;
            List<LookupDataContract> lstlookupDC = new List<LookupDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;

            LookupLogic lookupLogic = new LookupLogic();
            lstlookupDC = lookupLogic.GetAllLookups(ID);
            lstlookupDC = lstlookupDC.OrderBy(x => x.SortOrder).ToList();

            try
            {
                if (lstlookupDC != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstLookups = lstlookupDC
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
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetAllLookup", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/account/getUsersByRoleName")]
        public IActionResult GetUsersByRoleName(string ID)
        {
            GenericResult _genericResult = null;

            UserLogic _userlogic = new UserLogic();
            List<UserDataContract> _userdatacontractlst = new List<UserDataContract>();
            _userdatacontractlst = _userlogic.GetUsersByRoleName(ID);

            try
            {
                if (_userdatacontractlst != null && _userdatacontractlst.Count > 0)
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "record found",
                        UserList = _userdatacontractlst
                    };
                }
                else
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "No record found",
                        UserList = null
                    };
                }

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetUsersByRoleName", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);
        }


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/getappconfigbykey")]
        public IActionResult GetAppConfigByKey([FromBody] string key)
        {
            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;

            //Guid headerUserID = new Guid();
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }


            CRES.BusinessLogic.AppConfigLogic appl = new CRES.BusinessLogic.AppConfigLogic();
            //to get user 
            List<AppConfigDataContract> permissionlist;
            if (headerUserID == "")
                permissionlist = appl.GetAppConfigByKey(null, key);
            else
                permissionlist = appl.GetAppConfigByKey(new Guid(headerUserID), key);


            try
            {
                if (permissionlist != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        AllowBasicLogin = permissionlist[0]
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

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/getallappconfig")]
        public IActionResult GetAllAppConfig()
        {
            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;

            //Guid headerUserID = new Guid();
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            AppConfigLogic appl = new AppConfigLogic();
            //to get user 
            List<AppConfigDataContract> SettingKeyslist;
            if (headerUserID == "")
                SettingKeyslist = appl.GetAllAppConfig(null);
            else
                SettingKeyslist = appl.GetAllAppConfig(new Guid(headerUserID));


            try
            {
                if (SettingKeyslist != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        AllSettingKeys = SettingKeyslist
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
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetAllAppConfig", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/account/UpdateAppConfigByKey")]
        public IActionResult UpdateAppConfigByKey([FromBody] AppConfigDataContract _appconfigdatacontract)
        {
            GenericResult _authenticationResult = null;
            // AppConfigDataContract _appconfigdatacontract = new AppConfigDataContract();

            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            AppConfigLogic appconfiglogic = new AppConfigLogic();
            int result = appconfiglogic.UpdateAppConfigByKey(new Guid(headerUserID), _appconfigdatacontract);

            try
            {
                if (result == 1)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Changes were saved successfully."
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed",
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in UpdateAppConfigByKey", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_authenticationResult); ;

        }

        [HttpGet]
        [Route("api/account/GetHolidayList")]
        public IActionResult GetHolidayList()
        {
            GenericResult _genericResult = null;
            NoteLogic _NoteLogic = new NoteLogic();
            List<HolidayListDataContract> _HolidayList = new List<HolidayListDataContract>();
            _HolidayList = _NoteLogic.GetHolidayList();

            try
            {
                if (_HolidayList != null && _HolidayList.Count > 0)
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "record found",
                        HolidayList = _HolidayList
                    };
                }
                else
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "No record found",
                        HolidayList = null
                    };
                }

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetHolidayList", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);

        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/ForceLogout")]
        public IActionResult ForceLogout(string ID)
        {
            GenericResult _genericResult = null;
            UserLogic _userlogic = new UserLogic();

            Guid? _id = null;
            if (ID != "null")
                _id = new Guid(ID);
            try
            {
                if (_userlogic.ForceLogout(_id))
                {
                    /* Getting updated token for current user */
                    IEnumerable<string> headerValues;
                    var headerUserID = string.Empty;
                    //if (Request.Headers.TryGetValues("TokenUId", out headerValues))
                    //{
                    //    headerUserID = headerValues.FirstOrDefault();
                    //}


                    if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                    {
                        headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
                    }


                    UserDataContract userDC = new UserDataContract();
                    userDC = GetUserCredentialByUserID(new Guid(headerUserID), new Guid());

                    _genericResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "User(s) forced logout successfully",
                        Token = TokenGenerator(userDC),
                        UserData = userDC
                    };
                }
                else
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Error while force logout"
                    };
                }

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in ForceLogout", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);
        }

        //------------------------user delegation code-----------------------------------
        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/insertuserdelegateconfig")]
        public IActionResult InsertUserDelegateConfig([FromBody] UserDelegationConfigDataContract _udd)
        {
            GenericResult _authenticationResult = null;
            var Startdate = Convert.ToDateTime(_udd.Startdate);
            var Enddate = Convert.ToDateTime(_udd.Enddate);
            var newstartdate = Startdate.ToString("MM/dd/yyyy", CultureInfo.CreateSpecificCulture("en-US"));
            var newenddate = Enddate.ToString("MM/dd/yyyy", CultureInfo.CreateSpecificCulture("en-US"));

            UserDelegateLogic _udl = new UserDelegateLogic();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            //if (Request.Headers.TryGetValues("TokenUId", out headerValues))
            //{
            //    headerUserID = headerValues.FirstOrDefault();
            //}


            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            _udd.UserID = new Guid(headerUserID);
            string res = _udl.InsertDelegateConfiguration(_udd);
            UserDataContract _userdc = new UserDataContract();

            _userdc = GetUserCredentialByUserID(new Guid(_udd.DelegatedUserID.ToString()), new Guid());
            var receiveruserName = _userdc.FirstName + ' ' + _userdc.LastName;

            // if (newstartdate == newCurrentdate)
            //  {
            // EmailNotification notification = new EmailNotification();
            _iEmailNotification.SendDelegateUserNotification(_userdc.Email, receiveruserName, newstartdate, newenddate, _udd.username);
            //    }
            try
            {
                if (res == "TRUE")
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded"

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
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in InsertUserDelegateConfig", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/account/getallactivedelegateduser")]
        public IActionResult GetAllActiveDelegatedUser()
        {
            GenericResult _authenticationResult = null;
            List<UserDelegationConfigDataContract> AllActiveDelegatedUser = new List<UserDelegationConfigDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            UserDelegateLogic _udl = new UserDelegateLogic();

            Guid UserID = new Guid(headerUserID);
            AllActiveDelegatedUser = _udl.GetAllActiveDelegatedUser(UserID);

            try
            {
                if (AllActiveDelegatedUser != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "succeeded",
                        AllActiveDelegatedUser = AllActiveDelegatedUser
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
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetAllActiveDelegatedUser", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/account/getuserstoimpersonate")]
        public IActionResult GetUsersToImpersonate(string ID)
        {
            GenericResult _authenticationResult = null;

            List<UserDelegationConfigDataContract> udlist = new List<UserDelegationConfigDataContract>();
            UserDelegateLogic udl = new UserDelegateLogic();

            udlist = udl.GetUsersToImpersonate(new Guid(ID));

            try
            {
                if (udlist != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        UsersToImpersonateList = udlist
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
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetUsersToImpersonate", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/account/insertdelegatehistory")]
        public IActionResult InsertDelegateHistory([FromBody] UserDelegationConfigDataContract _udd)
        {
            GenericResult _authenticationResult = null;
            UserDelegateLogic _udl = new UserDelegateLogic();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            string res = _udl.InsertDelegateHistory(_udd);
            try
            {
                if (res == "TRUE")
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded"

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
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in InsertDelegateHistory", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/account/impersonateUserByUserID")]
        public IActionResult ImpersonateUserByUserID([FromBody] UserDelegationConfigDataContract _udd)
        {

            GenericResult _authenticationResult = null;
            UserDataContract _userdatacontract = new UserDataContract();
            UserLogic userlogic = new UserLogic();

            if (_udd.UserID != null)
            {
                _userdatacontract = GetUserCredentialByUserID(_udd.UserID.Value, new Guid());
                try
                {
                    if (_userdatacontract != null)
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "Authentication succeeded",
                            Token = TokenGenerator(_userdatacontract),
                            UserData = _userdatacontract
                        };

                        /* Logging into database */
                        InsertDelegateHistory(_udd);
                    }
                    else
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = "Authentication failed:User is not authorized in the application."

                        };
                    }

                }
                catch (Exception ex)
                {

                    LoggerLogic Log = new LoggerLogic();
                    Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in ImpersonateUserByUserID", "", "", ex.TargetSite.Name.ToString(), "", ex);

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = ex.Message
                    };
                }
            }
            else
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    //Message = "Authentication failed"
                    //  Message = "Username and password don't match to our records."
                };
            }

            return Ok(_authenticationResult);
        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/RevokeUserDelegateConfigByUserDelegateConfigID")]
        public IActionResult RevokeUserDelegateConfigByUserDelegateConfigID(string ID)
        {
            GenericResult _authenticationResult = null;
            UserDelegateLogic udl = new UserDelegateLogic();

            try
            {
                if (udl.RevokeUserDelegateConfigByUserDelegateConfigID(new Guid(ID)))
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Revoke User Delegate Config succeeded"
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Revoke User Delegate Config failed"
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in RevokeUserDelegateConfigByUserDelegateConfigID", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/account/UpdateUserByUserID")]
        public IActionResult UpdateUserByUserID([FromBody] UserDataContract _userDataContract)
        {
            GenericResult _authenticationResult = null;
            //  EmailNotification emailnotify = new EmailNotification();
            UserLogic userlogic = new UserLogic();

            _userDataContract.FirstName = CommonHelper.ToTitleCase(_userDataContract.FirstName);
            _userDataContract.LastName = CommonHelper.ToTitleCase(Convert.ToString(_userDataContract.LastName));
            if (_userDataContract.UserID == new Guid("00000000-0000-0000-0000-000000000000"))
            {
                _userDataContract.NewPassword = Encryptor.GenerateStrongPassword();
                _userDataContract.Password = Encryptor.MD5Hash(_userDataContract.NewPassword);
            }

            int result = userlogic.UpdateUserByUserID(_userDataContract);

            try
            {
                if (result == 1)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Changes were saved successfully."

                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed",
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in UpdateUserByUserID", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_authenticationResult); ;
        }



        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        //[Services.Controllers.DeflateCompression]
        [Route("api/account/GetAllTimeZoneData")]
        public IActionResult GetAllTimeZoneData([FromBody] AppTimeZoneDataContract _searchDC, int? pageIndex, int? pageSize)
        {
            GenericResult _genericResult = null;

            UserLogic _userlogic = new UserLogic();
            List<AppTimeZoneDataContract> _lstAppTimeZone = new List<AppTimeZoneDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();

            //if (Request.Headers.TryGetValues("TokenUId", out headerValues))
            //{
            //    headerUserID = new Guid(headerValues.FirstOrDefault());
            //}

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            _lstAppTimeZone = _userlogic.GetAllTimeZoneSearchData(headerUserID, _searchDC.Valuekey);

            try
            {
                if (_lstAppTimeZone != null && _lstAppTimeZone.Count > 0)
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "record found",
                        lstAppTimeZone = _lstAppTimeZone
                    };
                }
                else
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "No record found",
                        lstAppTimeZone = null
                    };
                }

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetAllTimeZoneData", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);

        }

        [HttpGet]
        [Route("api/account/getwfapprover")]
        public IActionResult GetWFApprover()
        {
            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            UserPermissionLogic upl = new UserPermissionLogic();
            //to get user 
            var userdata = upl.GetWFApprover(headerUserID);

            try
            {
                if (userdata != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        UserApproverList = userdata
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
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetWFApprover", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/account/InsertUpdateWFApprover")]
        public IActionResult InsertUpdateWFApprover([FromBody] List<UserDataContract> UserDC)
        {
            GenericResult _genericResult = null;
            List<UserDataContract> lstEmailNotificationFile = new List<UserDataContract>();
            IEnumerable<string> headerValues;

            UserPermissionLogic upl = new UserPermissionLogic();
            upl.InsertUpdateWFApprover(UserDC);
            try

            {
                if (UserDC.Count > 0)
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "record found",
                    };
                }
                else
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "no record found",
                    };

                }
            }
            catch (Exception ex)
            {

                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in InsertUpdateWFApprover", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/SendEmailWFApprover")]
        public IActionResult SendEmailWFApprover([FromBody] UserDataContract wfuserdetails)
        {
            GenericResult _genericResult = null;
            // EmailNotification notification = new EmailNotification();
            _iEmailNotification.SendUserWorkflowApproverChangeNotification(wfuserdetails.Email, wfuserdetails.ModuleId, wfuserdetails.ModuleName, wfuserdetails.FirstName, wfuserdetails.LastName, wfuserdetails.envName);
            try

            {
                _genericResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Email Send",
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in SendEmailWFApprover", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);
        }


        [HttpGet]
        [Route("api/account/GetAllTimeZone")]
        public IActionResult GetAllTimeZone()
        {
            GenericResult _genericResult = null;

            UserLogic _userlogic = new UserLogic();
            List<AppTimeZoneDataContract> _lstAppTimeZone = new List<AppTimeZoneDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            _lstAppTimeZone = _userlogic.GetAllTimeZoneData(headerUserID);

            try
            {
                if (_lstAppTimeZone != null && _lstAppTimeZone.Count > 0)
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "record found",
                        lstAppTimeZone = _lstAppTimeZone
                    };
                }
                else
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "No record found",
                        lstAppTimeZone = null
                    };
                }

            }
            catch (Exception ex)
            {

                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetAllTimeZone", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);

        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/DeleteWFApproverByEmailNotificationId")]
        public IActionResult DeleteWFApproverByEmailNotificationID([FromBody] UserDataContract wfuser)
        {
            GenericResult _genericResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            UserPermissionLogic upl = new UserPermissionLogic();
            upl.DeleteWFApproverByEmailNotificationID(headerUserID, wfuser.EmailNotificationID, wfuser.Email);
            try

            {
                _genericResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Deleted Successfully!",
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in DeleteWFApproverByEmailNotificationID", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);
        }


        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/getallservicer")]
        public IActionResult GetAllServicer()
        {
            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            //if (Request.Headers.TryGetValues("TokenUId", out headerValues))
            //{
            //    headerUserID = new Guid(headerValues.FirstOrDefault());
            //}
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            UserPermissionLogic upl = new UserPermissionLogic();
            //to get user 
            var servicerdata = upl.GetAllServicer(headerUserID);

            try
            {
                if (servicerdata != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        ServicerList = servicerdata
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
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetAllServicer", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/account/UpdateServicerByServicerID")]
        public IActionResult UpdateServicerByServicerID([FromBody] ServicerDataContract servicerDC)
        {
            GenericResult _genericResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            UserPermissionLogic upl = new UserPermissionLogic();
            bool result = upl.UpdateServicerByServicerId(headerUserID, servicerDC.ServicerMasterID, servicerDC.ServicerDropDate, servicerDC.RepaymentDropDate);
            try
            {
                if (result == true)
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Updated Successfully!",
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in UpdateServicerByServicerID", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);
        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/impersonateusercount")]
        public IActionResult ImpersonateUserCount()
        {
            GenericResult _authenticationResult = null;
            UserDelegateLogic _udl = new UserDelegateLogic();
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            //if (Request.Headers.TryGetValues("TokenUId", out headerValues))
            //{
            //    headerUserID = headerValues.FirstOrDefault();
            //}
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            var res = _udl.ImpersonateUserCount(headerUserID);

            try
            {
                if (res > 0)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = res

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
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in ImpersonateUserCount", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/account/getusercredentialbyUserID")]
        public IActionResult GetUserInfobyUserid()
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            UserLogic _ul = new UserLogic();
            UserDataContract res = _ul.GetUserCredentialByUserID(headerUserID, new Guid());

            try
            {
                if (res != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        _userinfo = res

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
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetUserInfobyUserid", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/account/gettimezonebyuserid")]
        public IActionResult GetTimeZoneByUserID()
        {
            GenericResult _authenticationResult = null;

            var headerUserID = new Guid();
            var headerdelegateduserid = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            UserLogic ul = new UserLogic();

            DataTable dt = ul.GetTimeZoneyUserID(headerUserID, headerdelegateduserid);

            try
            {
                if (dt.Rows.Count != 0)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        dt = dt
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
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetTimeZoneByUserID", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/account/getAlltransactionTypes")]
        public IActionResult GetAllTransactionTypes()
        {
            GenericResult _authenticationResult = null;
            UserPermissionLogic _udl = new UserPermissionLogic();
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            DataTable dt = _udl.GetAllTransactionTypes(headerUserID);


            try
            {
                if (dt.Rows.Count > 0)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        dt = dt

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
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetAllTransactionTypes", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/account/saveTransactionTypes")]
        public IActionResult SaveTransactionTypes([FromBody] DataTable dt)
        {
            GenericResult _genericResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            UserPermissionLogic upl = new UserPermissionLogic();
            upl.InsertUpdateTransactionTypes(dt, headerUserID);
            try
            {
                _genericResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Save/Updated Successfully!",
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in SaveTransactionTypes", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/deleteTransactiontypes")]
        public IActionResult DeleteTransactionTypes([FromBody] int? transactionTypeID)
        {
            GenericResult _genericResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            UserPermissionLogic upl = new UserPermissionLogic();
            upl.deleteTransactioType(transactionTypeID, headerUserID);
            try
            {
                _genericResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Deleted Successfully!",
                };
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in DeleteTransactionTypes", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/account/UpdateIPAddressByUserID")]
        public IActionResult UpdateIPAddressByUserID([FromBody] UserDataContract _userDataContract)
        {
            GenericResult _authenticationResult = null;
            //  EmailNotification emailnotify = new EmailNotification();
            UserLogic userlogic = new UserLogic();

          
            //if (_userDataContract.UserID == new Guid("00000000-0000-0000-0000-000000000000"))
            //{
            //    _userDataContract.NewPassword = Encryptor.GenerateStrongPassword();
            //    _userDataContract.Password = Encryptor.MD5Hash(_userDataContract.NewPassword);
            //}

            int result = userlogic.UpdateIPAddressByUserID(_userDataContract);

            try
            {
                if (result == 1)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Changes were saved successfully."

                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed",
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in UpdateUserByUserID", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_authenticationResult); ;
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/account/CheckDuplicateIPAddress")]
        public IActionResult CheckDuplicateIPAddress([FromBody] UserDataContract _userDataContract)
        {
            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            UserLogic userlogic = new UserLogic();
            bool res = userlogic.CheckDuplicateIPAddress(_userDataContract.UserID, _userDataContract.IpAddress);
            try
            {
                if (res == true)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Duplicate",
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "NoteDuplicate"
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
        [Route("api/account/GetUsersInfoByRoleNameForDropDown")]
        public IActionResult GetAssetMangerList(string ID)
        {
            GenericResult _genericResult = null;

            UserLogic _userlogic = new UserLogic();
            List<UserDataContract> _userdatacontractlst = new List<UserDataContract>();
            _userdatacontractlst = _userlogic.GetUsersInfoByRoleNameForDropDown(ID);

            try
            {
                if (_userdatacontractlst != null && _userdatacontractlst.Count > 0)
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "record found",
                        UserList = _userdatacontractlst
                    };
                }
                else
                {
                    _genericResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "No record found",
                        UserList = null
                    };
                }

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetUsersByRoleName", "", "", ex.TargetSite.Name.ToString(), "", ex);

                _genericResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_genericResult);
        }

      

      
      
       
        
    }

}

