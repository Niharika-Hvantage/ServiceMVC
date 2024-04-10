using CRES.DataContract;
using CRES.NoteCalculator;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using CRES.BusinessLogic;
using Microsoft.Extensions.Configuration;
using CRES.Utilities;
using System.Threading.Tasks;
using System.Collections;
using Microsoft.AspNetCore.Hosting;
using System.Reflection;
using System.util;
using static CRES.DataContract.V1CalcDataContract;

//using CRES.Utilities;

namespace CRES.ServicesNew.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class NoteController : ControllerBase
    {
        private IHostingEnvironment _env;


        //private readonly IEmailNotification _iEmailNotification;
        //public DealController(IEmailNotification iemailNotification, IHostingEnvironment env)
        //{
        //    _iEmailNotification = iemailNotification;
        //    _env = env;
        //}

        private readonly IEmailNotification _iEmailNotification;
        public NoteController(IEmailNotification iemailNotification, IHostingEnvironment env)
        {
            _iEmailNotification = iemailNotification;
            _env = env;
        }




        // GET: Note
        private NoteLogic _noteLogic = new NoteLogic();
        private TagMasterLogic _tagMasterLogic = new TagMasterLogic();

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/getnotesbydealId")]
        public IActionResult GetNotesByDealId([FromBody] NoteDataContract _noteDC, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            List<NoteDataContract> lstNotes = new List<NoteDataContract>();

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            int? totalCount;
            lstNotes = _noteLogic.GetAllNotesFromDealIds(_noteDC.DealID, headerUserID, pageSize, pageIndex, out totalCount);

            try
            {
                if (lstNotes != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        lstNotes = lstNotes
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
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in GetNotesByDealId: Note ID " + _noteDC.CRENoteID, _noteDC.NoteId.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/note/getnotesfromdealdetailbydealID")]
        public IActionResult GetNotesFromDealDetailByDealID([FromBody] NoteDataContract _noteDC, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            List<NoteUsedInDealDataContract> lstNotes = new List<NoteUsedInDealDataContract>();

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            int? totalCount;
            lstNotes = _noteLogic.GetNotesFromDealDetailByDealID(_noteDC.DealID, headerUserID, pageSize, pageIndex, out totalCount);

            if (lstNotes.Count == 0)
            {
                NoteUsedInDealDataContract note = new NoteUsedInDealDataContract();
                note.TotalCommitment = 0;
                note.Isexclude = false;
                lstNotes.Add(note);
            }
            try
            {
                if (lstNotes != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        lstNotesDeal = lstNotes
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
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in GetNotesFromDealDetailByDealID: Note ID " + _noteDC.CRENoteID, _noteDC.NoteId.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/note/addnewnote")]
        [Services.Controllers.DeflateCompression]
        public IActionResult AddNewNote([FromBody] List<NoteDataContract> _noteDC)
        {
            GenericResult _actionResult = null;
            List<NoteDataContract> _lstNotes = new List<NoteDataContract>();

            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            var createdBy = string.Empty;
            var UpdatedBy = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }


            NoteLogic _noteLogic = new NoteLogic();

            _noteDC = _noteDC.FindAll(x => x.CRENoteID != null && x.Name != null);

            string result = _noteLogic.AddNewNoteFromDealDetail(new Guid(headerUserID), _noteDC, headerUserID, headerUserID);
            if (_noteDC[0].NoteMaturityList != null)
            {
                _noteLogic.UpdateMaturityConfiguration(_noteDC, (new Guid(headerUserID)));
                if (_noteDC[0].NoteMaturityList.Rows.Count > 0)
                {
                    ValidateNoteMaturityScenariosFromDeal(_noteDC[0].maturityList, headerUserID, _noteDC);
                    _noteLogic.SaveMaturitybydeal(_noteDC[0].NoteMaturityList, (new Guid(headerUserID)));
                }
            }
            // to call for AIEntityApi
            AIDynamicEntityUpdateLogic _dynamicentity = new AIDynamicEntityUpdateLogic();
            Thread FirstThread = new Thread(() => _dynamicentity.InsertUpdateAINoteEntitiesAsync(_noteDC, headerUserID));
            FirstThread.Start();

            try
            {
                if (result != "")
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Changes were saved successfully.",
                    };
                }
                else
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Updation failed",
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred while saving Notes with : Note ID " + _noteDC[0].CRENoteID, _noteDC[0].NoteId.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_actionResult);
        }

        public void ValidateNoteMaturityScenariosFromDeal(DataTable dt, string UserID, List<NoteDataContract> _noteDC)
        {
            ExceptionsLogic _ExceptionsLogic = new ExceptionsLogic();

            string msg = "";
            string currentnoteid = "";
            string norecordformaturity = "";
            var distinctnoteid = _noteDC.Select(x => x.NoteId).Distinct();
            foreach (var dv in distinctnoteid)
            {
                currentnoteid = dv.ToString();
                List<ExceptionDataContract> exceptionlist = new List<ExceptionDataContract>();
                norecordformaturity = "";
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (currentnoteid == Convert.ToString(dr["NoteID"]))
                        {
                            norecordformaturity = "recordfound";
                            DateTime? dte = CommonHelper.ToDateTime(dr["MaturityDate"]);
                            int? MaturityType = CommonHelper.ToInt32(dr["MaturityType"]);
                            if (dte == null)
                            {
                                msg = "Maturity scenario cannot be empty";
                            }

                            if (MaturityType == 708 && dte == null)
                            {
                                msg = "initial Maturity Date cannot be empty";
                            }
                            if (msg != "")
                            {
                                ExceptionDataContract edc = new ExceptionDataContract();
                                edc.ObjectID = new Guid(Convert.ToString(dr["NoteID"]));
                                edc.ObjectTypeText = "Note";
                                edc.FieldName = "Maturity scenarios List";
                                edc.Summary = msg;
                                edc.ActionLevelText = "Critical";
                                exceptionlist.Add(edc);
                            }
                        }

                    }

                }
                else
                {
                    msg = "Maturity scenario cannot be empty";
                }
                if (norecordformaturity == "")
                {
                    ExceptionDataContract edc = new ExceptionDataContract();
                    edc.ObjectID = new Guid(Convert.ToString(currentnoteid));
                    edc.ObjectTypeText = "Note";
                    edc.FieldName = "Maturity scenarios List";
                    edc.Summary = "Maturity scenario cannot be empty";
                    edc.ActionLevelText = "Critical";
                    exceptionlist.Add(edc);
                }
                if (exceptionlist.Count > 0)
                {
                    _ExceptionsLogic.InsertExceptionsByFieldName(exceptionlist, UserID, "Maturity scenarios List");
                }
                else
                {
                    //remove old exceptions
                    _ExceptionsLogic.DeleteExceptionByobjectByFieldName(currentnoteid, "note", "Maturity scenarios List");
                }
            }
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/addupdatenoteadditionallist")]
        public IActionResult AddupdateNoteAdditinallist([FromBody] NoteAdditinalListDataContract _noteaddlistdc)
        {
            GenericResult _actionResult = null;
            IEnumerable<string> headerValues;
            string Validationmessage = string.Empty;

            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            try
            {

                #region  validate all list to have proper data

                if (_noteaddlistdc.RateSpreadScheduleList.Count > 0)
                    _noteaddlistdc.RateSpreadScheduleList = _noteaddlistdc.RateSpreadScheduleList.FindAll(y => y.Date != null).ToList();
                if (_noteaddlistdc.NotePrepayAndAdditionalFeeScheduleList != null)
                    _noteaddlistdc.NotePrepayAndAdditionalFeeScheduleList = _noteaddlistdc.NotePrepayAndAdditionalFeeScheduleList.FindAll(y => y.StartDate != null).ToList();
                if (_noteaddlistdc.NoteStrippingList != null)
                    _noteaddlistdc.NoteStrippingList = _noteaddlistdc.NoteStrippingList.FindAll(y => y.StartDate != null).ToList();
                if (_noteaddlistdc.lstFinancingFeeSchedule != null)
                    _noteaddlistdc.lstFinancingFeeSchedule = _noteaddlistdc.lstFinancingFeeSchedule.FindAll(y => y.Date != null).ToList();
                if (_noteaddlistdc.NoteFinancingScheduleList != null)
                    _noteaddlistdc.NoteFinancingScheduleList = _noteaddlistdc.NoteFinancingScheduleList.FindAll(y => y.Date != null).ToList();
                if (_noteaddlistdc.NoteDefaultScheduleList != null)
                    _noteaddlistdc.NoteDefaultScheduleList = _noteaddlistdc.NoteDefaultScheduleList.FindAll(y => y.StartDate != null).ToList();
                //  _noteaddlistdc.ListFutureFundingScheduleTab = _noteaddlistdc.ListFutureFundingScheduleTab.FindAll(y => y.Date != null).ToList();
                if (_noteaddlistdc.ListFixedAmortScheduleTab != null)
                    _noteaddlistdc.ListFixedAmortScheduleTab = _noteaddlistdc.ListFixedAmortScheduleTab.FindAll(y => y.Date != null).ToList();

                #endregion


                NoteLogic _noteLogic = new NoteLogic();
                #region validationengine

                ExceptionsLogic _ExceptionsLogic = new ExceptionsLogic();
                ValidationEngine validate = new ValidationEngine();
                NoteDataContract _cnoteobject = ConvertToNoteobject(_noteaddlistdc);

                List<FeeSchedulesConfigDataContract> ListFeeSchedulesConfig = new List<FeeSchedulesConfigDataContract>();
                ListFeeSchedulesConfig = _noteLogic.GetFeeSchedulesConfig(new Guid(headerUserID));
                _cnoteobject.ListFeeSchedulesConfiguration = ListFeeSchedulesConfig;

                List<ExceptionDataContract> edc = validate.ValidateNoteObject(_cnoteobject);

                #endregion validationengine

                int result = _noteLogic.AddUpdateNoteAdditinalList(new Guid(headerUserID), _noteaddlistdc, headerUserID, headerUserID);

                //_noteLogic.InsertUpdatedNoteRateSpreadSchedule(_noteaddlistdc.RateSpreadScheduleList, headerUserID);
                //_noteLogic.InsertUpdatedNoteFeeSchedule(_noteaddlistdc.NotePrepayAndAdditionalFeeScheduleList, headerUserID);

                if (_noteaddlistdc.deleteMarketPriceList != null)
                {
                    _noteLogic.DeleteMarketPriceByNoteID(_noteaddlistdc.deleteMarketPriceList, new Guid(headerUserID));
                }
                if (_noteaddlistdc.noteValue == "Copy")
                {
                    //Script for copy FF while NOteCopy
                    _noteLogic.CopyFundingSchedule(_noteaddlistdc.NoteId, _noteaddlistdc.NoteId, headerUserID);
                }

                DynamicSizerLogic _dynamicsizer = new DynamicSizerLogic();
                DataTable data = _dynamicsizer.GetPikPaidTransactionByCREnoteID(_cnoteobject.CRENoteID);

                Decimal? sumCashFlow = 0, sumactual = 0;
                foreach (DataRow row in data.Rows)
                {
                    if (Convert.ToString(row["Tabletype"]) == "Actual")
                    {
                        sumactual = 0;
                        sumactual = CommonHelper.ToDecimal(row["Amount"]);
                    }
                    if (Convert.ToString(row["Tabletype"]) == "CashFlow")
                    {
                        sumCashFlow = 0;
                        sumCashFlow = CommonHelper.ToDecimal(row["Amount"]);
                    }

                }
                Validationmessage = validate.ValidatePikBalance(sumactual.GetValueOrDefault(0), sumCashFlow.GetValueOrDefault(0));
                if (Validationmessage != "")
                {
                    ExceptionDataContract ed = new ExceptionDataContract();
                    ed.ObjectID = new Guid(_cnoteobject.NoteId);
                    ed.ObjectTypeText = "Note";
                    ed.FieldName = "PIK Balance";
                    ed.Summary = Validationmessage;
                    ed.ActionLevelText = "Critical";
                    edc.Add(ed);
                }
                if (edc.Count > 0)
                {
                    //update validate exception in database
                    foreach (ExceptionDataContract exc in edc)
                    {
                        exc.CreatedBy = headerUserID;
                        exc.CreatedBy = headerUserID;
                    }
                    _ExceptionsLogic.InsertExceptions(edc, headerUserID);
                    Validationmessage = "Note saved successfully with exceptions.Check Exceptions tab for more information";
                }
                else
                {
                    //remove old exceptions
                    _ExceptionsLogic.DeleteExceptionByobject(_cnoteobject.NoteId, "note");
                    Validationmessage = "Note saved successfully.";
                }
                List<ExceptionDataContract> filteredlist = edc.FindAll(x => x.ActionLevelText == "Critical").ToList();
                if (filteredlist.Count == 0)
                {
                    if (_noteaddlistdc.noteobj.EnableM61Calculations != 4)
                    {
                        InsertSingleNoteForCalculation(headerUserID.ToString(), _noteaddlistdc.noteobj.AnalysisID, _cnoteobject.NoteId);
                    }

                }
                ////=============

                if (result != 0)
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = Validationmessage,
                        TotalCount = edc.Count,
                        Allexceptionslist = edc
                    };
                }
                else
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Updation failed",
                    };
                }
            }
            catch (Exception ex)
            {

                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in AddupdateNoteAdditinallist " + _noteaddlistdc.NoteId, _noteaddlistdc.NoteId.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message.ToString()
                };
            }

            return Ok(_actionResult);
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/UpdateNoteRSSFEE")]
        public IActionResult UpdateNoteRSSFEElist([FromBody] NoteAdditinalListDataContract _noteupdatelistdc)
        {
            GenericResult _actionResult = null;
            IEnumerable<string> headerValues;
            string Validationmessage = string.Empty;

            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            if (_noteupdatelistdc.RateSpreadScheduleList != null)
            {
                foreach (var item in _noteupdatelistdc.RateSpreadScheduleList)
                {
                    if (string.IsNullOrEmpty(item.ScheduleID))
                    {
                        item.ScheduleID = "00000000-0000-0000-0000-000000000000";
                    }
                }
            }

            if (_noteupdatelistdc.NotePrepayAndAdditionalFeeScheduleList != null)
            {
                foreach (var item in _noteupdatelistdc.NotePrepayAndAdditionalFeeScheduleList)
                {
                    if (string.IsNullOrEmpty(item.ScheduleID))
                    {
                        item.ScheduleID = "00000000-0000-0000-0000-000000000000";
                    }
                }
            }

            try
            {
                if (_noteupdatelistdc.RateSpreadScheduleList != null)
                    _noteupdatelistdc.RateSpreadScheduleList = _noteupdatelistdc.RateSpreadScheduleList.FindAll(y => y.Date != null).ToList();
                if (_noteupdatelistdc.NotePrepayAndAdditionalFeeScheduleList != null)
                    _noteupdatelistdc.NotePrepayAndAdditionalFeeScheduleList = _noteupdatelistdc.NotePrepayAndAdditionalFeeScheduleList.FindAll(y => y.StartDate != null).ToList();

                NoteLogic _noteLogic = new NoteLogic();

                if (_noteupdatelistdc.RateSpreadScheduleList != null)
                {
                    _noteLogic.InsertUpdatedNoteRateSpreadSchedule(_noteupdatelistdc.RateSpreadScheduleList, headerUserID);
                }
                if (_noteupdatelistdc.NotePrepayAndAdditionalFeeScheduleList != null)
                {
                    _noteLogic.InsertUpdatedNoteFeeSchedule(_noteupdatelistdc.NotePrepayAndAdditionalFeeScheduleList, headerUserID);
                }
                _actionResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = Validationmessage
                };
            }
            catch (Exception ex)
            {

                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in UpdateNoteAdditinallist " + _noteupdatelistdc.NoteId, _noteupdatelistdc.NoteId.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message.ToString()
                };
            }

            return Ok(_actionResult);
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/copynote")]
        public IActionResult CopyNote([FromBody] NoteDataContract _note)
        {
            GenericResult _actionResult = null;

            IEnumerable<string> headerValues;
            string Validationmessage = string.Empty;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            try
            {
                NoteLogic _noteLogic = new NoteLogic();
                string newnoteid = _noteLogic.CopyNote(_note, headerUserID);

                if (newnoteid != "")
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Note Copied Successfully. ",
                        newNoteID = newnoteid
                        //TotalCount = edc.Count
                    };
                }
                else
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Updation failed",
                    };
                }
            }
            catch (Exception ex)
            {

                LoggerLogic Log = new LoggerLogic();
                string formatedstring = Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in CopyNote " + _note.CRENoteID, _note.NoteId.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                string emailextrainfo = "ParentNoteID :" + _note.NoteId + " CopyCRENoteId :" + _note.CopyCRENoteId + " CopyName :" + _note.CopyName + " ";
                Thread FirstThread = new Thread(() => _iEmailNotification.SendEmailOnExceptionFailed("CopyNote", formatedstring, ExceptionHelper.GetFullMessage(ex), headerUserID, emailextrainfo));
                FirstThread.Start();

                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message.ToString()
                };
            }

            return Ok(_actionResult);
        }
        public void ExportFutureFundingFromCRES(List<FutureFundingScheduleTab> lstFF, string headerUserID, NoteDataContract NoteDC)
        {
            NoteLogic nl = new NoteLogic();
            string BackShopStatus = "";
            string exceptionMessage = "success";

            exceptionMessage = nl.ExportFutureFundingFromNote(lstFF, headerUserID);

            if (exceptionMessage.ToLower() != "success")
            {
                if (exceptionMessage.ToLower().Contains("login failed"))
                {
                    BackShopStatus = "loginfailed";
                }
                else
                {
                    BackShopStatus = "backshopfailed";
                }

                if (BackShopStatus.ToLower().Contains("failed"))
                {
                    //   EmailNotification emailnotify = new EmailNotification();
                    _iEmailNotification.SendEmailExportFFBackShopFail(NoteDC, BackShopStatus, exceptionMessage);
                }
            }

        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/addupdatenotearchieveadditionallist")]
        public IActionResult AddupdateNoteArchieveAdditinallist([FromBody] NoteAdditinalListDataContract _noteaddlistdc)
        {
            GenericResult _actionResult = null;
            IEnumerable<string> headerValues;
            string Validationmessage = string.Empty;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            NoteLogic _noteLogic = new NoteLogic();
            int res = _noteLogic.DeleteNoteTransactionDetail(_noteaddlistdc.lstNoteServicingLog);

            int result = _noteLogic.AddUpdateNoteArchieveAdditinalList(new Guid(headerUserID), _noteaddlistdc, headerUserID, headerUserID);
            ////=============
            try
            {
                if (result != 0)
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = Validationmessage
                    };
                }
                else
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Updation failed",
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in AddupdateNoteArchieveAdditinallist " + _noteaddlistdc.NoteId, _noteaddlistdc.NoteId.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_actionResult);
        }


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/getnoteadditinallist")]
        public IActionResult GetNoteAdditinalList([FromBody] NoteDataContract _noteDC)
        {
            GenericResult _actionResult = null;
            NoteAdditinalListDataContract objNoteadd = new NoteAdditinalListDataContract();
            NoteAdditinalListDataContract NoteList_RSSFEE = new NoteAdditinalListDataContract();

            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            NoteLogic _noteLogic = new NoteLogic();
            if (_noteDC.NoteId != null)
            {

                objNoteadd = _noteLogic.GetNoteAdditinalList(new Guid(_noteDC.NoteId), new Guid(headerUserID));
                if (objNoteadd.ListFutureFundingScheduleTab == null)
                {
                    FutureFundingScheduleTab fss = new FutureFundingScheduleTab();
                    fss.Value = 0.00m;
                    fss.Applied = false;
                    List<FutureFundingScheduleTab> list = new List<FutureFundingScheduleTab>();
                    list.Add(fss);
                    objNoteadd.ListFutureFundingScheduleTab = list;
                }

                NoteList_RSSFEE = _noteLogic.GetNoteAdditional_RSSFEE(new Guid(_noteDC.NoteId), new Guid(headerUserID));

                try
                {
                    if (objNoteadd != null)
                    {

                        _actionResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "Authentication succeeded",
                            NoteAdditinalList = objNoteadd,
                            NoteList_RSSFEE = NoteList_RSSFEE
                        };
                    }
                    else
                    {
                        _actionResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = "Record not found"
                        };
                    }
                }
                catch (Exception ex)
                {
                    LoggerLogic Log = new LoggerLogic();
                    Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in GetNoteAdditinalList: " + _noteDC.CRENoteID, _noteDC.NoteId.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                    _actionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = ex.Message
                    };
                }
            }

            return Ok(_actionResult);
        }

        [HttpPost]
        [Services.Controllers.DeflateCompression]
        [Services.Controllers.IsAuthenticate]
        [Route("api/note/addnote")]
        public IActionResult Addteadditiote([FromBody] NoteDataContract _noteDC)
        {
            GenericResult _actionResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            NoteLogic _noteLogic = new NoteLogic();

            if (_noteDC.CopyCRENoteId != null)
            {
                _noteDC.CRENoteID = _noteDC.CopyCRENoteId;
                _noteDC.ClientNoteID = _noteDC.CopyCRENoteId;
                if (_noteDC.ListNoteMarketPrice.Count > 0 && _noteDC.ListNoteMarketPrice != null)
                {
                    for (var m = 0; m < _noteDC.ListNoteMarketPrice.Count; m++)
                    {
                        _noteDC.ListNoteMarketPrice[m].NoteID = _noteDC.CopyCRENoteId;
                    }
                }

            }

            if (_noteDC.CopyName != null)
            {
                _noteDC.Name = _noteDC.CopyName;
            }
            if (_noteDC.CopyDealName != null)
            {
                _noteDC.DealName = _noteDC.CopyDealName;
            }
            if (_noteDC.CopyDealID != null)
            {
                _noteDC.DealID = _noteDC.CopyDealID;
            }

            //  bool result = _noteLogic.AddNewNote(new Guid(headerUserID), _noteDC);
            List<NoteDataContract> lstNoteDC = new List<NoteDataContract> { _noteDC };
            string result = _noteLogic.AddNewNote(new Guid(headerUserID), lstNoteDC, headerUserID, headerUserID);

            if (_noteDC.ListNoteMarketPrice.Count > 0 && _noteDC.ListNoteMarketPrice != null)
            {
                _noteLogic.InsertUpdateMarketPriceByNoteID(_noteDC.ListNoteMarketPrice, new Guid(headerUserID));
            }
               TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            tagXIRRLogic.InsertUpdateTagAccountMappingXIRR(_noteDC.AccountID, _noteDC.ListSelectedXIRRTags, headerUserID);
           
            // to call for AIEntityApi
            AIDynamicEntityUpdateLogic _dynamicentity = new AIDynamicEntityUpdateLogic();
            Thread FirstThread = new Thread(() => _dynamicentity.InsertUpdateAINoteEntitiesAsync(lstNoteDC, headerUserID));
            FirstThread.Start();
            try
            {
                if (result != "00000000-0000-0000-0000-000000000000")
                {

                    _actionResult = new GenericResult()
                    {
                        newNoteID = result,
                        Succeeded = true,
                        Message = "Changes were saved successfully.",
                    };
                }
                else
                {
                    _actionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Updation failed",
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in AddNote: Note ID " + _noteDC.CRENoteID, _noteDC.NoteId.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_actionResult);
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/note/getnotebynoteId")]
        [Services.Controllers.DeflateCompression]
        public IActionResult GetNoteByNoteId([FromBody] NoteDataContract _noteDC)
        { //manish
            GenericResult _acationResult = null;
            NoteDataContract objNote = new NoteDataContract();

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            UserPermissionLogic upl = new UserPermissionLogic();
            DataTable dt = new DataTable();
            DataTable dtnotecommitment = new DataTable();
            //to get user
            List<UserPermissionDataContract> permissionlist = upl.GetuserPermissionByUserIDAndPageName(headerUserID.ToString(), "NoteDetail", _noteDC.NoteId, 182);
            if (permissionlist != null && permissionlist.Count > 0)
            {
                DateTime? LastAccountingclosedate = new DateTime();
                objNote = _noteLogic.GetNoteFromNoteId(_noteDC.NoteId, headerUserID, _noteDC.AnalysisID);

                PeriodicLogic pr = new PeriodicLogic();
                LastAccountingclosedate = pr.GetLastAccountingCloseDateByDealIDORNoteID(null, new Guid(_noteDC.NoteId));

                //LastAccountingclosedate = Convert.ToDateTime("07/30/2023");
                objNote.LastAccountingCloseDate = LastAccountingclosedate;
                if (objNote.LastAccountingCloseDate != null)
                {
                    if (objNote.LastAccountingCloseDate.Value.Year < 1970)
                    {
                        objNote.LastAccountingCloseDate = null;
                    }
                }
                TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
                ScenarioLogic _sl = new ScenarioLogic();
                objNote.DefaultScenarioParameters = _sl.GetActiveScenarioParameters(_noteDC.AnalysisID);
                objNote.DefaultscenarioID = objNote.DefaultScenarioParameters.AnalysisID;
                objNote.ListEffectiveDateCount = _noteLogic.GetScheduleEffectiveDateCount(new Guid(_noteDC.NoteId));
                objNote.ListSelectedXIRRTags = tagXIRRLogic.GetTagMasterXIRRByAccountID(objNote.AccountID);
                dt = _noteLogic.GetMarketPriceByNoteID(_noteDC.NoteId, Convert.ToString(headerUserID));
                dtnotecommitment = _noteLogic.GetNoteCommitmentsByNoteID(_noteDC.NoteId, Convert.ToString(headerUserID));
            }

            try
            {
                if (objNote.StatusCode == 200)
                {

                    _acationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        NoteData = objNote,
                        UserPermissionList = permissionlist,
                        dt = dt,
                        dtNoteCommitment = dtnotecommitment
                    };
                }
                else
                {
                    _acationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Not Exists",
                        StatusCode = 404
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in GetNotesByDealId: Note ID " + _noteDC.CRENoteID, _noteDC.NoteId.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _acationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_acationResult);
        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/getallLookup")]
        public IActionResult GetAllLookup()
        {
            string getAllLookup = "2,5,6,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,28,29,32,33,39,43,44,47,1,50,62,65,66,71,72,73,74,78,79,95,99,110,136,137,141";
            GenericResult _authenticationResult = null;
            List<LookupDataContract> lstlookupDC = new List<LookupDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            LookupLogic lookupLogic = new LookupLogic();
            lstlookupDC = lookupLogic.GetAllLookups(getAllLookup);
            lstlookupDC = lstlookupDC.OrderBy(x => x.SortOrder).ToList();

            try
            {
                if (lstlookupDC != null)
                {
                    // Logger.Write("Note lookup loaded successfully", MessageLevel.Info);
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
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in GetAllLookup ", "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

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
        [Services.Controllers.IsAuthenticate]
        [Route("api/note/getnotecalculatordatabynoteId")]
        public IActionResult GetNoteCalculatorDataByNoteId([FromBody] NoteDataContract _noteDC, int? pageIndex, int? pageSize)
        {
            IActionResult _result = null;
            GenericResult _authenticationResult = null;
            NoteDataContract _noteCalculatorDC = new NoteDataContract();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            try
            {
                bool flag = false;
                int? totalCount = 0;

                CalculationManagerLogic calculationlogic = new CalculationManagerLogic();
                // check for dependents
                PayruleSetupLogic _PayruleSetupLogic = new PayruleSetupLogic();
                List<PayruleSetupDataContract> notedependtslist = _PayruleSetupLogic.GetNoteDependentsByNoteID(_noteDC.NoteId);

                //  calculationlogic.UpdateCalculationStatusandTime(_cmDC.CalculationRequestID, _cmDC.NoteId, "Failed", "EndTime", ex.Message);

                if (notedependtslist.Count > 0)
                {
                    List<CalculationManagerDataContract> listnotes = new List<CalculationManagerDataContract>();
                    foreach (PayruleSetupDataContract pay in notedependtslist)
                    {
                        CalculationManagerDataContract cdc = new CalculationManagerDataContract();
                        cdc.StatusText = "Dependents";
                        cdc.UserName = headerUserID.ToString();
                        cdc.ApplicationText = "";
                        cdc.PriorityText = "Real Time";
                        cdc.NoteId = pay.StripTransferTo;
                        listnotes.Add(cdc);
                    }
                    calculationlogic.QueueNotesForCalculation(listnotes, headerUserID.ToString());
                }

                NoteLogic nl = new NoteLogic();
                _noteCalculatorDC = nl.GetNoteAllDataForCalculatorByNoteId(_noteDC.NoteId, null, _noteDC.AnalysisID, null, null);

                CalculationMaster cm = new CalculationMaster();
                NoteDataContract objNote = cm.StartCalculation(_noteCalculatorDC);

                List<PIKDistributionsDataContract> ListPIkDis = new List<PIKDistributionsDataContract>();
                if (objNote.NotePIKScheduleList != null)
                {
                    if (objNote.NotePIKScheduleList.Count > 0)
                    {
                        foreach (PIKSchedule pik in objNote.NotePIKScheduleList)
                        {
                            foreach (PIKInterestTab pikc in objNote.ListPIKInterestTab)
                            {
                                if (pikc.TotalPIKInteresttobeTransferredtoRelatedNoteforthePeriod != null && pikc.TotalPIKInteresttobeTransferredtoRelatedNoteforthePeriod != 0)
                                {
                                    PIKDistributionsDataContract pikdc = new PIKDistributionsDataContract();
                                    pikdc.SourceNoteID = pik.NoteID;
                                    pikdc.ReceiverNoteID = pik.TargateNoteID;
                                    pikdc.Amount = pikc.TotalPIKInteresttobeTransferredtoRelatedNoteforthePeriod;
                                    pikdc.TransactionDate = pikc.Date;
                                    ListPIkDis.Add(pikdc);
                                }
                            }

                        }

                    }
                }

                if (objNote != null && objNote.CalculatorExceptionMessage == "Succeed")
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Calculation succeeded",
                        lstNotePeriodicOutputsDataContract = objNote.ListNotePeriodicOutputs,
                        ListTransaction = objNote.ListTransaction,
                        ListCashflowTransactionEntry = objNote.ListCashflowTransactionEntry,
                        ListNotePeriodicOutput_Daily = objNote.ListNotePeriodicOutput_Daily,
                        ListNotePeriodicOutput_PVAndGaap = objNote.ListNotePeriodicOutput_PVAndGaap,
                        ListNotePeriodicOutput_SpreadAndLibor = objNote.ListNotePeriodicOutput_SpreadAndLibor,
                        ListInterestCalculator = objNote.ListInterestCalculator,
                        ListPIKDistribution = ListPIkDis,
                        ListDailyInterestAccruals = objNote.ListDailyInterestAccruals,
                        CalculatorExceptionMessage = objNote.CalculatorExceptionMessage
                    };
                }
                else
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = objNote.CalculatorExceptionMessage
                    };
                }
                if (_authenticationResult.ListCashflowTransactionEntry != null)
                {
                    _noteLogic.InsertCashflowTransaction(_authenticationResult.ListCashflowTransactionEntry, _noteDC.NoteId, headerUserID.ToString());
                }


                Thread FirstThread = new Thread(() => saveOutputCalculatorValues(_authenticationResult, _noteDC.NoteId, headerUserID.ToString(), _noteDC.AnalysisID));
                FirstThread.Start();
                if (notedependtslist.Count > 0)
                {
                    foreach (PayruleSetupDataContract psd in notedependtslist)
                    {
                        calculationlogic.UpdateCalculationStatus(psd.StripTransferTo, "Processing", _noteDC.AnalysisID);
                    }
                }


            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error while calculating single note id :" + _noteDC.CRENoteID, _noteDC.NoteId.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/note/getliborscheduledatabynoteId")]
        public IActionResult GetLiborScheduleDataByNoteId([FromBody] NoteDataContract _noteDC, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            List<LiborScheduleTab> lstLiborScheduledata = new List<LiborScheduleTab>();

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            int? totalCount;
            lstLiborScheduledata = _noteLogic.GetLiborScheduleTabListDataForCalcByNoteId(new Guid(_noteDC.NoteId), Convert.ToInt32(_noteDC.IndexNameID), headerUserID, _noteDC.AnalysisID, pageIndex, pageSize, out totalCount);
            try
            {
                if (lstLiborScheduledata != null)
                {
                    if (lstLiborScheduledata.Count > 0)
                    {

                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "success",
                            TotalCount = Convert.ToInt32(totalCount),
                            lstLiborScheduledata = lstLiborScheduledata
                        };
                    }
                    else
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = "failed"
                        };
                    }
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "failed"
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

        public GenericResult GetCalculatorAPI(NoteDataContract _noteCalculatorDC)
        {
            GetConfigSetting();
            //string _endPoint = System.Configuration.ConfigurationManager.AppSettings["_calcServiceEndPoint"].ToString();
            string _endPoint = Sectionroot.GetSection("_calcServiceEndPoint").Value;
            using (var client = new HttpClient())
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMinutes(4);
                    var response = httpClient.PostAsJsonAsync(_endPoint, _noteCalculatorDC).ContinueWith((postTask) => postTask.Result.EnsureSuccessStatusCode()).Result;
                    //return response;

                    return Newtonsoft.Json.JsonConvert.DeserializeObject<GenericResult>(response.Content.ReadAsStringAsync().Result);
                }
            }
        }


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/GetHistoricalDataOfModuleByNoteId")]
        public IActionResult GetHistoricalDataOfModuleByNoteId([FromBody] NoteDataContract _noteDC, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;

            //int? pageIndex = _noteDC.pageIndex;
            //int? pageSize = _noteDC.pageSize;
            string modulename = _noteDC.modulename;

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            try
            {
                bool flag = false;
                int? totalCount = 0;

                DataTable dtMaturityScenariosDataContract = new DataTable();
                DataTable dtRateSpreadSchedule = new DataTable();
                DataTable dtPrepayAndAdditionalFeeScheduleDataContract = new DataTable();
                DataTable dtStrippingScheduleDataContract = new DataTable();
                DataTable dtFinancingFeeScheduleDataContract = new DataTable();
                DataTable dtFinancingScheduleDataContract = new DataTable();
                DataTable dtDefaultScheduleDataContract = new DataTable();
                DataTable dtServicingFeeScheduleDataContract = new DataTable();
                DataTable dtPIKSchedule = new DataTable();

                DataTable dtFundingSchedule = new DataTable();
                DataTable dtPIKScheduleDetail = new DataTable();
                DataTable dtLIBORSchedule = new DataTable();
                DataTable dtAmortSchedule = new DataTable();
                DataTable dtFeeCouponStripReceivable = new DataTable();

                switch (modulename)
                {
                    case "Maturity":
                        if (_noteDC.MaturityMethodID == 0)
                        {
                            dtMaturityScenariosDataContract = _noteLogic.GetHistoricalDataOfModuleByNoteId(new Guid(_noteDC.NoteId), headerUserID, modulename, _noteDC.AnalysisID.ToString());
                        }
                        else
                        {
                            dtMaturityScenariosDataContract = _noteLogic.GetMaturityHistoricalDataByDealID(new Guid(_noteDC.DealID), headerUserID, _noteDC.MultipleNoteids);
                        }
                        if (dtMaturityScenariosDataContract.Rows.Count > 0) flag = true;

                        break;

                    //case "BalanceTransactionSchedule":
                    //    break;

                    case "DefaultSchedule":

                        dtDefaultScheduleDataContract = _noteLogic.GetHistoricalDataOfModuleByNoteId(new Guid(_noteDC.NoteId), headerUserID, modulename, _noteDC.AnalysisID.ToString());
                        if (dtDefaultScheduleDataContract.Rows.Count > 0) flag = true;

                        break;

                    //case "FeeCouponSchedule":
                    //    break;

                    case "FinancingFeeSchedule":

                        dtFinancingFeeScheduleDataContract = _noteLogic.GetHistoricalDataOfModuleByNoteId(new Guid(_noteDC.NoteId), headerUserID, modulename, _noteDC.AnalysisID.ToString());
                        if (dtFinancingFeeScheduleDataContract.Rows.Count > 0) flag = true;

                        break;

                    case "FinancingSchedule":

                        dtFinancingScheduleDataContract = _noteLogic.GetHistoricalDataOfModuleByNoteId(new Guid(_noteDC.NoteId), headerUserID, modulename, _noteDC.AnalysisID.ToString());
                        if (dtFinancingScheduleDataContract.Rows.Count > 0) flag = true;

                        break;

                    case "PIKSchedule":

                        dtPIKSchedule = _noteLogic.GetHistoricalDataOfModuleByNoteId(new Guid(_noteDC.NoteId), headerUserID, modulename, _noteDC.AnalysisID.ToString());
                        if (dtPIKSchedule.Rows.Count > 0) flag = true;

                        break;

                    case "PrepayAndAdditionalFeeSchedule":

                        dtPrepayAndAdditionalFeeScheduleDataContract = _noteLogic.GetHistoricalDataOfModuleByNoteId(new Guid(_noteDC.NoteId), headerUserID, modulename, _noteDC.AnalysisID.ToString());
                        if (dtPrepayAndAdditionalFeeScheduleDataContract.Rows.Count > 0) flag = true;

                        break;

                    case "RateSpreadSchedule":

                        dtRateSpreadSchedule = _noteLogic.GetHistoricalDataOfModuleByNoteId(new Guid(_noteDC.NoteId), headerUserID, modulename, _noteDC.AnalysisID.ToString());
                        if (dtRateSpreadSchedule.Rows.Count > 0) flag = true;

                        break;

                    case "ServicingFeeSchedule":

                        dtServicingFeeScheduleDataContract = _noteLogic.GetHistoricalDataOfModuleByNoteId(new Guid(_noteDC.NoteId), headerUserID, modulename, _noteDC.AnalysisID.ToString());
                        if (dtServicingFeeScheduleDataContract.Rows.Count > 0) flag = true;

                        break;

                    case "StrippingSchedule":

                        dtStrippingScheduleDataContract = _noteLogic.GetHistoricalDataOfModuleByNoteId(new Guid(_noteDC.NoteId), headerUserID, modulename, _noteDC.AnalysisID.ToString());
                        if (dtStrippingScheduleDataContract.Rows.Count > 0) flag = true;

                        break;

                    case "FundingSchedule":

                        dtFundingSchedule = _noteLogic.GetHistoricalDataOfModuleByNoteId(new Guid(_noteDC.NoteId), headerUserID, modulename, _noteDC.AnalysisID.ToString());
                        if (dtFundingSchedule.Rows.Count > 0) flag = true;

                        break;

                    case "PIKScheduleDetail":

                        dtPIKScheduleDetail = _noteLogic.GetHistoricalDataOfModuleByNoteId(new Guid(_noteDC.NoteId), headerUserID, modulename, _noteDC.AnalysisID.ToString());
                        if (dtPIKScheduleDetail.Rows.Count > 0) flag = true;

                        break;

                    case "LIBORSchedule":
                        dtLIBORSchedule = _noteLogic.GetHistoricalDataOfModuleByNoteId(new Guid(_noteDC.NoteId), headerUserID, modulename, _noteDC.AnalysisID.ToString());
                        if (dtLIBORSchedule.Rows.Count > 0) flag = true;

                        break;

                    case "AmortSchedule":
                        dtAmortSchedule = _noteLogic.GetHistoricalDataOfModuleByNoteId(new Guid(_noteDC.NoteId), headerUserID, modulename, _noteDC.AnalysisID.ToString());
                        if (dtAmortSchedule.Rows.Count > 0) flag = true;

                        break;

                    case "FeeCouponStripReceivable":
                        dtFeeCouponStripReceivable = _noteLogic.GetHistoricalDataOfModuleByNoteId(new Guid(_noteDC.NoteId), headerUserID, modulename, _noteDC.AnalysisID.ToString());
                        if (dtFeeCouponStripReceivable.Rows.Count > 0) flag = true;

                        break;

                    default:
                        break;
                }

                if (flag)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        lstMaturityScenariosDataContract = dtMaturityScenariosDataContract,
                        lstRateSpreadSchedule = dtRateSpreadSchedule,
                        lstPrepayAndAdditionalFeeScheduleDataContract = dtPrepayAndAdditionalFeeScheduleDataContract,
                        lstStrippingScheduleDataContract = dtStrippingScheduleDataContract,
                        lstFinancingFeeScheduleDataContract = dtFinancingFeeScheduleDataContract,
                        lstFinancingScheduleDataContract = dtFinancingScheduleDataContract,
                        lstDefaultScheduleDataContract = dtDefaultScheduleDataContract,
                        lstNoteServicingFeeScheduleDataContract = dtServicingFeeScheduleDataContract,
                        lstPIKSchedule = dtPIKSchedule,

                        lstFundingSchedule = dtFundingSchedule,
                        lstPIKScheduleDetail = dtPIKScheduleDetail,
                        lstLIBORSchedule = dtLIBORSchedule,
                        lstAmortSchedule = dtAmortSchedule,
                        lstFeeCouponStripReceivable = dtFeeCouponStripReceivable,
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Not Exists",
                        StatusCode = 404
                    };
                }
            }
            catch (Exception ex)
            {

                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in GetHistoricalDataOfModuleByNoteId: Note ID " + _noteDC.CRENoteID, _noteDC.NoteId.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/note/getAllScheduleLatestDataByNoteId")]
        public IActionResult GetAllScheduleLatestDataByNoteId([FromBody] NoteDataContract _noteDC, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            NoteAllScheduleLatestRecordDataContract _noteAllScheduleLatestRecordDataContract = new NoteAllScheduleLatestRecordDataContract();

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            int? totalCount;
            _noteAllScheduleLatestRecordDataContract = _noteLogic.GetAllScheduleLatestDataByNoteId(new Guid(_noteDC.NoteId), headerUserID, _noteDC.AnalysisID, pageIndex, pageSize, out totalCount);

            try
            {
                if (_noteAllScheduleLatestRecordDataContract != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        NoteAllScheduleLatestRecord = _noteAllScheduleLatestRecordDataContract,
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

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/getFeeCouponStripReceivableDataByNoteId")]
        public IActionResult GetFeeCouponStripReceivableListDataByNoteId([FromBody] NoteDataContract _noteDC, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            NoteAllScheduleLatestRecordDataContract _noteAllScheduleLatestRecordDataContract = new NoteAllScheduleLatestRecordDataContract();

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            int? totalCount;
            _noteAllScheduleLatestRecordDataContract.ListFeeCouponStripReceivable = _noteLogic.GetFeeCouponStripReceivableListDataByNoteId(new Guid(_noteDC.NoteId), headerUserID, _noteDC.AnalysisID, pageIndex, pageSize, out totalCount);

            try
            {
                if (_noteAllScheduleLatestRecordDataContract != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        NoteAllScheduleLatestRecord = _noteAllScheduleLatestRecordDataContract,
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
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in GetFeeCouponStripReceivableListDataByNoteId: Note ID " + _noteDC.CRENoteID, _noteDC.NoteId.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }

        public GenericResult GetCalculatorAPIForsave(NoteDataContract _noteDC)
        {
            string _endPoint = "http://localhost:63477/api/note/CalculateNoteSaveInputAndOutput";
            using (var client = new HttpClient())
            {
                using (var httpClient = new HttpClient())
                {
                    var response = httpClient.PostAsJsonAsync(_endPoint, _noteDC).Result;
                    //return response;

                    return Newtonsoft.Json.JsonConvert.DeserializeObject<GenericResult>(response.Content.ReadAsStringAsync().Result);
                }
            }
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/getNotePeriodicCalcByNoteId")]
        public IActionResult GetNotePeriodicCalcByNoteId([FromBody] NoteDataContract _noteDC, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            List<NotePeriodicOutputsDataContract> lstnotePeriodicOutputs = new List<NotePeriodicOutputsDataContract>();

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            lstnotePeriodicOutputs = _noteLogic.GetNotePeriodicCalcByNoteId(new Guid(_noteDC.NoteId), _noteDC.AnalysisID);

            try
            {
                if (lstnotePeriodicOutputs != null)
                {
                    if (lstnotePeriodicOutputs.Count > 0)
                    {

                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "success",
                            lstnotePeriodicOutputs = lstnotePeriodicOutputs
                        };
                    }
                    else
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = "failed"
                        };
                    }
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "failed"
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in GetNotePeriodicCalcByNoteId: Note ID " + _noteDC.CRENoteID, _noteDC.NoteId.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/note/getNotePeriodicCalcDynamicByNoteId")]
        public IActionResult GetNotePeriodicCalcDynamicByNoteId([FromBody] NoteDataContract _noteDC, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            //List<NoteCashflowsExportDataContract> lstNoteCashflowsExportData = new List<NoteCashflowsExportDataContract>();
            DataTable lstnotePeriodicOutputs = new DataTable();

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            lstnotePeriodicOutputs = _noteLogic.GetNotePeriodicCalcDynamicByNoteId(new Guid(_noteDC.NoteId));
            try
            {
                if (lstnotePeriodicOutputs != null)
                {
                    if (lstnotePeriodicOutputs.Rows.Count > 0)
                    {

                        lstnotePeriodicOutputs.Columns.Remove("NotePeriodicCalcID");
                        lstnotePeriodicOutputs.Columns.Remove("NoteID");
                        lstnotePeriodicOutputs.Columns.Remove("CreatedDate");
                        lstnotePeriodicOutputs.Columns.Remove("CreatedBy");
                        lstnotePeriodicOutputs.Columns.Remove("UpdatedBy");
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "success",
                            lstNoteCashflowsExportData = lstnotePeriodicOutputs
                        };
                    }
                    else
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = "failed"
                        };
                    }
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "failed"
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
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/checkduplicatenote")]
        // public IActionResult CheckDuplicateNote([FromBody] List<NoteDataContract> _noteDC)
        public IActionResult CheckDuplicateNote([FromBody] NoteDataContract _noteDC)
        {
            bool isexist = false;
            List<string> lstnoteexistmsg = new List<string>();
            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            NoteLogic noteLogic = new NoteLogic();

            if (_noteDC.CopyCRENoteId != null)
            {
                _noteDC.CRENoteID = _noteDC.CopyCRENoteId;
            }

            if (_noteDC.CopyName != null)
            {
                _noteDC.Name = _noteDC.CopyName;
            }
            if (_noteDC.CopyDealName != null)
            {
                _noteDC.DealName = _noteDC.CopyDealName;
            }
            if (_noteDC.CopyDealID != null)
            {
                _noteDC.DealID = _noteDC.CopyDealID;
            }


            isexist = noteLogic.CheckDuplicateNote(_noteDC);
            if (isexist)
            {
                lstnoteexistmsg.Add("Note " + _noteDC.Name + " and " + _noteDC.CRENoteID + " combination already exist.");

            }

            try
            {
                if (isexist == true)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Note " + _noteDC.Name + " and " + _noteDC.CRENoteID + " combination already exist.",
                        lstnoteexistmsg = lstnoteexistmsg
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        //   Message = "Note does not exist.",
                        lstnoteexistmsg = null
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
        [Services.Controllers.IsAuthenticate]
        [Route("api/note/getnoteexceptions")]
        public IActionResult GetNoteExceptions([FromBody] string objectid)
        {
            GenericResult _authenticationResult = null;
            List<NoteDataContract> lstNotes = new List<NoteDataContract>();

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            ExceptionsLogic _ExceptionsLogic = new ExceptionsLogic();

            List<ExceptionDataContract> _NoteExceptions = _ExceptionsLogic.GetNoteExceptionsList(objectid, "Note");

            try
            {
                if (_NoteExceptions != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        Allexceptionslist = _NoteExceptions
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
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in GetNoteExceptions: Note ID " + objectid, objectid, headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/note/validatenoteobj")]
        public IActionResult Validatenoteobj([FromBody] NoteAdditinalListDataContract _noteaddlistdc)
        {
            GenericResult _authenticationResult = null;
            string Validationmessage = string.Empty;
            int count = int.MinValue;
            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            #region validationengine

            ExceptionsLogic _ExceptionsLogic = new ExceptionsLogic();
            ValidationEngine validate = new ValidationEngine();

            NoteDataContract _cnoteobject = ConvertToNoteobject(_noteaddlistdc);

            List<FeeSchedulesConfigDataContract> ListFeeSchedulesConfig = new List<FeeSchedulesConfigDataContract>();
            ListFeeSchedulesConfig = _noteLogic.GetFeeSchedulesConfig(headerUserID);
            _cnoteobject.ListFeeSchedulesConfiguration = ListFeeSchedulesConfig;


            List<ExceptionDataContract> edc = validate.ValidateNoteObject(_cnoteobject);
            int Criticalerror = edc.FindAll(x => x.ActionLevelText == "Critical").ToList().Count();

            if (Criticalerror == 0)
            {
                count = edc.Count;
            }
            else
            {
                Validationmessage = "Cannot calculate with missing fields see Exception tab for more information";
                count = edc.Count;
            }

            #endregion validationengine

            try
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "Authentication succeeded",
                    Validationstring = Validationmessage,
                    exceptioncount = count,
                    Allexceptionslist = edc,
                    Criticalerror = Criticalerror
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

        //_validatenoteobj

        public NoteDataContract ConvertToNoteobject(NoteAdditinalListDataContract _noteaddlistdc)
        {
            try
            {
                NoteAdditinalListDataContract _NoteAdditinalListDataContract = _noteaddlistdc;
                NoteDataContract noteobj = new NoteDataContract();

                noteobj.NoteId = _NoteAdditinalListDataContract.noteobj.NoteId;
                noteobj.AccountID = _NoteAdditinalListDataContract.noteobj.AccountID;
                noteobj.DealID = _NoteAdditinalListDataContract.noteobj.DealID;
                noteobj.DealName = _NoteAdditinalListDataContract.noteobj.DealName;
                noteobj.CREDealID = _NoteAdditinalListDataContract.noteobj.CREDealID;
                noteobj.CalculatorExceptionMessage = _NoteAdditinalListDataContract.noteobj.CalculatorExceptionMessage;
                noteobj.CRENoteID = _NoteAdditinalListDataContract.noteobj.CRENoteID;
                noteobj.ClientNoteID = _NoteAdditinalListDataContract.noteobj.ClientNoteID;
                noteobj.Comments = _NoteAdditinalListDataContract.noteobj.Comments;
                noteobj.EnableTrace = _NoteAdditinalListDataContract.noteobj.EnableTrace;
                noteobj.InitialInterestAccrualEndDate = _NoteAdditinalListDataContract.noteobj.InitialInterestAccrualEndDate;
                noteobj.AccrualFrequency = _NoteAdditinalListDataContract.noteobj.AccrualFrequency;
                noteobj.DeterminationDateLeadDays = _NoteAdditinalListDataContract.noteobj.DeterminationDateLeadDays;
                noteobj.DeterminationDateReferenceDayoftheMonth = _NoteAdditinalListDataContract.noteobj.DeterminationDateReferenceDayoftheMonth;
                noteobj.DeterminationDateInterestAccrualPeriod = _NoteAdditinalListDataContract.noteobj.DeterminationDateInterestAccrualPeriod;
                noteobj.DeterminationDateHolidayList = _NoteAdditinalListDataContract.noteobj.DeterminationDateHolidayList;
                noteobj.DeterminationDateHolidayListText = _NoteAdditinalListDataContract.noteobj.DeterminationDateHolidayListText;
                noteobj.FirstPaymentDate = _NoteAdditinalListDataContract.noteobj.FirstPaymentDate;
                noteobj.InitialMonthEndPMTDateBiWeekly = _NoteAdditinalListDataContract.noteobj.InitialMonthEndPMTDateBiWeekly;
                noteobj.PaymentDateBusinessDayLag = _NoteAdditinalListDataContract.noteobj.PaymentDateBusinessDayLag;
                noteobj.IOTerm = _NoteAdditinalListDataContract.noteobj.IOTerm;
                noteobj.AmortTerm = _NoteAdditinalListDataContract.noteobj.AmortTerm;
                noteobj.PIKSeparateCompounding = _NoteAdditinalListDataContract.noteobj.PIKSeparateCompounding;
                noteobj.MonthlyDSOverridewhenAmortizing = _NoteAdditinalListDataContract.noteobj.MonthlyDSOverridewhenAmortizing;
                noteobj.AccrualPeriodPaymentDayWhenNotEOMonth = _NoteAdditinalListDataContract.noteobj.AccrualPeriodPaymentDayWhenNotEOMonth;
                noteobj.FirstPeriodInterestPaymentOverride = _NoteAdditinalListDataContract.noteobj.FirstPeriodInterestPaymentOverride;
                noteobj.FirstPeriodPrincipalPaymentOverride = _NoteAdditinalListDataContract.noteobj.FirstPeriodPrincipalPaymentOverride;
                noteobj.FinalInterestAccrualEndDateOverride = _NoteAdditinalListDataContract.noteobj.FinalInterestAccrualEndDateOverride;
                noteobj.AmortType = _NoteAdditinalListDataContract.noteobj.AmortType;
                noteobj.RateType = _NoteAdditinalListDataContract.noteobj.RateType;
                noteobj.RateTypeText = _NoteAdditinalListDataContract.noteobj.RateTypeText;
                noteobj.ReAmortizeMonthly = _NoteAdditinalListDataContract.noteobj.ReAmortizeMonthly;
                noteobj.ReAmortizeatPMTReset = _NoteAdditinalListDataContract.noteobj.ReAmortizeatPMTReset;
                noteobj.StubPaidInArrears = _NoteAdditinalListDataContract.noteobj.StubPaidInArrears;
                noteobj.RelativePaymenttMonth = _NoteAdditinalListDataContract.noteobj.RelativePaymenttMonth;
                noteobj.SettleWithAccrualFlag = _NoteAdditinalListDataContract.noteobj.SettleWithAccrualFlag;
                noteobj.InterestDueAtMaturity = _NoteAdditinalListDataContract.noteobj.InterestDueAtMaturity;
                noteobj.RateIndexResetFreq = _NoteAdditinalListDataContract.noteobj.RateIndexResetFreq;
                noteobj.FirstRateIndexResetDate = _NoteAdditinalListDataContract.noteobj.FirstRateIndexResetDate;
                noteobj.LoanPurchase = _NoteAdditinalListDataContract.noteobj.LoanPurchase;
                noteobj.AmortIntCalcDayCount = _NoteAdditinalListDataContract.noteobj.AmortIntCalcDayCount;
                noteobj.StubPaidinAdvanceYN = _NoteAdditinalListDataContract.noteobj.StubPaidinAdvanceYN;
                noteobj.FullPeriodInterestDueatMaturity = _NoteAdditinalListDataContract.noteobj.FullPeriodInterestDueatMaturity;
                noteobj.ProspectiveAccountingMode = _NoteAdditinalListDataContract.noteobj.ProspectiveAccountingMode;
                noteobj.IsCapitalized = _NoteAdditinalListDataContract.noteobj.IsCapitalized;
                noteobj.SelectedMaturityDateScenario = _NoteAdditinalListDataContract.noteobj.SelectedMaturityDateScenario;
                //noteobj.SelectedMaturityDate = _NoteAdditinalListDataContract.noteobj.SelectedMaturityDate;
                // noteobj.InitialMaturityDate = _NoteAdditinalListDataContract.noteobj.InitialMaturityDate;
                //noteobj.ExpectedMaturityDate = _NoteAdditinalListDataContract.noteobj.ExpectedMaturityDate;
                // noteobj.FullyExtendedMaturityDate = _NoteAdditinalListDataContract.noteobj.FullyExtendedMaturityDate;
                //noteobj.OpenPrepaymentDate = _NoteAdditinalListDataContract.noteobj.OpenPrepaymentDate;
                noteobj.CashflowEngineID = _NoteAdditinalListDataContract.noteobj.CashflowEngineID;
                noteobj.LoanType = _NoteAdditinalListDataContract.noteobj.LoanType;
                noteobj.Classification = _NoteAdditinalListDataContract.noteobj.Classification;
                noteobj.SubClassification = _NoteAdditinalListDataContract.noteobj.SubClassification;
                noteobj.GAAPDesignation = _NoteAdditinalListDataContract.noteobj.GAAPDesignation;
                noteobj.PortfolioID = _NoteAdditinalListDataContract.noteobj.PortfolioID;
                noteobj.GeographicLocation = _NoteAdditinalListDataContract.noteobj.GeographicLocation;
                noteobj.PropertyType = _NoteAdditinalListDataContract.noteobj.PropertyType;
                noteobj.RatingAgency = _NoteAdditinalListDataContract.noteobj.RatingAgency;
                noteobj.RiskRating = _NoteAdditinalListDataContract.noteobj.RiskRating;
                noteobj.PurchasePrice = _NoteAdditinalListDataContract.noteobj.PurchasePrice;
                noteobj.FutureFeesUsedforLevelYeild = _NoteAdditinalListDataContract.noteobj.FutureFeesUsedforLevelYeild;
                noteobj.TotalToBeAmortized = _NoteAdditinalListDataContract.noteobj.TotalToBeAmortized;
                noteobj.StubPeriodInterest = _NoteAdditinalListDataContract.noteobj.StubPeriodInterest;
                noteobj.WDPAssetMultiple = _NoteAdditinalListDataContract.noteobj.WDPAssetMultiple;
                noteobj.WDPEquityMultiple = _NoteAdditinalListDataContract.noteobj.WDPEquityMultiple;
                noteobj.PurchaseBalance = _NoteAdditinalListDataContract.noteobj.PurchaseBalance;
                noteobj.DaysofAccrued = _NoteAdditinalListDataContract.noteobj.DaysofAccrued;
                noteobj.InterestRate = _NoteAdditinalListDataContract.noteobj.InterestRate;
                noteobj.PurchasedInterestCalc = _NoteAdditinalListDataContract.noteobj.PurchasedInterestCalc;
                noteobj.ModelFinancingDrawsForFutureFundings = _NoteAdditinalListDataContract.noteobj.ModelFinancingDrawsForFutureFundings;
                noteobj.NumberOfBusinessDaysLagForFinancingDraw = _NoteAdditinalListDataContract.noteobj.NumberOfBusinessDaysLagForFinancingDraw;
                noteobj.FinancingFacilityID = _NoteAdditinalListDataContract.noteobj.FinancingFacilityID;
                noteobj.FinancingFacilityIDText = _NoteAdditinalListDataContract.noteobj.FinancingFacilityIDText;
                noteobj.FinancingInitialMaturityDate = _NoteAdditinalListDataContract.noteobj.FinancingInitialMaturityDate;
                noteobj.FinancingExtendedMaturityDate = _NoteAdditinalListDataContract.noteobj.FinancingExtendedMaturityDate;
                noteobj.FinancingPayFrequency = _NoteAdditinalListDataContract.noteobj.FinancingPayFrequency;
                noteobj.FinancingInterestPaymentDay = _NoteAdditinalListDataContract.noteobj.FinancingInterestPaymentDay;
                noteobj.ClosingDate = _NoteAdditinalListDataContract.noteobj.ClosingDate;
                noteobj.InitialFundingAmount = _NoteAdditinalListDataContract.noteobj.InitialFundingAmount;
                noteobj.Discount = _NoteAdditinalListDataContract.noteobj.Discount;
                noteobj.OriginationFee = _NoteAdditinalListDataContract.noteobj.OriginationFee;
                noteobj.CapitalizedClosingCosts = _NoteAdditinalListDataContract.noteobj.CapitalizedClosingCosts;
                noteobj.PurchaseDate = _NoteAdditinalListDataContract.noteobj.PurchaseDate;
                noteobj.PurchaseAccruedFromDate = _NoteAdditinalListDataContract.noteobj.PurchaseAccruedFromDate;
                noteobj.PurchasedInterestOverride = _NoteAdditinalListDataContract.noteobj.PurchasedInterestOverride;
                noteobj.OngoingAnnualizedServicingFee = _NoteAdditinalListDataContract.noteobj.OngoingAnnualizedServicingFee;
                noteobj.DiscountRate = _NoteAdditinalListDataContract.noteobj.DiscountRate;
                noteobj.ValuationDate = _NoteAdditinalListDataContract.noteobj.ValuationDate;
                noteobj.FairValue = _NoteAdditinalListDataContract.noteobj.FairValue;
                noteobj.DiscountRatePlus = _NoteAdditinalListDataContract.noteobj.DiscountRatePlus;
                noteobj.FairValuePlus = _NoteAdditinalListDataContract.noteobj.FairValuePlus;
                noteobj.DiscountRateMinus = _NoteAdditinalListDataContract.noteobj.DiscountRateMinus;
                noteobj.FairValueMinus = _NoteAdditinalListDataContract.noteobj.FairValueMinus;
                noteobj.IncludeServicingPaymentOverrideinLevelYield = _NoteAdditinalListDataContract.noteobj.IncludeServicingPaymentOverrideinLevelYield;
                noteobj.IncludeServicingPaymentOverrideinLevelYieldText = _NoteAdditinalListDataContract.noteobj.IncludeServicingPaymentOverrideinLevelYieldText;
                noteobj.CreatedBy = _NoteAdditinalListDataContract.noteobj.CreatedBy;
                noteobj.CreatedDate = _NoteAdditinalListDataContract.noteobj.CreatedDate;
                noteobj.UpdatedBy = _NoteAdditinalListDataContract.noteobj.UpdatedBy;
                noteobj.UpdatedDate = _NoteAdditinalListDataContract.noteobj.UpdatedDate;
                noteobj.PIKSeparateCompoundingText = _NoteAdditinalListDataContract.noteobj.PIKSeparateCompoundingText;
                noteobj.LoanPurchaseYNText = _NoteAdditinalListDataContract.noteobj.LoanPurchaseYNText;
                noteobj.StubPaidinAdvanceYNText = _NoteAdditinalListDataContract.noteobj.StubPaidinAdvanceYNText;
                noteobj.ModelFinancingDrawsForFutureFundingsText = _NoteAdditinalListDataContract.noteobj.ModelFinancingDrawsForFutureFundingsText;
                noteobj.RelativePaymentMonth = _NoteAdditinalListDataContract.noteobj.RelativePaymentMonth;
                noteobj.InitialIndexValueOverride = _NoteAdditinalListDataContract.noteobj.InitialIndexValueOverride;
                noteobj.StubInterestPaidonFutureAdvances = _NoteAdditinalListDataContract.noteobj.StubInterestPaidonFutureAdvances;
                noteobj.TaxAmortCheck = _NoteAdditinalListDataContract.noteobj.TaxAmortCheck;
                noteobj.PIKWoCompCheck = _NoteAdditinalListDataContract.noteobj.PIKWoCompCheck;
                noteobj.GAAPAmortCheck = _NoteAdditinalListDataContract.noteobj.GAAPAmortCheck;
                noteobj.RoundingMethod = _NoteAdditinalListDataContract.noteobj.RoundingMethod;
                noteobj.RoundingMethodText = _NoteAdditinalListDataContract.noteobj.RoundingMethodText;
                noteobj.IndexRoundingRule = _NoteAdditinalListDataContract.noteobj.IndexRoundingRule;
                noteobj.StubOnFFtext = _NoteAdditinalListDataContract.noteobj.StubOnFFtext;
                noteobj.StubOnFF = _NoteAdditinalListDataContract.noteobj.StubOnFF;
                noteobj.StubInterestPurchased = _NoteAdditinalListDataContract.noteobj.StubInterestPurchased;
                noteobj.StatusID = _NoteAdditinalListDataContract.noteobj.StatusID;
                noteobj.StatusName = _NoteAdditinalListDataContract.noteobj.StatusName;
                noteobj.Name = _NoteAdditinalListDataContract.noteobj.Name;
                noteobj.BaseCurrencyID = _NoteAdditinalListDataContract.noteobj.BaseCurrencyID;
                noteobj.StartDate = _NoteAdditinalListDataContract.noteobj.StartDate;
                noteobj.EndDate = _NoteAdditinalListDataContract.noteobj.EndDate;
                noteobj.PayFrequency = _NoteAdditinalListDataContract.noteobj.PayFrequency;
                noteobj.LoanCurrency = _NoteAdditinalListDataContract.noteobj.LoanCurrency;
                noteobj.StubIntOverride = _NoteAdditinalListDataContract.noteobj.StubIntOverride;
                noteobj.PurchasedIntOverride = _NoteAdditinalListDataContract.noteobj.PurchasedIntOverride;
                noteobj.ExitFeeFreePrepayAmt = _NoteAdditinalListDataContract.noteobj.ExitFeeFreePrepayAmt;
                noteobj.ExitFeeBaseAmountOverride = _NoteAdditinalListDataContract.noteobj.ExitFeeBaseAmountOverride;
                noteobj.ExitFeeAmortCheck = _NoteAdditinalListDataContract.noteobj.ExitFeeAmortCheck;
                noteobj.ExitFeeAmortCheckText = _NoteAdditinalListDataContract.noteobj.ExitFeeAmortCheckText;
                noteobj.FixedAmortSchedule = _NoteAdditinalListDataContract.noteobj.FixedAmortSchedule;
                noteobj.FixedAmortScheduleText = _NoteAdditinalListDataContract.noteobj.FixedAmortScheduleText;
                noteobj.UseRuletoDetermineNoteFunding = _NoteAdditinalListDataContract.noteobj.UseRuletoDetermineNoteFunding;
                noteobj.UseRuletoDetermineNoteFundingText = _NoteAdditinalListDataContract.noteobj.UseRuletoDetermineNoteFundingText;
                noteobj.NoteFundingRule = _NoteAdditinalListDataContract.noteobj.NoteFundingRule;
                noteobj.NoteFundingRuleText = _NoteAdditinalListDataContract.noteobj.NoteFundingRuleText;
                noteobj.NoteBalanceCap = _NoteAdditinalListDataContract.noteobj.NoteBalanceCap;
                noteobj.FundingPriority = _NoteAdditinalListDataContract.noteobj.FundingPriority;
                noteobj.RepaymentPriority = _NoteAdditinalListDataContract.noteobj.RepaymentPriority;
                // list items
                noteobj.NotePIKScheduleList = _NoteAdditinalListDataContract.NotePIKScheduleList;
                noteobj.RateSpreadScheduleList = _NoteAdditinalListDataContract.RateSpreadScheduleList;
                noteobj.MaturityScenariosList = _NoteAdditinalListDataContract.MaturityScenariosList;
                noteobj.lstMaturity = _NoteAdditinalListDataContract.lstMaturity;
                noteobj.NoteStrippingList = _NoteAdditinalListDataContract.NoteStrippingList;
                noteobj.NoteDefaultScheduleList = _NoteAdditinalListDataContract.NoteDefaultScheduleList;
                noteobj.NotePrepayAndAdditionalFeeScheduleList = _NoteAdditinalListDataContract.NotePrepayAndAdditionalFeeScheduleList;
                noteobj.ListFutureFundingScheduleTab = _NoteAdditinalListDataContract.ListFutureFundingScheduleTab;
                noteobj.ListPIKfromPIKSourceNoteTab = _NoteAdditinalListDataContract.ListPIKfromPIKSourceNoteTab;
                noteobj.ListFeeCouponStripReceivable = _NoteAdditinalListDataContract.ListFeeCouponStripReceivable;
                noteobj.ListLiborScheduleTab = _NoteAdditinalListDataContract.ListLiborScheduleTab;
                noteobj.ListFixedAmortScheduleTab = _NoteAdditinalListDataContract.ListFixedAmortScheduleTab;
                noteobj.NoteServicingFeeScheduleList = _NoteAdditinalListDataContract.NoteServicingFeeScheduleList;
                noteobj.NoteFinancingScheduleList = _NoteAdditinalListDataContract.NoteFinancingScheduleList;
                noteobj.ListFinancingFeeSchedule = _NoteAdditinalListDataContract.lstFinancingFeeSchedule;
                noteobj.pageIndex = _NoteAdditinalListDataContract.noteobj.pageIndex;
                noteobj.pageSize = _NoteAdditinalListDataContract.noteobj.pageSize;
                noteobj.modulename = _NoteAdditinalListDataContract.noteobj.modulename;
                noteobj.SaveWithoutCalc = _NoteAdditinalListDataContract.noteobj.SaveWithoutCalc;
                noteobj.NoofdaysrelPaymentDaterollnextpaymentcycle = _NoteAdditinalListDataContract.noteobj.NoofdaysrelPaymentDaterollnextpaymentcycle;
                noteobj.lastCalcDateTime = _NoteAdditinalListDataContract.noteobj.lastCalcDateTime;
                noteobj.FirstIndexDeterminationDateOverride = _NoteAdditinalListDataContract.noteobj.FirstIndexDeterminationDateOverride;

                return noteobj;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        [HttpGet]
        [Route("api/note/ValidateAllNote")]
        public IActionResult ValidateAllNote()
        {
            GenericResult _authenticationResult = null;

            int? totalCount;
            List<NoteDataContract> lstNotes = new List<NoteDataContract>();
            lstNotes = _noteLogic.GetAllNotesFromDealIds("", new Guid("00000000-0000-0000-0000-000000000000"), 1000, 1, out totalCount);

            foreach (NoteDataContract note in lstNotes)
            {
                try
                {
                    NoteDataContract noteobject = _noteLogic.GetNoteAllDataForCalculatorByNoteId(note.NoteId, null, note.AnalysisID, null, null);
                    ExceptionsLogic _ExceptionsLogic = new ExceptionsLogic();
                    ValidationEngine validate = new ValidationEngine();
                    List<ExceptionDataContract> edc = validate.ValidateNoteObject(noteobject);

                    if (edc.Count > 0)
                    {
                        //update validate exception in database
                        foreach (ExceptionDataContract exc in edc)
                        {
                            exc.CreatedBy = "kbaderia";
                        }
                        _ExceptionsLogic.InsertExceptions(edc, "kbaderia");
                    }
                    else
                    {
                        //remove old exceptions
                        _ExceptionsLogic.DeleteExceptionByobject(noteobject.NoteId, "note");
                    }
                }
                catch (Exception ex)
                {
                    string msg = ex.Message.ToString() + note.CRENoteID;
                }
            }

            try
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = "All note validated"
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

        [Services.Controllers.DeflateCompression]
        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Route("api/note/getnotecalculatordatabyjson")]
        public IActionResult GetNoteCalculatorDataByJson([FromBody] NoteDataContract _noteDC, int? pageIndex, int? pageSize)
        {
            IActionResult _result = null;
            GenericResult _authenticationResult = null;
            //NoteDataContract _noteCalculatorDC = new NoteDataContract();

            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            try
            {
                bool flag = false;
                int? totalCount = 0;

                var _calcJSONRequest = _noteDC.CalcJSONRequest;

                var _noteCalculatorDC = JsonConvert.DeserializeObject<NoteDataContract>(_calcJSONRequest);

                CalculationMaster cm = new CalculationMaster();
                NoteDataContract objNote = cm.StartCalculation(_noteCalculatorDC);
                //Track Note object
                var result1 = JsonConvert.SerializeObject(objNote);
                _authenticationResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = result1
                };

                return Ok(_authenticationResult);

            }
            catch (Exception ex)
            {
                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_result);
        }

        private static string NullToString(object Value)
        {
            // Value.ToString() allows for Value being DBNull, but will also convert int, double, etc.
            return Value == null ? "" : Value.ToString();

            // If this is not what you want then this form may suit you better, handles 'Null' and DBNull otherwise tries a straight cast
            // which will throw if Value isn't actually a string object.
            //return Value == null || Value == DBNull.Value ? "" : (string)Value;
        }

        public void saveOutputCalculatorValues(GenericResult _gresult, string Noteid, string UserId, Guid? AnalysisID)
        {
            try
            {


                if (_gresult.lstNotePeriodicOutputsDataContract != null)
                {
                    foreach (var item in _gresult.lstNotePeriodicOutputsDataContract)
                    {
                        item.CreatedBy = UserId;
                        item.UpdatedBy = UserId;
                        item.NoteID = new Guid(Noteid);
                    }
                    _noteLogic.InsertNotePeriodicCalc(_gresult.lstNotePeriodicOutputsDataContract);
                    _noteLogic.InsertNotePeriodicCalcFromCalculationDaily(_gresult.ListNotePeriodicOutput_Daily, UserId, new Guid(Noteid));
                    _noteLogic.InsertNotePeriodicCalcFromCalculationPVandGaap(_gresult.ListNotePeriodicOutput_PVAndGaap, UserId, new Guid(Noteid));
                    _noteLogic.InsertNotePeriodicCalcFromCalculationSpreadLibor(_gresult.ListNotePeriodicOutput_SpreadAndLibor, UserId, new Guid(Noteid));


                    _noteLogic.InsertInterestCalculator(_gresult.ListInterestCalculator, Noteid, UserId);
                    if (_gresult.ListDailyInterestAccruals != null)
                    {
                        _noteLogic.InsertDailyInterestAccural(_gresult.ListDailyInterestAccruals, Noteid, UserId);
                    }


                    PayruleSetupLogic _PayruleSetupLogic = new PayruleSetupLogic();
                    _PayruleSetupLogic.InsertUpdatePayruleDistributions(Noteid, UserId, AnalysisID);
                }

                if (_gresult.ListPIKDistribution != null)
                {
                    _noteLogic.InsertPIKDistributions(_gresult.ListPIKDistribution, UserId);
                }

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error in Saving outputs " + Noteid, Noteid, UserId, ex.TargetSite.Name.ToString(), "", ex);
            }

        }

        public Stream GenerateStreamFromString(string s)
        {
            System.IO.MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        [HttpGet]
        //[Services.Controllers.IsAuthenticate]
        [Route("api/note/getimportsourcetodw-new")]
        public IActionResult Getimportsourcetodw()
        {
            GenericResult _authenticationResult = null;

            _noteLogic.importsourcetodw();
            _authenticationResult = new GenericResult()
            {
                Succeeded = true,
                Message = "Import succeeded"
            };

            return Ok(_authenticationResult);
        }

        [HttpGet]
        [Route("api/note/getconsolidatedemailnightly")]
        public IActionResult getconsolidatedemailnightly()
        {

            GenericResult _authenticationResult = null;
            Thread FirstThread = new Thread(() => GetDealNoteFundingDiscrepancyNew());
            FirstThread.Start();


            //Thread SecondThread = new Thread(() => GetNonFullpayoffDealDiscrepancy());
            //SecondThread.Start();

            Thread ThirdThread = new Thread(() => GetParentClientMissingEmail());
            ThirdThread.Start();

            Thread fourthThread = new Thread(() => SendGenerateAutomationEmails());
            fourthThread.Start();

            Thread fithThread = new Thread(() => SendAllAutoSpreadDealsAutomationEmail("All_AutoSpread_Deals"));
            fithThread.Start();


            Thread sixthThread = new Thread(() => SendAllAutoSpreadDealsAutomationEmail("AutoSpread_UnderwritingDataChanged"));
            sixthThread.Start();

            Thread seventhThread = new Thread(() => SendFundingDrawBusinessdayEmails());
            seventhThread.Start();



            //Thread seventhThread = new Thread(() => SendErrorEmail());
            //seventhThread.Start();


            _authenticationResult = new GenericResult()
            {
                Succeeded = true,
                Message = "Consolidated email succeeded"
            };
            return Ok(_authenticationResult);
        }

        [HttpGet]
        [Route("api/note/getexecuteprocedurenightly-new")]
        public IActionResult GetExecuteProcedureNightly()
        {

            GenericResult _authenticationResult = null;
            //  GetDealNoteFundingDiscrepancyNew();
            //  GetNonFullpayoffDealDiscrepancy();
            var isButtonClick = 0;
            _noteLogic.GetExecuteProcedureNightly(Convert.ToInt32(isButtonClick));
            _authenticationResult = new GenericResult()
            {
                Succeeded = true,
                Message = "Import succeeded"
            };
            return Ok(_authenticationResult);
        }

        [HttpGet]
        [Route("api/note/getexecuteadditionalprocedurenightly")]
        public IActionResult GetExecuteAdditionalProcedureNightly()
        {
            GenericResult _authenticationResult = null;
            try
            {
                //create and upload tag file to azure
                bool isSuccess = false;
                DataTable tagdatatable = new DataTable();

                List<TagMasterDataContract> listTagFiles = _tagMasterLogic.GetTagFileNameForAzureUpload();
                List<TagFileDataContract> lstTagFile = new List<TagFileDataContract>();

                foreach (TagMasterDataContract tagfile in listTagFiles)
                {
                    tagdatatable = _tagMasterLogic.GetNoteCashflowsExportDataFromTransactionClose(tagfile.AnalysisID.ToString(), tagfile.TagMasterID.ToString());
                    isSuccess = UploadDataTableToAzureblob(tagdatatable, tagfile.TagFileName);
                    if (isSuccess)
                    {
                        lstTagFile.Add(new TagFileDataContract { TagMasterID = new Guid(tagfile.TagMasterID.ToString()), TagFileName = tagfile.TagFileName });
                        _tagMasterLogic.UpdateTagFileName(lstTagFile);
                        lstTagFile.Clear();
                        Thread SecondThread = new Thread(() => ImportIntoTransactionEntryCloseArchive(tagfile.TagMasterID, tagfile.AnalysisID));
                        SecondThread.Start();

                    }

                }
                //if (lstTagFile.Count > 0)
                //{
                //    _tagMasterLogic.UpdateTagFileName(lstTagFile);

                //}
            }
            catch { }
            _authenticationResult = new GenericResult()
            {
                Succeeded = true,
                Message = "Import succeeded"
            };

            return Ok(_authenticationResult);
        }

        public void ImportIntoTransactionEntryCloseArchive(Guid? TagMasterID, Guid? AnalysisID)
        {
            _tagMasterLogic.ImportIntoTransactionEntryCloseArchive(TagMasterID, AnalysisID);

        }

        public bool UploadDataTableToAzureblob(System.Data.DataTable dt, string csvname)
        {
            GetConfigSetting();

            System.Text.UnicodeEncoding uniEncoding = new System.Text.UnicodeEncoding();

            MemoryStream ms1 = new MemoryStream();

            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    StreamWriter sw = new StreamWriter(ms);
                    int columnCount = dt.Columns.Count;

                    for (int i = 0; i < columnCount; i++)
                    {
                        sw.Write(dt.Columns[i]);

                        if (i < columnCount - 1)
                        {
                            sw.Write(",");
                        }
                    }

                    sw.Write(sw.NewLine);

                    foreach (DataRow dr in dt.Rows)
                    {
                        for (int i = 0; i < columnCount; i++)
                        {
                            if (!Convert.IsDBNull(dr[i]))
                            {
                                sw.Write(dr[i].ToString());
                            }

                            if (i < columnCount - 1)
                            {
                                sw.Write(",");
                            }
                        }

                        sw.Write(sw.NewLine);
                    }
                    ms1 = ms;

                    //var Container = System.Configuration.ConfigurationManager.AppSettings["storage:container:name"];
                    var Container = Sectionroot.GetSection("storage:container:name").Value;

                    // Get Blob Container
                    Microsoft.WindowsAzure.Storage.Blob.CloudBlobContainer container = BlobUtilities.GetBlobClient.GetContainerReference(Container);
                    // Get reference to blob (binary content)
                    Microsoft.WindowsAzure.Storage.Blob.CloudBlockBlob blockBlob = container.GetBlockBlobReference(csvname);
                    blockBlob.Properties.ContentType = "application/octet-stream";
                    ms1.Seek(0, SeekOrigin.Begin);
                    blockBlob.UploadFromStream(ms1);
                }
                return true;
            }
            catch (Exception ex)
            {
                return true;
            }
        }

        [HttpGet]
        //[Services.Controllers.IsAuthenticate]
        [Route("api/note/getexecuteprocedureonesinaday-new")]
        public IActionResult GetExecuteProcedureOnesInADay()
        {
            GenericResult _authenticationResult = null;

            _noteLogic.ExecuteProcedureOnesInADay();
            _authenticationResult = new GenericResult()
            {
                Succeeded = true,
                Message = "Execute Procedure Ones In A Day succeeded."
            };

            return Ok(_authenticationResult);
        }



        [HttpPost]
        [Services.Controllers.DeflateCompression]
        [Services.Controllers.IsAuthenticate]
        [Route("api/note/getNoteCashflowsExportData")]
        public IActionResult GetNoteCashflowsExportData([FromBody] DownloadCashFlowDataContract downloadCashFlow)
        {
            GenericResult _authenticationResult = null;
            //List<NoteCashflowsExportDataContract> lstNoteCashflowsExportData = new List<NoteCashflowsExportDataContract>();
            DataTable lstNoteCashflowsExportData = new DataTable();

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            DealLogic deallogic = new DealLogic();
            DataTable exceptiondt = new DataTable();
            //lstNoteCashflowsExportData = _noteLogic.GetNoteCashflowsExportData(new Guid(downloadCashFlow.NoteId), new Guid(downloadCashFlow.DealID), new Guid(downloadCashFlow.AnalysisID), downloadCashFlow.MutipleNoteId);
            lstNoteCashflowsExportData = _noteLogic.GetNoteCashflowsExportData(downloadCashFlow);
            if (downloadCashFlow.Pagename == "Deal")
            {
                exceptiondt = deallogic.GetAllExceptionsByDealID(new Guid(downloadCashFlow.DealID), downloadCashFlow.Pagename, "", new Guid(), new Guid());
            }
            else if (downloadCashFlow.Pagename == "Calc")
            {
                exceptiondt = deallogic.GetAllExceptionsByDealID(new Guid(), downloadCashFlow.Pagename, downloadCashFlow.MutipleNoteId, new Guid(), new Guid());
            }
            else if (downloadCashFlow.Pagename == "Note")
            {
                exceptiondt = deallogic.GetAllExceptionsByDealID(new Guid(), downloadCashFlow.Pagename, downloadCashFlow.MutipleNoteId, new Guid(), new Guid(downloadCashFlow.NoteId));
            }
            try
            {
                if (lstNoteCashflowsExportData != null)
                {
                    if (lstNoteCashflowsExportData.Rows.Count > 0)
                    {
                        // Logger.Write("Note Cashflows Export Data " + _noteDC.NoteId + " loaded successfully", MessageLevel.Info);
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "success",
                            lstNoteCashflowsExportData = lstNoteCashflowsExportData,
                            dt = exceptiondt
                        };
                    }
                    else
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "success with no records",
                            lstNoteCashflowsExportData = lstNoteCashflowsExportData
                        };
                    }
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "failed"
                    };
                }
            }
            catch (Exception ex)
            {

                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.CashFlowDownload.ToString(), "Error occurred in cashflow download ", "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/note/checkduplicatetransactionCashflow")]
        public IActionResult CheckDuplicateTransactionCashflow([FromBody] DownloadCashFlowDataContract downloadCashFlow)
        {
            GenericResult _authenticationResult = null;
            DataTable lstCheckDuplicateTransactionCashflow = new DataTable();

            IEnumerable<string> headerValues;

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            NoteLogic noteLogic = new NoteLogic();

            if (downloadCashFlow.Pagename == "Scenario")
            {
                lstCheckDuplicateTransactionCashflow = _noteLogic.CheckDuplicateTransactionCashflowDownload(downloadCashFlow.AnalysisID);
            }
            else if (downloadCashFlow.Pagename == "Deal")
            {
                lstCheckDuplicateTransactionCashflow = _noteLogic.CheckDuplicateTransactionCashflowDownloadAnalysis_Deal(downloadCashFlow.AnalysisID, downloadCashFlow.DealID);
            }
            else if (downloadCashFlow.Pagename == "Calc")
            {
                lstCheckDuplicateTransactionCashflow = _noteLogic.CheckDuplicateTransactionCashflowDownload(downloadCashFlow.AnalysisID);
            }
            else if (downloadCashFlow.Pagename == "Note")
            {
                lstCheckDuplicateTransactionCashflow = _noteLogic.CheckDuplicateTransactionCashflowDownloadByAnalysis_Note(downloadCashFlow.AnalysisID, downloadCashFlow.NoteId);

            }

            try
            {

                if (lstCheckDuplicateTransactionCashflow.Rows.Count > 0)
                {
                    _authenticationResult = new GenericResult()
                    {
                       CheckDuplicateData = lstCheckDuplicateTransactionCashflow
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        CheckDuplicateData = null
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
        [Route("api/note/getNoteCashflowsExportExcel")]
        public IActionResult GetNoteCashflowsExportExcel([FromBody] DownloadCashFlowDataContract downloadCashFlow)
        {
            GenericResult _authenticationResult = null;
            DataTable lstNoteCashflowsExportData = new DataTable();
            DataTable lstGaapBasisExportData = new DataTable();
            DataTable lstCheckDuplicateTransactionCashflow = new DataTable();
            var headerUserID = new Guid();
            bool isallnotesselected = false;

            try
            {
                if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
                {
                    headerUserID = new Guid(Request.Headers["TokenUId"]);
                }
                DealLogic deallogic = new DealLogic();
                DataTable exceptiondt = new DataTable();

                if (downloadCashFlow.Pagename != "Deal" && downloadCashFlow.Pagename != "Note")
                {
                    if ((downloadCashFlow.MutipleNoteId == "" || downloadCashFlow.MutipleNoteId == null))
                    {
                        isallnotesselected = true;
                    }
                }

                if (downloadCashFlow.Pagename != "Scenario")
                {
                    if (isallnotesselected == true)
                    {
                        string analysisid = downloadCashFlow.AnalysisID;

                            lstNoteCashflowsExportData = _noteLogic.GetNoteCashflowsExportData_All(analysisid);
                  

                    }
                    else
                    {
                            lstNoteCashflowsExportData = _noteLogic.GetNoteCashflowsExportData(downloadCashFlow);
                        
                        if (downloadCashFlow.Pagename == "Deal")
                        {
                                lstGaapBasisExportData = _noteLogic.GetNoteCashflowsGAAPBasisExportData(downloadCashFlow);
                                exceptiondt = deallogic.GetAllExceptionsByDealID(new Guid(downloadCashFlow.DealID), downloadCashFlow.Pagename, "", new Guid(), new Guid());
                         
                        }
                        else if (downloadCashFlow.Pagename == "Calc")
                        {
                                exceptiondt = deallogic.GetAllExceptionsByDealID(new Guid(), downloadCashFlow.Pagename, downloadCashFlow.MutipleNoteId, new Guid(), new Guid());
                         
                        }
                        else if (downloadCashFlow.Pagename == "Note")
                        {
                                lstGaapBasisExportData = _noteLogic.GetNoteCashflowsGAAPBasisExportData(downloadCashFlow);
                                exceptiondt = deallogic.GetAllExceptionsByDealID(new Guid(), downloadCashFlow.Pagename, downloadCashFlow.MutipleNoteId, new Guid(), new Guid(downloadCashFlow.NoteId));
                        }
                    }

                }
                
                else
                {
                        string analysisid = downloadCashFlow.AnalysisID;
                        lstNoteCashflowsExportData = _noteLogic.GetNoteCashflowsExportData_All(analysisid);
                        exceptiondt = deallogic.GetAllExceptionsByDealID(new Guid(), "Scenario", "", new Guid(), new Guid());
                }

                // Export to excel
                DataSet ds = new DataSet();
                lstNoteCashflowsExportData.TableName = "Cashflow";
                ds.Tables.Add(lstNoteCashflowsExportData);

                if (lstGaapBasisExportData.Rows.Count > 0)
                {
                    lstGaapBasisExportData.TableName = "GaapBasis";
                    ds.Tables.Add(lstGaapBasisExportData);
                }

                if (exceptiondt.Rows.Count > 0)
                {
                    exceptiondt.TableName = "Exceptions";
                    ds.Tables.Add(exceptiondt);
                }
                Stream stream = new StreamReader(_env.WebRootPath + "/ExcelTemplate/" + "Cashflow_download.xlsx").BaseStream;
                MemoryStream ms = WriteDataToExcel(ds, stream);
                return File(ms, "application/octet-stream");
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.CashFlowDownload.ToString(), "Error occurred in cashflow download ", "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
                return Ok(_authenticationResult);
            }

        }

        [Services.Controllers.DeflateCompression]
        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Route("api/note/DownloadNoteDataTape")]
        public IActionResult DownloadNoteDataTape([FromBody] string withoutSpread)
        {
            GenericResult _authenticationResult = null;
            DataTable lstDownloadNoteDataTape = new DataTable();

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            lstDownloadNoteDataTape = _noteLogic.DownloadNoteDataTape(Convert.ToInt32(withoutSpread));

            try
            {
                if (lstDownloadNoteDataTape != null)
                {
                    if (lstDownloadNoteDataTape.Rows.Count > 0)
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "success",
                            lstDownloadNoteDataTape = lstDownloadNoteDataTape
                        };
                    }
                    else
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = "failed"
                        };
                    }
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "failed"
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
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/searchnote")]
        public IActionResult SearchNoteByCRENoteId([FromBody] NoteDataContract _noteDC)
        {
            GenericResult _authenticationResult = null;
            List<NoteDataContract> lstNotes = new List<NoteDataContract>();

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            try
            {
                lstNotes = _noteLogic.SearchNoteByCRENoteId(_noteDC);
                if (lstNotes != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstNotes = lstNotes
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

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/getactivitylogbymoduleid")]
        public IActionResult GetActivityLogModuleId([FromBody] ActivityLogDataContract logDC, int? pageIndex, int? pageSize)
        //public IActionResult GetActivityLogModuleId([FromBody] ActivityLogDataContract logDC)
        {
            GenericResult _authenticationResult = null;
            List<ActivityLogDataContract> lstactivitydc = new List<ActivityLogDataContract>();
            IEnumerable<string> headerValues;
            //  string currentTime = "2017-08-09";
            var headerUserID = string.Empty;
            int? totalCount = 0;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            lstactivitydc = _noteLogic.GetActivityLogByModuleId(headerUserID.ToString(), logDC, pageIndex, pageSize, out totalCount);

            try
            {
                if (lstactivitydc != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstActivityLog = lstactivitydc,
                        TotalCount = Convert.ToInt32(totalCount)
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




        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/checkconcurrentupdate")]
        public IActionResult CheckConcurrentUpdate([FromBody] NoteDataContract _noteDC)
        {
            GenericResult _authenticationResult = null;
            DealDataContract Deal = new DealDataContract();
            IEnumerable<string> headerValues;

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            DealLogic dealLogic = new DealLogic();

            DateTime dt = DateTime.ParseExact(_noteDC.FFLastUpdatedDate_String, "MM/dd/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);

            Deal = dealLogic.CheckConcurrentUpdate(new Guid(_noteDC.NoteId), "Note", dt);

            try
            {
                if (Deal.LastUpdatedFF == null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = ""
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = Deal.LastUpdatedByFF + " has modified the schedule on " + Convert.ToDateTime(Deal.LastUpdatedFF).ToString("M/dd/yyyy h:mm:ss tt") + " please refresh and then save your changes."

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
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/GetLastUpdatedDateAndUpdatedByForSchedule")]
        public IActionResult GetLastUpdatedDateAndUpdatedByForSchedule([FromBody] NoteDataContract _noteDC)
        {
            GenericResult _authenticationResult = null;
            NoteAllScheduleLatestRecordDataContract noteallsche = new NoteAllScheduleLatestRecordDataContract();
            IEnumerable<string> headerValues;

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            NoteLogic noteLogic = new NoteLogic();

            noteallsche = noteLogic.GetLastUpdatedDateAndUpdatedByForSchedule(new Guid(_noteDC.NoteId), "Note");

            try
            {
                if (noteallsche != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        NoteAllScheduleLatestRecord = noteallsche
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        NoteAllScheduleLatestRecord = noteallsche
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
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/addupdatenoterulebynoteId")]
        public IActionResult AddUpdateNoteRuleByNoteId([FromBody] NoteDataContract _noteDC)
        {
            GenericResult _actionResult = null;
            IEnumerable<string> headerValues;
            string Validationmessage = string.Empty;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            try
            {
                NoteLogic _noteLogic = new NoteLogic();
                _noteLogic.AddUpdateNoteRuleByNoteId(_noteDC, new Guid(headerUserID));
                _actionResult = new GenericResult()
                {
                    Succeeded = true,
                    Message = ""
                };

            }
            catch (Exception ex)
            {
                _actionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }

            return Ok(_actionResult);
        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/getAllClient")]
        public IActionResult GetAllClient()
        {
            GenericResult _authenticationResult = null;
            List<ClientDataContract> lstClient = new List<ClientDataContract>();

            lstClient = _noteLogic.GetAllClient();

            try
            {
                if (lstClient != null)
                {
                    if (lstClient.Count > 0)
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "success",
                            lstClient = lstClient
                        };
                    }
                    else
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = "failed"
                        };
                    }
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "failed"
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
        [Services.Controllers.DeflateCompression]
        [Route("api/note/getAllFund")]
        public IActionResult GetAllFund()
        {
            GenericResult _authenticationResult = null;
            List<FundDataContract> lstFund = new List<FundDataContract>();

            lstFund = _noteLogic.GetAllFund();

            try
            {
                if (lstFund != null)
                {
                    if (lstFund.Count > 0)
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "success",
                            lstFund = lstFund
                        };
                    }
                    else
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = "failed"
                        };
                    }
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "failed"
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

        public DataSet ImportWellsDataByDealID(string DealID)
        {
            DataSet lstWellsImportData = new DataSet();
            DataSet dsWellsImportData = new DataSet();
            DataTable dt, dtCopy;

            try
            {
                dsWellsImportData = _noteLogic.GetWellsViewsAllDataByDealID(DealID);

                dtCopy = new DataTable();
                dt = new DataTable("Master");
                dt.TableName = "Master";

                dtCopy = dsWellsImportData.Tables[0];
                dt = dtCopy.Copy();
                lstWellsImportData.Tables.Add(dt);
                lstWellsImportData.Tables[lstWellsImportData.Tables.Count - 1].TableName = "Master";

                dt = new DataTable("Investor");
                dt.TableName = "Investor";
                dtCopy = dsWellsImportData.Tables[1];
                dt = dtCopy.Copy();
                lstWellsImportData.Tables.Add(dt);
                lstWellsImportData.Tables[lstWellsImportData.Tables.Count - 1].TableName = "Investor";

                dt = new DataTable("Property");
                dt.TableName = "Property";
                dtCopy = dsWellsImportData.Tables[2];
                dt = dtCopy.Copy();
                lstWellsImportData.Tables.Add(dt);
                lstWellsImportData.Tables[lstWellsImportData.Tables.Count - 1].TableName = "Property";

                dt = new DataTable("Tax");
                dt.TableName = "Tax";
                dtCopy = dsWellsImportData.Tables[3];
                dt = dtCopy.Copy();
                lstWellsImportData.Tables.Add(dt);
                lstWellsImportData.Tables[lstWellsImportData.Tables.Count - 1].TableName = "Tax";

                dt = new DataTable("Insurance");
                dt.TableName = "Insurance";
                dtCopy = dsWellsImportData.Tables[4];
                dt = dtCopy.Copy();
                lstWellsImportData.Tables.Add(dt);
                lstWellsImportData.Tables[lstWellsImportData.Tables.Count - 1].TableName = "Insurance";

                dt = new DataTable("Reserve");
                dt.TableName = "Reserve";
                dtCopy = dsWellsImportData.Tables[5];
                dt = dtCopy.Copy();
                lstWellsImportData.Tables.Add(dt);
                lstWellsImportData.Tables[lstWellsImportData.Tables.Count - 1].TableName = "Reserve";

                dt = new DataTable("ARM");
                dt.TableName = "ARM";
                dtCopy = dsWellsImportData.Tables[6];
                dt = dtCopy.Copy();
                lstWellsImportData.Tables.Add(dt);
                lstWellsImportData.Tables[lstWellsImportData.Tables.Count - 1].TableName = "ARM";

            }
            catch (Exception ex)
            {

            }

            return lstWellsImportData;

        }


        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/getAllLiborSchedule")]
        public IActionResult GetLiborScheduleForFast()
        {
            GenericResult _authenticationResult = null;
            List<FLiborScheduleTab> _liborScheduleTabList = new List<FLiborScheduleTab>();

            _liborScheduleTabList = _noteLogic.GetLiborScheduleForFast();

            try
            {
                if (_liborScheduleTabList != null)
                {
                    if (_liborScheduleTabList.Count > 0)
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "success",
                            lstFastLiborScheduledata = _liborScheduleTabList
                        };
                    }
                    else
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = "failed"
                        };
                    }
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "failed"
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
        [Services.Controllers.DeflateCompression]
        [Route("api/note/GetAllNotes")]
        public IActionResult GetAllNotes()
        {
            GenericResult _authenticationResult = null;
            List<NoteDataContract> lstNotes = new List<NoteDataContract>();

            lstNotes = _noteLogic.GetAllNotes();

            try
            {
                if (lstNotes != null)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstNotes = lstNotes
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
        [Services.Controllers.DeflateCompression]
        [Route("api/note/GetAllFeeTypesFromFeeSchedulesConfig")]
        public IActionResult GetAllFeeTypesFromFeeSchedulesConfig()
        {
            GenericResult _authenticationResult = null;
            List<FeeSchedulesConfigDataContract> lstFeeSchedulesConfigDataContract = new List<FeeSchedulesConfigDataContract>();

            lstFeeSchedulesConfigDataContract = _noteLogic.GetAllFeeTypesFromFeeSchedulesConfig();

            try
            {
                if (lstFeeSchedulesConfigDataContract != null)
                {
                    if (lstFeeSchedulesConfigDataContract.Count > 0)
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "success",
                            lstFeeTypeLookUp = lstFeeSchedulesConfigDataContract
                        };
                    }
                    else
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = "failed",
                            lstFeeTypeLookUp = null
                        };
                    }
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "failed",
                        lstFeeTypeLookUp = null
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in GetAllFeeTypesFromFeeSchedulesConfig", "", "00000000-0000-0000-0000-000000000000", ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/note/getTransactionEntryByNoteId")]
        public IActionResult GetTransactionEntryByNoteId([FromBody] NoteDataContract _noteDC)
        {
            GenericResult _authenticationResult = null;
            List<TransactionEntryDataContract> lsttransactionEntry = new List<TransactionEntryDataContract>();

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            lsttransactionEntry = _noteLogic.GetTransactionEntryByNoteId(new Guid(_noteDC.NoteId), _noteDC.AnalysisID);

            try
            {
                if (lsttransactionEntry != null)
                {
                    if (lsttransactionEntry.Count > 0)
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = true,
                            Message = "success",
                            lstTransactionEntry = lsttransactionEntry
                        };
                    }
                    else
                    {
                        _authenticationResult = new GenericResult()
                        {
                            Succeeded = false,
                            Message = "failed"
                        };
                    }
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "failed"
                    };
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in GetTransactionEntryByNoteId: Note ID " + _noteDC.CRENoteID, _noteDC.NoteId.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

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
        [Services.Controllers.DeflateCompression]
        [Route("api/note/GetLookupForMaster")]
        public IActionResult GetLookupForMaster()
        {

            GenericResult _authenticationResult = null;
            List<LookupMasterDataContract> lstlookupMasterDC = new List<LookupMasterDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            NoteLogic noteLogic = new NoteLogic();
            lstlookupMasterDC = noteLogic.GetLookupForMaster();


            try
            {
                if (lstlookupMasterDC != null)
                {
                    // Logger.Write("Note lookup loaded successfully", MessageLevel.Info);
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstlookupMaster = lstlookupMasterDC
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
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in GetLookupForMaster ", "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

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
        [Route("api/note/getnotecalcinfobynoteId")]
        public IActionResult GetNoteCalcInfoByNoteId([FromBody] DevDashBoardDataContract devDashBoard)
        {
            GenericResult _authenticationResult = null;
            DevDashBoardDataContract devd = new DevDashBoardDataContract();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            NoteLogic noteLogic = new NoteLogic();
            devd = noteLogic.GetNoteCalcInfoByNoteId(new Guid(devDashBoard.NoteID), new Guid(devDashBoard.ScenarioID), new Guid(headerUserID));

            try
            {
                if (devd != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        dwstatus = devd
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
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in GetLookupForMaster ", "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

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
        [Services.Controllers.DeflateCompression]
        [Route("api/note/GetFinancingSource")]
        public IActionResult GetFinancingSource()
        {

            GenericResult _authenticationResult = null;
            List<FinancingSourceDataContract> lstfinancingsourceDC = new List<FinancingSourceDataContract>();
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            NoteLogic noteLogic = new NoteLogic();
            lstfinancingsourceDC = noteLogic.GetFinancingSource(new Guid(headerUserID));


            try
            {
                if (lstfinancingsourceDC != null)
                {
                    // Logger.Write("Note lookup loaded successfully", MessageLevel.Info);
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        lstfinancingsource = lstfinancingsourceDC
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
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error occurred in GetFinancingSource ", "", headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

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
                builder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
                var root = builder.Build();
                Sectionroot = root.GetSection("Application");
            }
        }
        public void GetDealNoteFundingDiscrepancyNew()
        {
            DataTable dt = new DataTable();
            DataTable dt1 = new DataTable();
            DataTable dt2 = new DataTable();
            DataTable dt3 = new DataTable();
            DataTable dt4 = new DataTable();
            DataTable dt5 = new DataTable();
            DataTable dt6 = new DataTable();
            DataTable dt7 = new DataTable();
            DataTable dt8 = new DataTable();
            DataTable dt9 = new DataTable();

            DataTable dt10 = new DataTable();
            DataTable dt11 = new DataTable();
            DataTable dt12 = new DataTable();
            DataTable dt13 = new DataTable();
            DataTable dt14 = new DataTable();
            DataTable dt15 = new DataTable();

            DealLogic _dealLogic = new DealLogic();
            dt = _dealLogic.GetDealNoteFundingDiscrepancy();
            dt1 = _dealLogic.GetDiscrepancyForExitAndExtentionStripReceiveable();
            dt2 = _dealLogic.GetDiscrepancyForFFBetweenM61andBackshop();
            ////dt3 = _dealLogic.GetDiscrepancyForCommitment();
            dt4 = _dealLogic.GetDiscrepancyForCommitmentData();
            dt5 = _dealLogic.GetDiscrepancyListOfDealForEnableAutoSpread();
            dt6 = _dealLogic.GetDiscrepancyForExportPaydown();
            dt7 = _dealLogic.GetDiscrepancyForNetIOTransaction();
            dt8 = _dealLogic.GetDiscrepancyForFinancingSource();
            dt9 = _dealLogic.GetInvoiceDiscrepancy();

            dt10 = _dealLogic.GetDiscrepancyForWireConfirmed();
            dt11 = _dealLogic.GetDiscrepancyForBalanceM61VsBackshop();
            dt12 = _dealLogic.GetDiscrepancyForAdjCommitmentM61VsBackshop();
            dt13 = _dealLogic.GetDiscrepancyForTotalFFVsUnfundedCommitment();
            dt14 = _dealLogic.GetDiscrepancyForDuplicatePIK_InBackshop();
            dt15 = _dealLogic.GetDiscrepancyForNotesFailedInCalculation();

            if (dt4.Rows.Count > 0)
            { dt4.Columns.Remove("DealID"); }


            #region get first row of table
            //if (dt.Rows.Count > 0)
            //    dt = dt.AsEnumerable().Take(1).CopyToDataTable();

            //if (dt1.Rows.Count > 0)
            //    dt1 = dt1.AsEnumerable().Take(1).CopyToDataTable();

            //if (dt2.Rows.Count > 0)
            //    dt2 = dt2.AsEnumerable().Take(1).CopyToDataTable();

            //if (dt4.Rows.Count > 0)
            //    dt4 = dt4.AsEnumerable().Take(1).CopyToDataTable();

            //if (dt5.Rows.Count > 0)
            //    dt5 = dt5.AsEnumerable().Take(1).CopyToDataTable();

            //if (dt6.Rows.Count > 0)
            //    dt6 = dt6.AsEnumerable().Take(1).CopyToDataTable();

            //if (dt7.Rows.Count > 0)
            //    dt7 = dt7.AsEnumerable().Take(1).CopyToDataTable();

            //if (dt8.Rows.Count > 0)
            //    dt8 = dt8.AsEnumerable().Take(1).CopyToDataTable();

            //if (dt9.Rows.Count > 0)
            //    dt9 = dt9.AsEnumerable().Take(1).CopyToDataTable();

            //if (dt10.Rows.Count > 0)
            //    dt10 = dt10.AsEnumerable().Take(1).CopyToDataTable();

            //if (dt11.Rows.Count > 0)
            //    dt11 = dt11.AsEnumerable().Take(1).CopyToDataTable();

            //if (dt12.Rows.Count > 0)
            //    dt12 = dt12.AsEnumerable().Take(1).CopyToDataTable();

            //if (dt13.Rows.Count > 0)
            //    dt13 = dt13.AsEnumerable().Take(1).CopyToDataTable();

            //if (dt14.Rows.Count > 0)
            //    dt14 = dt14.AsEnumerable().Take(1).CopyToDataTable();

            //if (dt15.Rows.Count > 0)
            //    dt15 = dt15.AsEnumerable().Take(1).CopyToDataTable();
            #endregion


            try
            {
                _iEmailNotification.SendDealFundingandNoteFundingDiscrepancy(dt, dt.Rows.Count, dt1, dt1.Rows.Count, dt2, dt2.Rows.Count, dt3, dt3.Rows.Count, dt4, dt4.Rows.Count, dt5, dt5.Rows.Count, dt6, dt6.Rows.Count, dt7, dt7.Rows.Count, dt8, dt8.Rows.Count, dt9, dt9.Rows.Count, dt10, dt10.Rows.Count, dt11, dt11.Rows.Count, dt12, dt12.Rows.Count, dt13, dt13.Rows.Count, dt14, dt14.Rows.Count, dt15, dt15.Rows.Count);
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.GenericScheduler.ToString(), "Error occurred while executing methode GetDealNoteFundingDiscrepancyNew ", "", "", ex.TargetSite.Name.ToString(), "", ex);

            }

        }

        [HttpGet]
        [Route("api/note/getRefreshBSUnderwriting")]
        public IActionResult GetRefreshBSUnderwriting()
        {
            GenericResult _authenticationResult = null;
            string Message = "";
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            var batchstatus = "Refresh From Backshop";
            var isButtonClick = 1;
            DataTable dt = _noteLogic.refreshBSUnderwritingStatus(batchstatus, new Guid(headerUserID));
            if (dt.Rows[0]["Status2"].ToString() != "Process Running")
            {
                Message = "Request to refresh backshop submitted successfully.";
                Thread FirstThread = new Thread(() => _noteLogic.GetExecuteProcedureNightly(isButtonClick));
                FirstThread.Start();
            }
            else { Message = "Refresh backshop process is already running."; }

            _authenticationResult = new GenericResult()
            {
                Succeeded = true,
                Message = Message
            };

            return Ok(_authenticationResult);
        }

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/queuenoteforcalculation")]
        public IActionResult QueueNotesForCalculation([FromBody] NoteDataContract _noteDC, int? pageIndex, int? pageSize)
        {
            GenericResult _authenticationResult = null;
            bool status = true;
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            try
            {
                GetConfigSetting();
                List<CalculationManagerDataContract> nlist = new List<CalculationManagerDataContract>();
                CalculationManagerDataContract cdc = new CalculationManagerDataContract();
                string envname = Sectionroot.GetSection("ApplicationName").Value;
                cdc.StatusText = "Processing";
                cdc.UserName = headerUserID.ToString();
                cdc.ApplicationText = envname;
                cdc.AnalysisID = _noteDC.AnalysisID;
                cdc.NoteId = _noteDC.NoteId;
                cdc.PriorityText = "Real Time";
                cdc.CalcType = 775;
                nlist.Add(cdc);

                CalculationManagerLogic calculationlogic = new CalculationManagerLogic();
                status = calculationlogic.QueueNotesForCalculation(nlist, headerUserID.ToString());

                if (status)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Calculation request submitted successfully"
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Error"
                    };
                }

            }
            catch (Exception ex)
            {

                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.Note.ToString(), "Error while calculating single : Note ID " + _noteDC.CRENoteID, _noteDC.NoteId.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }

        public void InsertSingleNoteForCalculation(string username, Guid? AnalysisID, string NoteId)
        {

            List<CalculationManagerDataContract> nlist = new List<CalculationManagerDataContract>();
            CalculationManagerDataContract cdc = new CalculationManagerDataContract();

            cdc.StatusText = "Processing";
            cdc.UserName = username.ToString();
            cdc.ApplicationText = "";
            cdc.AnalysisID = AnalysisID;
            cdc.NoteId = NoteId;
            cdc.PriorityText = "Real Time";
            cdc.CalcType = 775;
            nlist.Add(cdc);

            CalculationManagerLogic calculationlogic = new CalculationManagerLogic();
            calculationlogic.QueueNotesForCalculation(nlist, username);
        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/getholidaymaster")]
        public IActionResult GetHolidayMaster()
        {
            GenericResult _authenticationResult = null;
            DataTable dtholidaymaster = new DataTable();
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }
            UserPermissionLogic upl = new UserPermissionLogic();

            dtholidaymaster = upl.GetHolidayMaster(new Guid(headerUserID));
            try
            {
                if (dtholidaymaster.Rows.Count > 0)
                {

                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "succeeded",
                        dtholidaymaster = dtholidaymaster

                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "failed"
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
        [Services.Controllers.DeflateCompression]
        [Route("api/note/getmaturitybynoteid")]
        public IActionResult GetMaturityByNoteID(string ID)
        {
            GenericResult _authenticationResult = null;

            IEnumerable<string> headerValues;

            var headerUserID = string.Empty;

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            DealLogic dealLogic = new DealLogic();
            DataTable dtMaturity = dealLogic.GetMaturityByDealID(headerUserID, null, ID);

            try
            {
                if (dtMaturity != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        dt = dtMaturity
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
                Log.WriteLogException(CRESEnums.Module.Deal.ToString(), "Error occurred in GetMaturityByNoteID: Deal ID " + ID, ID.ToString(), headerUserID.ToString(), ex.TargetSite.Name.ToString(), "", ex);

                _authenticationResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_authenticationResult);
        }
        public MemoryStream WriteDataToExcel(DataSet dsData, Stream strm)
        {

            DataTable dt = new DataTable();
            Stream TemplateMemoryStream = new MemoryStream();
            List<string> lstTemplateLines = new List<string>();
            try
            {
                using (var package = new OfficeOpenXml.ExcelPackage(strm))
                {

                    int iSheetsCount = 0;
                    try
                    {
                        iSheetsCount = package.Workbook.Worksheets.Count;
                    }
                    catch (Exception)
                    {
                        iSheetsCount = package.Workbook.Worksheets.Count;
                    }
                    if (iSheetsCount > 0)
                    {
                        for (int i = 0; i < iSheetsCount; i++)
                        {
                            OfficeOpenXml.ExcelWorksheet worksheet;
                            try
                            {
                                worksheet = package.Workbook.Worksheets[i];
                            }
                            catch (Exception)
                            {
                                worksheet = package.Workbook.Worksheets[i];
                            }

                            if (dsData.Tables[worksheet.Name] != null)
                            {
                                worksheet.Cells[1, 1].LoadFromDataTable(dsData.Tables[worksheet.Name], true);
                            }

                        }
                        Byte[] fileBytes = package.GetAsByteArray();
                        TemplateMemoryStream = new MemoryStream(fileBytes);
                    }
                }
                return (MemoryStream)TemplateMemoryStream;

            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.CashFlowDownload.ToString(), "Error detail(method: ReadAndUploadTemplateFile) :  ", "", "", ex.TargetSite.Name.ToString(), "", ex);

                throw ex;
            }
        }
        [HttpGet]
        [Route("api/note/getrefreshentitydatatodw")]
        public IActionResult getrefreshentitydatatodw()
        {
            GenericResult _authenticationResult = null;
            string Message = "";
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            var batchstatus = "Refresh Entity Data";

            DataTable dt = _noteLogic.refreshBSUnderwritingStatus(batchstatus, new Guid(headerUserID));
            if (dt.Rows[0]["Status2"].ToString() != "Process Running")
            {
                Message = "Request to refresh entity data submitted successfully.";
                Thread FirstThread = new Thread(() => _noteLogic.refreshentitydatatodw());
                FirstThread.Start();
            }
            else { Message = "Refresh entity data process is already running."; }

            _authenticationResult = new GenericResult()
            {
                Succeeded = true,
                Message = Message
            };

            return Ok(_authenticationResult);
        }
        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/AddUpdateNoteRuleTypeSetup")]
        public IActionResult AddUpdateNoteRuleTypeSetup([FromBody] List<ScenarioruletypeDataContract> scenarioDC)
        {
            GenericResult _authenticationResult = null;
            IEnumerable<string> headerValues;
            var headerUserID = string.Empty;
            string scenariomsg = "";
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = Convert.ToString(Request.Headers["TokenUId"]);
            }

            NoteLogic noteLogic = new NoteLogic();
            string res = noteLogic.AddUpdateNoteRuleTypeSetup(scenarioDC, headerUserID);

            try
            {
                if (res != null)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
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

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/note/GetRuleTypeSetupByNoteId")]
        public IActionResult GetRuleTypeSetupByNoteId([FromBody] ScenarioruletypeDataContract scenarioruletypeDataContract)
        {
            GenericResult _authenticationResult = null;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            List<ScenarioruletypeDataContract> listscenario = new List<ScenarioruletypeDataContract>();
            NoteLogic notelogic = new NoteLogic();
            listscenario = notelogic.GetRuleTypeSetupByNoteId(scenarioruletypeDataContract.NoteID, scenarioruletypeDataContract.AnalysisID);

            try
            {
                if (listscenario.Count > 0)
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        AnalysisId = listscenario.First().AnalysisID,
                        lstScenariorule = listscenario
                    };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded"

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
        public void SendGenerateAutomationEmails()
        {
            LoggerLogic Log = new LoggerLogic();
            try
            {
                Log.WriteLogInfo(CRESEnums.Module.GenerateAutomation.ToString(), "AmortizationAutoWire Email sending called ", "", "");
                GenerateAutomationLogic GenerateAutomationLogic = new GenerateAutomationLogic();
                //--AmortizationAutoWire 'FundingMoveToNextMonth'
                DataTable dt = GenerateAutomationLogic.GetAutomationRequestsForEmail("AmortizationAutoWire");
                if (dt != null && dt.Rows.Count > 0)
                {

                    DataTable datadump = GenerateAutomationLogic.GetAutomationRequestsDataForEmailByBatchType("AmortizationAutoWire");
                    MemoryStream ms = GetAutomationStreamfromDatatableAutomation(datadump);
                    string randomstring = DateTime.Now.ToString("MM_dd_yyyy_hhmmss");
                    _iEmailNotification.SendAutoConfirmAmortization(dt, "AmortizationAutoWire", ms, "Deal_Funding_AmortizationAutoWire_" + randomstring + ".xlsx", datadump);
                    GenerateAutomationLogic.UpdateAutomationRequestsSentEmailToY("AmortizationAutoWire");
                    Log.WriteLogInfo(CRESEnums.Module.GenerateAutomation.ToString(), "Email sent for AmortizationAutoWire", "", "");
                }
                else
                {
                    Log.WriteLogInfo(CRESEnums.Module.GenerateAutomation.ToString(), "No email sent for AmortizationAutoWire as not record found ", "", "");
                }

                DataTable dt1 = GenerateAutomationLogic.GetAutomationRequestsForEmail("FundingMoveToNextMonth");
                if (dt1 != null && dt1.Rows.Count > 0)
                {
                    DataTable datadump = GenerateAutomationLogic.GetAutomationRequestsDataForEmailByBatchType("FundingMoveToNextMonth");
                    MemoryStream ms = GetAutomationStreamfromDatatableAutomation(datadump);
                    string randomstring = DateTime.Now.ToString("MM_dd_yyyy_hhmmss");
                    _iEmailNotification.SendAutoConfirmAmortization(dt1, "FundingMoveToNextMonth", ms, "Deal_Funding_AutoKickedOutFundings_" + randomstring + ".xlsx", datadump);
                    GenerateAutomationLogic.UpdateAutomationRequestsSentEmailToY("FundingMoveToNextMonth");
                }
                else
                {
                    Log.WriteLogInfo(CRESEnums.Module.GenerateAutomation.ToString(), "No email sent for FundingMoveToNextMonth as no record found ", "", "");
                }

            }
            catch (Exception ex)
            {
                Log.WriteLogException(CRESEnums.Module.GenericScheduler.ToString(), "Error occurred while executing SendGenerateAutomationEmails ", "", "", ex.TargetSite.Name.ToString(), "", ex);
            }

        }

        public void GetParentClientMissingEmail()
        {
            DataTable dt = new DataTable();
            WFLogic _wfLogic = new WFLogic();
            dt = _wfLogic.GetParentClientMissingEmail();

            try
            {
                if (dt != null && dt.Rows.Count > 0)
                {
                    _iEmailNotification.SendEmailForParentClientMissingEmailId(dt);
                }
            }
            catch (Exception ex)
            {
                LoggerLogic Log = new LoggerLogic();
                Log.WriteLogException(CRESEnums.Module.WFNotification.ToString(), "Error occurred while sending a list of parent clients needs email ids for notification ", "", "", ex.TargetSite.Name.ToString(), "", ex);

            }
        }
        public void SendAllAutoSpreadDealsAutomationEmail(string BatchType)
        {
            LoggerLogic Log = new LoggerLogic();
            try
            {
                Log.WriteLogInfo(CRESEnums.Module.GenerateAutomation.ToString(), "Email sending called for " + BatchType, "", "");
                GenerateAutomationLogic GenerateAutomationLogic = new GenerateAutomationLogic();
                DataTable dt = GenerateAutomationLogic.GetAutomationRequestsAutoSpreadDealsForEmail(BatchType);
                if (dt != null && dt.Rows.Count > 0)
                {
                    MemoryStream ms = GetAutomationStreamfromDatatableAutomation(dt);
                    string randomstring = DateTime.Now.ToString("MM_dd_yyyy_hhmmss");
                    _iEmailNotification.SendFundingValidationEmail(ms, "Deal_Funding_Validation_" + randomstring + ".xlsx", BatchType, dt);
                    GenerateAutomationLogic.UpdateAutomationRequestsSentEmailToY(BatchType);
                    Log.WriteLogInfo(CRESEnums.Module.GenerateAutomation.ToString(), "Email sent for " + BatchType, "", "");
                }
                else
                {
                    Log.WriteLogInfo(CRESEnums.Module.GenerateAutomation.ToString(), "No email sent for " + BatchType + "as no record found ", "", "");
                }

            }
            catch (Exception ex)
            {
                Log.WriteLogException(CRESEnums.Module.GenericScheduler.ToString(), "Error occurred while executing GetAutomationRequestsAutoSpreadDealsForEmail ", "", "", ex.TargetSite.Name.ToString(), "", ex);
            }
        }

        [Route("api/note/CheckAutomationEmail")]
        public IActionResult CheckAutomationEmail(string type)
        {
            try
            {


                //if (type == "all")
                //{
                //    //--789   130 AutoSpread_UnderwritingDataChanged
                //    //--799   130 All_AutoSpread_Deals
                //    SendAllAutoSpreadDealsAutomationEmail("AutoSpread_UnderwritingDataChanged");
                //}
                if (type == "amort")
                {
                    GenerateAutomationLogic GenerateAutomationLogic = new GenerateAutomationLogic();

                    DataTable dt = GenerateAutomationLogic.GetAutomationRequestsForEmail("AmortizationAutoWire");
                    if (dt != null && dt.Rows.Count > 0)
                    {

                        DataTable datadump = GenerateAutomationLogic.GetAutomationRequestsDataForEmailByBatchType("AmortizationAutoWire");
                        MemoryStream ms = GetAutomationStreamfromDatatableAutomation(datadump);
                        string randomstring = DateTime.Now.ToString("MM_dd_yyyy_hhmmss");
                        _iEmailNotification.SendAutoConfirmAmortization(dt, "AmortizationAutoWire", ms, "Deal_Funding_AmortizationAutoWire_" + randomstring + ".xlsx", datadump);

                        GenerateAutomationLogic.UpdateAutomationRequestsSentEmailToY("AmortizationAutoWire");

                    }

                }
                else if (type == "nextmonth")
                {
                    GenerateAutomationLogic GenerateAutomationLogic = new GenerateAutomationLogic();
                    DataTable dt1 = GenerateAutomationLogic.GetAutomationRequestsForEmail("FundingMoveToNextMonth");
                    if (dt1 != null && dt1.Rows.Count > 0)
                    {
                        DataTable dt = GenerateAutomationLogic.GetAutomationRequestsDataForEmailByBatchType("FundingMoveToNextMonth");
                        MemoryStream ms = GetAutomationStreamfromDatatableAutomation(dt);
                        string randomstring = DateTime.Now.ToString("MM_dd_yyyy_hhmmss");
                        _iEmailNotification.SendAutoConfirmAmortization(dt1, "FundingMoveToNextMonth", ms, "Deal_Funding_AutoKickedOutFundings_" + randomstring + ".xlsx", dt);
                        GenerateAutomationLogic.UpdateAutomationRequestsSentEmailToY("FundingMoveToNextMonth");

                    }

                }
                else if (type == "FundingMoveTo1BusinessdaysWF")
                {
                    SendFundingMoveTo1BusinessdaysWFEmails();
                }
                else
                {
                    //--789   130 AutoSpread_UnderwritingDataChanged
                    //    //--799   130 All_AutoSpread_Deals
                    SendAllAutoSpreadDealsAutomationEmail(type);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError, ex);
            }
            return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status200OK, "Email Sent");
        }

        [Route("api/note/downloadAutomationExcel")]
        public IActionResult DownloadAutomationExcel(int ID)
        {
            var ms = new MemoryStream();
            try
            {
                GenerateAutomationLogic GenerateAutomationLogic = new GenerateAutomationLogic();

                DataTable dt = GenerateAutomationLogic.GetAutomationRequestsAutoForDownloadExcel(ID);
                ms = GetAutomationStreamfromDatatableAutomation(dt);

                //if (dt != null && dt.Rows.Count > 0)
                //    {
                //        _iEmailNotification.SendAutoConfirmAmortization(dt, "AmortizationAutoWire");

                //    }
            }
            catch (Exception ex)
            {
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError, ex);
            }
            //return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status200OK, "Email Sent");
            return File(ms, "application/octet-stream");
        }

        public MemoryStream GetAutomationStreamfromDatatableAutomation(DataTable dt)
        {
            Stream ms = new MemoryStream();
            List<AutoMationOutputData> vallist = new List<AutoMationOutputData>();
            if (dt != null && dt.Rows.Count > 0)
            {
                DataView view = new DataView(dt);
                DataTable distinctValues = view.ToTable(true, "DealID");
                foreach (DataRow dv in distinctValues.Rows)
                {
                    DataTable tblFiltered = new DataTable();

                    string query = "DealID = '" + dv["DealID"].ToString() + "'";
                    tblFiltered = dt.Select(query).CopyToDataTable();
                    int j = 1;
                    AutoMationOutputData am = new AutoMationOutputData();

                    am.CREID = tblFiltered.Rows[0]["CREDealID"].ToString();
                    am.Name = tblFiltered.Rows[0]["DealName"].ToString();

                    foreach (DataRow row in tblFiltered.Rows)
                    {
                        if (row["Message"].ToString() == "Funding schedule generated successfully.")
                        {
                            am.GenerateMessage = row["Message"].ToString();
                            am.SaveMessage = "Deal Saved Successfully";
                        }
                        else
                        {
                            //read property name dynamicly
                            PropertyInfo _propertyInfo = am.GetType().GetProperty("Validation" + j);
                            _propertyInfo.SetValue(am, row["Message"].ToString(), null);
                            j = j + 1;
                            am.GenerateMessage = "";
                        }

                    }
                    vallist.Add(am);
                }
                DataTable emaildata = ObjToDataTable.ConvertToDataTable(vallist);
                DataSet ds = new DataSet();
                ds.Tables.Add(emaildata);

                Stream stream = new StreamReader(_env.WebRootPath + "/ExcelTemplate/" + "Deal_Funding_Validation.xlsx").BaseStream;
                ms = WriteDataToExcel(ds, stream);

            }

            return (MemoryStream)ms;
        }

        [Route("api/note/SendErrorEmail")]
        public void SendErrorEmail()
        {
            LoggerLogic Log = new LoggerLogic();
            try
            {
                DevDashBoardLogic DevDashBoardLogic = new DevDashBoardLogic();
                DataTable AutoMationOutputData = DevDashBoardLogic.GetErrorForEmail();
                if (AutoMationOutputData != null && AutoMationOutputData.Rows.Count > 0)
                {
                    AutoMationOutputData.TableName = "ErrorList";

                    DataSet ds = new DataSet();
                    ds.Tables.Add(AutoMationOutputData);
                    Stream stream = new StreamReader(_env.WebRootPath + "/ExcelTemplate/" + "ErrorList.xlsx").BaseStream;
                    MemoryStream ms = WriteDataToExcel(ds, stream);

                    string randomstring = DateTime.Now.ToString("MM_dd_yyyy_hhmmss");


                    _iEmailNotification.SendErrorListEmail(ms, "ErrorList_" + randomstring + ".xlsx");
                }
                else
                {
                    Log.WriteLogInfo("EmailNotification", "No email sent for SendErrorEmail as no record found ", "", "");
                }

            }
            catch (Exception ex)
            {
                Log.WriteLogException(CRESEnums.Module.GenericScheduler.ToString(), "Error occurred while executing SendErrorEmail ", "", "", ex.TargetSite.Name.ToString(), "", ex);
            }

        }

        [Route("api/note/CheckSendFundingDrawBusinessdayEmails")]
        public IActionResult CheckSendFundingDrawBusinessdayEmails()
        {
            try
            {
                SendFundingDrawBusinessdayEmails();

            }
            catch (Exception ex)
            {
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError, ex);
            }
            return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status200OK, "Email Sent");
        }
        public void SendFundingDrawBusinessdayEmails()
        {
            LoggerLogic Log = new LoggerLogic();
            try
            {
                Log.WriteLogInfo(CRESEnums.Module.GenerateAutomation.ToString(), "SendFundingDrawBusinessdayEmails sending called ", "", "");
                GenerateAutomationLogic GenerateAutomationLogic = new GenerateAutomationLogic();
                DataTable dt = GenerateAutomationLogic.GetFundingDrawByBusinessday(15);
                if (dt != null && dt.Rows.Count > 0)
                {
                    Log.WriteLogInfo(CRESEnums.Module.GenerateAutomation.ToString(), "SendFundingDrawBusinessdayEmails sending called data found ", "", "");

                    _iEmailNotification.SendFundingDrawBusinessdayEmails(dt);
                    Log.WriteLogInfo(CRESEnums.Module.GenerateAutomation.ToString(), "SendFundingDrawBusinessdayEmails sending called ended ", "", "");

                }
                else
                {
                    Log.WriteLogInfo(CRESEnums.Module.GenerateAutomation.ToString(), "No email sent for SendFundingDrawBusinessdayEmails as not record found ", "", "");
                }

            }
            catch (Exception ex)
            {
                Log.WriteLogException(CRESEnums.Module.GenericScheduler.ToString(), "Error occurred while executing SendFundingDrawBusinessdayEmails ", "", "", ex.TargetSite.Name.ToString(), "", ex);
            }

        }

        [Route("api/note/SendFundingMoveTo1BusinessdaysWFEmails")]
        public void SendFundingMoveTo1BusinessdaysWFEmails()
        {
            LoggerLogic Log = new LoggerLogic();
            try
            {
                Log.WriteLogInfo(CRESEnums.Module.GenerateAutomation.ToString(), "SendFundingMoveTo1BusinessdaysWFEmails sending called ", "", "");

                GenerateAutomationLogic GenerateAutomationLogic = new GenerateAutomationLogic();
                DataTable dt1 = GenerateAutomationLogic.GetAutomationRequestsForEmail("FundingMoveTo1BusinessdaysWF");
                if (dt1 != null && dt1.Rows.Count > 0)
                {
                    DataTable datadump = GenerateAutomationLogic.GetAutomationRequestsDataForEmailByBatchType("FundingMoveTo1BusinessdaysWF");
                    MemoryStream ms = GetAutomationStreamfromDatatableAutomation(datadump);
                    string randomstring = DateTime.Now.ToString("MM_dd_yyyy_hhmmss");
                    _iEmailNotification.SendAutoConfirmAmortization(dt1, "FundingMoveTo1BusinessdaysWF", ms, "Deal_Funding_AutoKickedOutFundings_" + randomstring + ".xlsx", datadump);
                    GenerateAutomationLogic.UpdateAutomationRequestsSentEmailToY("FundingMoveTo1BusinessdaysWF");
                }
                else
                {
                    Log.WriteLogInfo(CRESEnums.Module.GenerateAutomation.ToString(), "No email sent for SendFundingMoveTo1BusinessdaysWFEmails as no record found ", "", "");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLogException(CRESEnums.Module.GenericScheduler.ToString(), "Error occurred while executing SendFundingMoveTo1BusinessdaysWFEmails ", "", "", ex.TargetSite.Name.ToString(), "", ex);
            }

        }

        [Route("api/note/CheckGetDealNoteFundingDiscrepancy")]
        public IActionResult CheckGetDealNoteFundingDiscrepancy()
        {
            try
            {
                GetDealNoteFundingDiscrepancyNew();

            }
            catch (Exception ex)
            {
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError, ex);
            }
            return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status200OK, "Email Sent");
        }

        [HttpGet]
        [Services.Controllers.IsAuthenticate]
        [Route("api/note/getAllTagNameXIRR")]
        public IActionResult GetAllTagNameXIRR()
        {
            GenericResult _authenticationResult = null;
            TagXIRRLogic tagXIRRLogic = new TagXIRRLogic();
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            DataTable dt = tagXIRRLogic.GetAllNoteTagsXIRR(headerUserID);


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
                Log.WriteLogException(CRESEnums.Module.Account.ToString(), "Error occurred in GetAllTags", "", "", ex.TargetSite.Name.ToString(), "", ex);

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
