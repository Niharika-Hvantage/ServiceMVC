using System.Collections.Generic;
using CRES.DataContract;
using CRES.BusinessLogic;
using System.Net.Http;
using System.Linq;
using System;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using CRES.BusinessLogic;

namespace CRES.Services.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class SearchController : ControllerBase
    {
        SearchLogic _searchLogic = new SearchLogic();

        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression] 
        [Route("api/search/getautosuggestsearchdata")]  
        public IActionResult GetAutosuggestSearchData([FromBody] SearchDataContract _searchDC, int? pageIndex, int? pageSize)
        {
            GenericResult _auctionResult = null;
            List<SearchDataContract> lstSearchResult = new List<SearchDataContract>();

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            int? totalCount;
            lstSearchResult = _searchLogic.GetAutosuggestSearchData(headerUserID, pageIndex, pageSize, _searchDC.Valuekey, out totalCount);

            try
            {
                if (lstSearchResult != null)
                {
                    _auctionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        lstSearch = lstSearchResult
                    };
                }
                else
                {
                    _auctionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed"
                    };
                }
            }
            catch (Exception ex)
            {
                _auctionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_auctionResult);
        }



        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/search/getautosuggestpikaccount")]
        public IActionResult GetAutoSearchDataPIKAccount([FromBody] SearchDataContract _searchDC, int? pageIndex, int? pageSize)
        {
            GenericResult _auctionResult = null;
            List<SearchDataContract> lstSearchResult = new List<SearchDataContract>();

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            int? totalCount;
            lstSearchResult = _searchLogic.GetAutoSearchDataPIKAccount(headerUserID, pageIndex, pageSize, _searchDC.Valuekey, out totalCount);

            try
            {
                if (lstSearchResult != null)
                {
                    _auctionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        lstSearch = lstSearchResult
                    };
                }
                else
                {
                    _auctionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed"
                    };
                }
            }
            catch (Exception ex)
            {
                _auctionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_auctionResult);
        }



        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/search/updaterankinsearchitem")]
        public IActionResult UpdateRankInSearchItem([FromBody] SearchDataContract _searchDC)
        {
            GenericResult _auctionResult = null;
            
            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }


            _searchLogic.UpdateRankInSearchItem(new Guid(_searchDC.ValueID), _searchDC.Valuekey);


            try
            {
                if (true)
                {
                    _auctionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "succeeded",
                         
                    };
                }
                
            }
            catch (Exception ex)
            {
                _auctionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_auctionResult);
            
        }



        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/search/getautosuggestsearchdeal")]
        public IActionResult GetAutosuggestSearchDeal([FromBody] SearchDataContract _searchDC, int? pageIndex, int? pageSize)
        {
            GenericResult _auctionResult = null;
            List<SearchDataContract> lstSearchResult = new List<SearchDataContract>();

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }
            int? totalCount;
            lstSearchResult = _searchLogic.GetAutosuggestSearchDeal(headerUserID, pageIndex, pageSize, _searchDC.Valuekey, out totalCount);

            try
            {
                if (lstSearchResult != null)
                {
                    _auctionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        lstSearch = lstSearchResult
                    };
                }
                else
                {
                    _auctionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed"
                    };
                }
            }
            catch (Exception ex)
            {
                _auctionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_auctionResult);
        }


        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/search/getautosuggestsearchusername")]
        public IActionResult GetAutosuggestSearchUserName([FromBody] SearchDataContract _searchDC, int? pageIndex, int? pageSize)
        {
            GenericResult _auctionResult = null;
            List<SearchDataContract> lstSearchResult = new List<SearchDataContract>();

            IEnumerable<string> headerValues;

            var headerUserID = new Guid();

            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            int? totalCount;
            lstSearchResult = _searchLogic.GetAutosuggestUserName(headerUserID, pageIndex, pageSize, _searchDC.Valuekey, out totalCount);

            try
            {
                if (lstSearchResult != null)
                {
                    _auctionResult = new GenericResult()
                    {
                        Succeeded = true,
                        Message = "Authentication succeeded",
                        TotalCount = Convert.ToInt32(totalCount),
                        lstSearch = lstSearchResult
                    };
                }
                else
                {
                    _auctionResult = new GenericResult()
                    {
                        Succeeded = false,
                        Message = "Authentication failed"
                    };
                }
            }
            catch (Exception ex)
            {
                _auctionResult = new GenericResult()
                {
                    Succeeded = false,
                    Message = ex.Message
                };
            }
            return Ok(_auctionResult);
        }

    }
}