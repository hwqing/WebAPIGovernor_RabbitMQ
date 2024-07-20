
using CommonLibrary;
using MQGovernor;
using Newtonsoft.Json;

namespace MQWebAPICallTestPanel.Services
{
    public class MQFService : IMQFService
    {
        private string targetServiceName = "WebApplicationTest2";
        public async Task<string> Remote_testapi333(int input)
        {
            MQFClient client = new MQFClient();
            string url = $"testabc/testapi333?input={input}";
            return await client.GetAsync(targetServiceName, url, null);
        }

        public async Task<string> Remote_testpostapi444(CommonStructure content)
        {
            MQFClient client = new MQFClient();
            string url = $"testabc/testpostapi444";
            return await client.PostAsync(targetServiceName, url, JsonConvert.SerializeObject(content), null, BridgeRequestMechanism.Sync);
        }
    }
}
