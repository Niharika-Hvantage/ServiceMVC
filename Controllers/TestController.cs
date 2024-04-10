using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CRES.ServiceMVC.Controllers
{
    //[Route("api/account")]
    [ApiController]
    public class TestController : ControllerBase
    {

        // GET: api/Test
        [HttpGet]
        [Route("api/account/getaccoutdetail")]
        public IEnumerable<string> Get123()
        {
            return new string[] { "value1", "value2" };
        }

        //// GET: api/Test/5
        //[HttpGet("{id}", Name = "Get")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        // POST: api/Test
        [HttpPost]
        [Route("api/account/post123")]
        public void Post([FromBody] string value)
        {
        }

       
    }
}
