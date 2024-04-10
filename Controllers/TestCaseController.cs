
using CRES.BusinessLogic;
using CRES.DataContract;
using CRES.Utilities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;


namespace CRES.Services.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("CRESPolicy")]
    public class TestCaseController : ControllerBase
    {
        [HttpPost]
        [Services.Controllers.IsAuthenticate]
        [Services.Controllers.DeflateCompression]
        [Route("api/testcase/runtestcase")]
        public IActionResult RunTestCases([FromBody]  TestCaseDataContract testcase, int? pageIndex, int? pageSize)
        {
            
            GenericResult _authenticationResult = null;
            DataTable TestCasesdatatable = new DataTable(); 
            TestCaseLogic testcaselogic = new TestCaseLogic();
           
            IEnumerable<string> headerValues;
            var headerUserID = new Guid();
            if (!string.IsNullOrEmpty(Request.Headers["TokenUId"]))
            {
                headerUserID = new Guid(Request.Headers["TokenUId"]);
            }

            TestCasesdatatable = testcaselogic.RunTestCases(testcase.isRun, headerUserID.ToString(), testcase.ModuleName, pageSize, pageIndex);
            try
            {
                if (TestCasesdatatable.Rows.Count>0)
                {
                    var TotalCount = Convert.ToInt32(TestCasesdatatable.Rows[0][0]);
                    var lastexcutedtime = TestCasesdatatable.Rows[0][1].ToString();
                    TestCasesdatatable.Columns.Remove("TotalCount");
                    TestCasesdatatable.Columns.Remove("LastExcuted");
                    _authenticationResult = new GenericResult()
                    {
                        dtTestCase = TestCasesdatatable,
                        TotalCount = TotalCount,
                        Trace = lastexcutedtime,
                        Succeeded = true,
                        Message = "Authentication succeeded",
                       

                };
                }
                else
                {
                    _authenticationResult = new GenericResult()
                    {
                        dtTestCase = null,
                        Succeeded = false,
                        TotalCount = 0,
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