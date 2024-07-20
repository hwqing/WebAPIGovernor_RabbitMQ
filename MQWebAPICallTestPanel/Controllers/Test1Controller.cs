using Microsoft.AspNetCore.Mvc;
using MQWebAPICallTestPanel.Services;
using Newtonsoft.Json;

namespace MQWebAPICallTestPanel.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Test1Controller : ControllerBase
    {
        private readonly IMQFService _mqfService;
        public Test1Controller(IMQFService mQFService)
        {
            _mqfService = mQFService;
        }

        [HttpGet]
        [Route("testapi1")]
        public string TestApi1(int index)
        {
            return $"TestApi_{HttpContext.Connection.LocalIpAddress}_{HttpContext.Connection.LocalPort}";
        }

        [HttpPost]
        [Route("testapi2")]
        public string TestApi2(TestParam param)
        {
            var content = JsonConvert.SerializeObject(param);

            return $"TestApi_{content}";
        }

        [HttpGet]
        [Route("testremotecall1")]
        public async Task<string> TestRemoteCall(int input)
        {
            return await _mqfService.Remote_testapi333(input);
        }

        [HttpGet]
        [Route("testremotecall2")]
        public async Task<string> TestRemoteCall2(string name, string desc)
        {
            return await _mqfService.Remote_testpostapi444(new CommonLibrary.CommonStructure
            {
                Name = name,
                Description = desc
            });
        }
    }

    public class TestParam
    {
        public int index;
        public string value;
    }
}
