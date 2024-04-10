using CRES.BusinessLogic;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using CRES.DataContract;
using System.Collections.Generic;
using System;
using System.Linq;
using CRES.Utilities;
using CRES.DataContract.WorkFlow;
using System.Data;
using CRES.NoteCalculator;

namespace CRES.ServiceMVC.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]

    public class JournalLedgerController : ControllerBase
    {
        Microsoft.Extensions.Configuration.IConfigurationSection Sectionroot = null;

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
        private IHostingEnvironment _env;

        private readonly IEmailNotification _iEmailNotification;

        public JournalLedgerController(IEmailNotification iemailNotification, IHostingEnvironment env)
        {
            _iEmailNotification = iemailNotification;
            _env = env;
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/journalLedger/InsertUpdateJournalEntry")]
        public IActionResult InsertUpdateJournalEntry([FromBody] JournalLedgerMasterDataContract jldcm)
        {
            string JournalEntryMasterGUID = "";
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            if (jldcm.JournalEntryMasterID == null)
            {
                //set it to 0 in case of new 
                jldcm.JournalEntryMasterID = 0;
            }
            if (jldcm.Comments == null)
            {
                //set it to 0 in case of new 
                jldcm.Comments = "";
            }


            foreach (var jldc in jldcm.Listjldc)
            {
                if (string.IsNullOrEmpty(jldc.CommentsDetail))
                {
                    jldc.CommentsDetail = jldcm.Comments;
                }
            }

            JournalEntryLogic _JournalLogic = new JournalEntryLogic();
            JournalEntryMasterGUID =  _JournalLogic.InsertUpdateJournalEntry(jldcm.Listjldc, headerUserID, jldcm.JournalEntryMasterID, jldcm.JournalEntryDate, jldcm.Comments);

            try
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Succeeded",
                    JournalEntryMasterGUID= JournalEntryMasterGUID,

                };
            }
            catch (Exception ex)
            {
                string message = ExceptionHelper.GetFullMessage(ex);
                Logger.Write(CRESEnums.Module.JournalEntry.ToString(), "Error in loading: " + "" + " Exception : " + message, MessageLevel.Error, headerUserID, "");
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
        [Route("api/journalLedger/GetJournalLedgerbyJournalEntryMasterGuid")]
        public IActionResult GetJournalLedgerbyJournalEntryMasterGuid([FromBody] string JournalEntryMasterGUID)
        {
            GenericResult _authenticationResult = null;
            JournalLedgerDataContract jldc = new JournalLedgerDataContract();
            List<JournalLedgerDataContract> ListjournalLedger = new List<JournalLedgerDataContract>();
            IEnumerable<string> headerValues;

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            LiabilityNoteLogic LiabilityNotelogic = new LiabilityNoteLogic();
            List<LookupDataContract> lstSearch = LiabilityNotelogic.GetAllLiabilityTypeLookup();
            List<LookupDataContract> lstLookups = LiabilityNotelogic.GetTransactionTypesLookupForJournalEntry();
            
            JournalEntryLogic _JournalLogic = new JournalEntryLogic();
            if (JournalEntryMasterGUID != null && JournalEntryMasterGUID != "00000000-0000-0000-0000-000000000000")
            {
                ListjournalLedger = _JournalLogic.GetJournalEntryByJournalEntryMasterGUID(new Guid(JournalEntryMasterGUID.ToString()));

                foreach (var item in ListjournalLedger)
                {
                    jldc.JournalEntryMasterID = item.JournalEntryMasterID;
                    jldc.JournalEntryDate = item.JournalEntryDate;
                    jldc.Comments = item.Comments;
                }
            }
            else 
            {
                JournalLedgerDataContract jd = new JournalLedgerDataContract();
                jd.TransactionAmount = 0;
                ListjournalLedger.Add(jd);
            }
            try
            {
                if (ListjournalLedger != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        journalLedger = jldc,
                        ListjournalLedger = ListjournalLedger,
                        AssetList = lstSearch,
                        lstLookups= lstLookups
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


    }
}
