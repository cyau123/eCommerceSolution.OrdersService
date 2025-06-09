using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.RabbitMQ;

public class RabbitMQProductDeletionConsumer : IDisposable, IRabbitMQProductDeletionConsumer
{
  private readonly IConfiguration _configuration;
  private readonly IModel _channel;
  private readonly IConnection _connection;
  private readonly ILogger<RabbitMQProductDeletionConsumer> _logger;

  public RabbitMQProductDeletionConsumer(IConfiguration configuration, ILogger<RabbitMQProductDeletionConsumer> logger)
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
    // If ExchangeType is Topic, the routing key can be a pattern like "product.#" to match multiple queues.
    // (# matches zero or more words, * matches exactly one word). For example, "product.update.*" would match "product.update.name" and "product.update.price".
    // string routingKey = "product.delete";
    
    var headers = new Dictionary<string, object>()
    {
      { "x-match", "all" }, // Use "any" to match any of the headers, or "all" to match all headers.
      { "event", "product.delete" }, // Custom header to identify the type of message.
      { "RowCount", 1 }
    };
    
    string queueName = "orders.product.delete.queue";

    //Create exchange
    string exchangeName = _configuration["RabbitMQ_Products_Exchange"]!;
    // _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Direct, durable: true);
    _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Headers, durable: true);
    // If ExchangeType is Fanout, we can delete the routing key, as it is not used in that case, or use String.Empty
    // All message queues bound to the exchange will receive the message, meaning that NameUpdate Consumer will also receive the message.

    //Create message queue
    _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null); //x-message-ttl | x-max-length | x-expired 

    //Bind the message to exchange
    // _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: routingKey);
    _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: string.Empty, arguments: headers);

    EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);

    consumer.Received += (sender, args) =>
    {
      byte[] body = args.Body.ToArray();
      string message = Encoding.UTF8.GetString(body);

      if (message != null)
      {
        ProductDeletionMessage? productDeletionMessage = JsonSerializer.Deserialize<ProductDeletionMessage>(message);

        if (productDeletionMessage != null)
        {
          _logger.LogInformation($"Product deleted: {productDeletionMessage.ProductID}, Product name: {productDeletionMessage.ProductName}");
        }
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