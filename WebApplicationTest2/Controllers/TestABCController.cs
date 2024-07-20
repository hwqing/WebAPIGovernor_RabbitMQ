using CommonLibrary;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace WebApplicationTest2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestABCController : ControllerBase
    {
        [HttpGet]
        [Route("testapi333")]
        public string TestApi333(int input)
        {
            return $"TestApi333333333333_{HttpContext.Connection.LocalIpAddress}_{HttpContext.Connection.LocalPort}_{input}";
        }

        [HttpPost]
        [Route("testpostapi444")]
        public string TestPostApi444(CommonStructure abccc)
        {
            Thread.Sleep(5000);
            return $"TestApi333333333333_{HttpContext.Connection.LocalIpAddress}_{HttpContext.Connection.LocalPort}_{JsonConvert.SerializeObject(abccc)}";
        }
    }
}
