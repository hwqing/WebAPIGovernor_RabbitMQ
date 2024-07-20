using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQGovernor
{
    public class BridgeRegistrationData : BridgeBaseData
    {
        public string ServiceName { get; set; }
        public string QueueName { get; set; }
        public override BridgeDataType DataType { get; set; } = BridgeDataType.Registration;
    }

    public class BridgeRequestData : BridgeBaseData
    {
        public Guid RequestGuid { get; set; }
        public string Requestor { get; set; }
        public string ServiceName { get; set; }
        public string Url { get; set; }
        public BridgeRequestMethod Method { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string ContentType { get; set; } = "application/json";
        public override BridgeDataType DataType { get; set; } = BridgeDataType.Request;
    }

    public class BridgeResponseData : BridgeBaseData
    {
        public Guid RequestGuid { get; set; }
        public string Content { get; set; }
        public override BridgeDataType DataType { get; set; } = BridgeDataType.Response;
    }

    public class BridgeBaseData
    {
        public virtual BridgeDataType DataType { get; set; }
        public BridgeRequestMechanism RequestMechanism { get; set; } = BridgeRequestMechanism.Async;
    }

    public enum BridgeRequestMethod
    {
        GET,
        POST,
        PUT,
        DELETE
    }

    public enum BridgeRequestMechanism
    {
        Sync,
        Async
    }

    public enum BridgeDataType
    {
        Registration,
        Request,
        Response
    }

    public class BridgeRequestResultData
    {
        public Guid RequestGuid { get; set; }
        public string ResultContent { get; set; }
        public ManualResetEvent QuitEvent { get; set; }
    }
}
