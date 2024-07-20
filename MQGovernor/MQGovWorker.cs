
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;

namespace MQGovernor
{
    public class MQGovWorker : Microsoft.Extensions.Hosting.BackgroundService
    {
        private static ConnectionFactory _connectionFactory;
        private static IConnection _connection;
        private static IModel Channel;
        private EventingBasicConsumer _consumer;

        private static string _myServiceName = string.Empty;
        private static string _myQueueName = string.Empty;
        private static string _myExchangeName = string.Empty;
        private static string _governorExchangeName = string.Empty;
        private static string _mqHost = string.Empty;
        private static int _mqPort = -1;

        private string _myServerIp = string.Empty;
        private string _myServerPort = string.Empty;

        private HttpClient _httpClient = null;

        private readonly IServer _server;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;


        public static Dictionary<Guid, BridgeRequestResultData> GlobalBridgeRequest = new Dictionary<Guid, BridgeRequestResultData>();

        public static void SendToGovernor(BridgeRequestData request)
        {
            request.Requestor = _myServiceName;
            Channel.BasicPublish(_governorExchangeName, string.Empty, null, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request)));
        }

        public MQGovWorker(IConfiguration configuration, IServer server, IHostApplicationLifetime hostApplicationLifetime)
        {
            _myServiceName = configuration["AppSettings:MyServiceName"];
            _myExchangeName = _myServiceName;
            _myQueueName = _myServiceName;
            _governorExchangeName = configuration["AppSettings:GovernorExchangeName"];
            _mqHost = configuration["AppSettings:RabbitMQHost"];
            _mqPort = int.Parse(configuration["AppSettings:RabbitMQPort"]);
            _httpClient = new HttpClient();
            _hostApplicationLifetime = hostApplicationLifetime;
            _server = server;
        }

        private Task WaitForApplicationStarted()
        {
            var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _hostApplicationLifetime.ApplicationStarted.Register(() => completionSource.TrySetResult());
            return completionSource.Task;
        }

        private void ProcessServerIpPort()
        {
            var addresses = _server.Features.Get<IServerAddressesFeature>().Addresses.ToArray();
            string targetProtocol = "http://";
            foreach (var address in addresses)
            {
                if (address.StartsWith(targetProtocol))
                {
                    string[] array = address.Replace(targetProtocol, string.Empty).Split(':');
                    _myServerIp = array[0];
                    _myServerPort = array[1];
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await WaitForApplicationStarted();
            ProcessServerIpPort();
            InitializeMessageQueueConnection();
            var registration = new BridgeRegistrationData
            {
                QueueName = _myQueueName,
                ServiceName = _myServiceName
            };
            Channel.BasicPublish(_governorExchangeName, string.Empty, null, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(registration)));
            Console.WriteLine($"Service Request submitted to governor: ServiceName = {_myServiceName}, QueueName = {_myQueueName}");
        }

        private void InitializeMessageQueueConnection()
        {
            _connectionFactory = new ConnectionFactory();
            _connectionFactory.HostName = _mqHost;
            _connectionFactory.Port = _mqPort;

            _connection = _connectionFactory.CreateConnection();
            Channel = _connection.CreateModel();

            Channel.ExchangeDeclare(_myExchangeName, ExchangeType.Fanout, true, false, null);
            Channel.QueueDeclare(_myQueueName, true, false, false, null);
            Channel.QueueBind(_myQueueName, _myExchangeName, string.Empty, null);

            _consumer = new EventingBasicConsumer(Channel);
            Channel.BasicConsume(_myQueueName, false, _consumer);
            _consumer.Received += _consumer_Received;
        }

        private void _consumer_Received(object? sender, BasicDeliverEventArgs e)
        {
            byte[] body = e.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Channel.BasicAck(e.DeliveryTag, false);

            var baseData = JsonConvert.DeserializeObject<BridgeBaseData>(message);
            if (baseData.DataType == BridgeDataType.Request)
            {
                Console.WriteLine($"Service Request received: {message}");
                var requestData = JsonConvert.DeserializeObject<BridgeRequestData>(message);
                if (requestData.RequestMechanism == BridgeRequestMechanism.Sync)
                {
                    PerformHttpRequest(requestData).GetAwaiter().GetResult();
                }
                else if (requestData.RequestMechanism == BridgeRequestMechanism.Async)
                {
                    PerformHttpRequest(requestData);
                }
                PerformHttpRequest(requestData);
            }
            else if (baseData.DataType == BridgeDataType.Response)
            {
                Console.WriteLine($"Service Response received: {message}");
                var responseData = JsonConvert.DeserializeObject<BridgeResponseData>(message);
                if (GlobalBridgeRequest.ContainsKey(responseData.RequestGuid))
                {
                    GlobalBridgeRequest[responseData.RequestGuid].ResultContent = responseData.Content;
                    GlobalBridgeRequest[responseData.RequestGuid].QuitEvent.Set();
                }
            }
        }



        private async Task PerformHttpRequest(BridgeRequestData data)
        {

            string fullUrl = $"http://{_myServerIp}:{_myServerPort}/{data.Url}";
            Console.WriteLine($"About to access {fullUrl}");

            if (data.Headers != null)
            {
                foreach (var header in data.Headers)
                {
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            string result = string.Empty;

            if (data.Method == BridgeRequestMethod.POST)
            {
                var content = new StringContent(data.Body, Encoding.UTF8, data.ContentType);
                var response = await _httpClient.PostAsync(fullUrl, content);
                result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"POST {fullUrl} completed");
            }
            else if (data.Method == BridgeRequestMethod.GET)
            {
                var response = await _httpClient.GetAsync(fullUrl);
                result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"GET {fullUrl} completed");
            }
            else if (data.Method == BridgeRequestMethod.PUT)
            {
                var content = new StringContent(data.Body, Encoding.UTF8, data.ContentType);
                var response = await _httpClient.PutAsync(fullUrl, content);
                result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"PUT {fullUrl} completed");
            }
            else if (data.Method == BridgeRequestMethod.DELETE)
            {
                var response = await _httpClient.DeleteAsync(fullUrl);
                result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"DELETE {fullUrl} completed");
            }
            //else
            //{
            //    return string.Empty;
            //}

            var responseData = new BridgeResponseData
            {
                RequestGuid = data.RequestGuid,
                Content = result
            };

            Channel.BasicPublish(_governorExchangeName, string.Empty, null, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responseData)));
            Console.WriteLine($"Service Request result sent to governer");

        }
    }
}
