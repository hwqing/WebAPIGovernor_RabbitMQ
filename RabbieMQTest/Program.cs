// See https://aka.ms/new-console-template for more information
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection.Metadata;
using System.Text;

Console.WriteLine("RabbitMQ TestPanel");



var factory = new ConnectionFactory();
factory.HostName = "127.0.0.1";
factory.Port = 5672;

using (var connection = factory.CreateConnection())
using (var channel = connection.CreateModel())
{
    while (true)
    {
        string cmdLine = Console.ReadLine();
        string[] cmdLineArray = cmdLine.Split(' ');
        string cmd = cmdLineArray.First().ToLower();
        if (cmd == "send_to_exchange")
        {
            string exchangeName = cmdLineArray[1];
            byte[] msgBody = Encoding.UTF8.GetBytes(cmdLineArray.Last());
            channel.ExchangeDeclare(exchangeName, ExchangeType.Fanout, true, false, null);
            channel.BasicPublish(exchangeName, string.Empty, null, msgBody);
        }
        else if (cmd == "receive_from_exchange")
        {
            string exchangeName = cmdLineArray[1];
            string queueName = cmdLineArray[2];

            channel.ExchangeDeclare(exchangeName, ExchangeType.Fanout, true, false, null);
            channel.QueueDeclare(queueName, true, false, false, null);
            channel.QueueBind(queueName, exchangeName, string.Empty, null);

            var consumer = new EventingBasicConsumer(channel);
            channel.BasicConsume(queueName, false, consumer);
            consumer.Received += (model, ea) =>
            {
                byte[] body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine(message);

                channel.BasicAck(ea.DeliveryTag, false);
            };
        }
    }
}

void Consumer_Received(object? sender, BasicDeliverEventArgs e)
{
    byte[] body = e.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine(message);

    IModel model = sender as IModel;
    model.BasicAck(e.DeliveryTag, false);
}
