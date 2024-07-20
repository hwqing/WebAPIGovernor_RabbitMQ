using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQGovernor
{
    public class MQFClient
    {
        public MQFClient()
        {

        }

        public async Task<string> DeleteAsync(string serviceName, string url, Dictionary<string, string> headers, BridgeRequestMechanism mechanism = BridgeRequestMechanism.Async)
        {
            BridgeRequestData request = new BridgeRequestData
            {
                Body = string.Empty,
                DataType = BridgeDataType.Request,
                Headers = headers,
                Method = BridgeRequestMethod.DELETE,
                RequestGuid = Guid.NewGuid(),
                ServiceName = serviceName,
                Url = url,
                RequestMechanism = mechanism
            };

            BridgeRequestResultData resData = new BridgeRequestResultData
            {
                QuitEvent = new ManualResetEvent(false),
                RequestGuid = request.RequestGuid,
                ResultContent = string.Empty
            };

            return await CommonRequest(request, resData);
        }

        public async Task<string> GetAsync(string serviceName, string url, Dictionary<string, string> headers, BridgeRequestMechanism mechanism = BridgeRequestMechanism.Async)
        {
            BridgeRequestData request = new BridgeRequestData
            {
                Body = string.Empty,
                DataType = BridgeDataType.Request,
                Headers = headers,
                Method = BridgeRequestMethod.GET,
                RequestGuid = Guid.NewGuid(),
                ServiceName = serviceName,
                Url = url,
                RequestMechanism = mechanism
            };

            BridgeRequestResultData resData = new BridgeRequestResultData
            {
                QuitEvent = new ManualResetEvent(false),
                RequestGuid = request.RequestGuid,
                ResultContent = string.Empty
            };

            return await CommonRequest(request, resData);
        }

        private Task<string> CommonRequest(BridgeRequestData request, BridgeRequestResultData resultData)
        {
            return Task.Run(string () =>
            {
                MQGovWorker.SendToGovernor(request);
                MQGovWorker.GlobalBridgeRequest.Add(resultData.RequestGuid, resultData);
                resultData.QuitEvent.WaitOne();
                return MQGovWorker.GlobalBridgeRequest[resultData.RequestGuid].ResultContent;
            });
        }

        public async Task<string> PostAsync(string serviceName, string url, string body, Dictionary<string, string> headers, BridgeRequestMechanism mechanism = BridgeRequestMechanism.Async)
        {
            BridgeRequestData request = new BridgeRequestData
            {
                Body = body,
                DataType = BridgeDataType.Request,
                Headers = headers,
                Method = BridgeRequestMethod.POST,
                RequestGuid = Guid.NewGuid(),
                ServiceName = serviceName,
                Url = url,
                RequestMechanism = mechanism
            };

            BridgeRequestResultData resData = new BridgeRequestResultData
            {
                QuitEvent = new ManualResetEvent(false),
                RequestGuid = request.RequestGuid,
                ResultContent = string.Empty
            };

            return await CommonRequest(request, resData);
        }

        public async Task<string> PutAsync(string serviceName, string url, string body, Dictionary<string, string> headers, BridgeRequestMechanism mechanism = BridgeRequestMechanism.Async)
        {
            BridgeRequestData request = new BridgeRequestData
            {
                Body = body,
                DataType = BridgeDataType.Request,
                Headers = headers,
                Method = BridgeRequestMethod.PUT,
                RequestGuid = Guid.NewGuid(),
                ServiceName = serviceName,
                Url = url,
                RequestMechanism = mechanism
            };

            BridgeRequestResultData resData = new BridgeRequestResultData
            {
                QuitEvent = new ManualResetEvent(false),
                RequestGuid = request.RequestGuid,
                ResultContent = string.Empty
            };

            return await CommonRequest(request, resData);
        }
    }
}
