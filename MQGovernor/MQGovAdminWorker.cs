using Microsoft.Extensions.Hosting;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace MQGovernor
{
    public class MQGovAdminWorker : BackgroundService
    {
        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;
        private EventingBasicConsumer _consumer;

        private string _myQueueName = string.Empty;
        private string _governorExchangeName = string.Empty;
        private string _mqHost = string.Empty;
        private int _mqPort = -1;

        private Dictionary<string, string> serviceNameQueueNameMappingDict = new Dictionary<string, string>();
        private Dictionary<Guid, string> requestDict = new Dictionary<Guid, string>();

        public MQGovAdminWorker(IConfiguration configuration)
        {
            _myQueueName = configuration["AppSettings:MyQueueName"];
            _governorExchangeName = configuration["AppSettings:GovernorExchangeName"];
            _mqHost = configuration["AppSettings:RabbitMQHost"];
            _mqPort = int.Parse(configuration["AppSettings:RabbitMQPort"]);
        }

        private void InitializeMessageQueueConnection()
        {
            _connectionFactory = new ConnectionFactory();
            _connectionFactory.HostName = _mqHost;
            _connectionFactory.Port = _mqPort;

            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(_governorExchangeName, ExchangeType.Fanout, true, false, null);
            _channel.QueueDeclare(_myQueueName, true, false, false, null);
            _channel.QueueBind(_myQueueName, _governorExchangeName, string.Empty, null);

            _consumer = new EventingBasicConsumer(_channel);
            _channel.BasicConsume(_myQueueName, false, _consumer);
            _consumer.Received += _consumer_Received;
            Console.WriteLine($"MQ Governor Admin Ready...");
        }

        private void PerformMessage(string message)
        {
            var baseType = JsonConvert.DeserializeObject<BridgeBaseData>(message);
            if (baseType.DataType == BridgeDataType.Registration)
            {
                var data = JsonConvert.DeserializeObject<BridgeRegistrationData>(message);
                if (!serviceNameQueueNameMappingDict.ContainsKey(data.ServiceName))
                {
                    serviceNameQueueNameMappingDict.Add(data.ServiceName, data.QueueName);
                }
                else
                {
                    serviceNameQueueNameMappingDict[data.ServiceName] = data.QueueName;
                }

                Console.WriteLine($"Service Registered: ServiceName = {data.ServiceName}, QueueName = {data.QueueName}");
            }
            else if (baseType.DataType == BridgeDataType.Request)
            {
                var data = JsonConvert.DeserializeObject<BridgeRequestData>(message);
                if (!requestDict.ContainsKey(data.RequestGuid))
                {
                    requestDict.Add(data.RequestGuid, data.Requestor);
                }
                else
                {
                    requestDict[data.RequestGuid] = data.Requestor;
                }

                Console.WriteLine($"Service Requested Received {message}");

                if (serviceNameQueueNameMappingDict.ContainsKey(data.ServiceName))
                {
                    string qName = serviceNameQueueNameMappingDict[data.ServiceName];
                    _channel.BasicPublish(qName, string.Empty, null, Encoding.UTF8.GetBytes(message));
                    Console.WriteLine($"Service Request dispatched to target: qName = {qName}");
                }
            }
            else if (baseType.DataType == BridgeDataType.Response)
            {
                var data = JsonConvert.DeserializeObject<BridgeResponseData>(message);
                Console.WriteLine($"Service Response received: {message}");

                if (requestDict.ContainsKey(data.RequestGuid))
                {
                    var requestor = requestDict[data.RequestGuid];
                    if (serviceNameQueueNameMappingDict.ContainsKey(requestor))
                    {
                        var qName = serviceNameQueueNameMappingDict[requestor];
                        _channel.BasicPublish(qName, string.Empty, null, Encoding.UTF8.GetBytes(message));
                        Console.WriteLine($"Service Response dispatched to target: qName = {qName}");
                    }
                }
            }
        }

        private void _consumer_Received(object? sender, BasicDeliverEventArgs e)
        {
            byte[] body = e.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var baseType = JsonConvert.DeserializeObject<BridgeBaseData>(message);
            _channel.BasicAck(e.DeliveryTag, false);
            PerformMessage(message);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            InitializeMessageQueueConnection();
        }
    }
}
