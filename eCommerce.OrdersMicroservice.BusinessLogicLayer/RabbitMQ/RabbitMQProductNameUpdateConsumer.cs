using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.RabbitMQ;

public class RabbitMQProductNameUpdateConsumer : IDisposable, IRabbitMQProductNameUpdateConsumer
{
    private readonly IConfiguration _configuration;
    private readonly IModel _channel;
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMQProductNameUpdateConsumer> _logger;
    
    public RabbitMQProductNameUpdateConsumer(IConfiguration configuration,  ILogger<RabbitMQProductNameUpdateConsumer> logger)
    {
        _configuration = configuration;

        string hostName = _configuration["RabbitMQ_HostName"]!;
        string userName = _configuration["RabbitMQ_UserName"]!;
        string password = _configuration["RabbitMQ_Password"]!;
        string port = _configuration["RabbitMQ_Port"]!;
        _logger = logger;

        ConnectionFactory connectionFactory = new ConnectionFactory()
        {
            HostName = hostName,
            UserName = userName,
            Password = password,
            Port = Convert.ToInt32(port)
        };
        _connection = connectionFactory.CreateConnection();

        _channel = _connection.CreateModel();
    }

    public void Consume()
    {
        // string routingKey = "product.update.name";
        var headers = new Dictionary<string, object>()
        {
            { "x-match", "all" },
            { "event", "product.update" },
            { "field", "name" },
            { "RowCount", 1 }
        };
        
        string queueName = "orders.product.update.name.queue";
        
        // Create exchange
        string exchangeName = _configuration["RabbitMQ_Products_Exchange"]!;
        _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Headers, durable: true);
        
        // Create message queue
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        
        // Bind the queue to the exchange with the routing key
        // _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: routingKey);
        _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: string.Empty, arguments: headers);

        EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (sender, args) =>
        {
            byte[] body = args.Body.ToArray();
            string message = Encoding.UTF8.GetString(body);

            if (message != null)
            {
                ProductNameUpdateMessage? productNameUpdateMessage =
                    JsonSerializer.Deserialize<ProductNameUpdateMessage>(message);
                _logger.LogInformation(
                    $"Product name updated: {productNameUpdateMessage.ProductID}, New name: {productNameUpdateMessage.NewName}");
            }
        };

        _channel.BasicConsume(queue: queueName, consumer: consumer, autoAck: true);
    }
    
    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}