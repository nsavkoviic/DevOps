using System.Text;
using System.Text.Json;
using OrderService.Models;
using RabbitMQ.Client;

namespace OrderService.Services;

public class MessagePublisher : IMessagePublisher, IDisposable
{
    private readonly IConfiguration _config;
    private readonly ILogger<MessagePublisher> _logger;
    private readonly object _lock = new();
    private IConnection? _connection;
    private bool _disposed;

    public MessagePublisher(IConfiguration config, ILogger<MessagePublisher> logger)
    {
        _config = config;
        _logger = logger;
    }

    private IConnection GetConnection()
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        lock (_lock)
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            _connection?.Dispose();

            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQ:Host"] ?? "localhost",
                UserName = _config["RabbitMQ:Username"] ?? "devops",
                Password = _config["RabbitMQ:Password"] ?? "devops123"
            };
            _connection = factory.CreateConnection();
            _logger.LogInformation("RabbitMQ connection established for MessagePublisher");

            return _connection;
        }
    }

    public void PublishOrderCreated(OrderCreatedMessage message)
    {
        var connection = GetConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare(
            queue: "order-created",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        channel.BasicPublish(
            exchange: "",
            routingKey: "order-created",
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Published OrderCreated event for OrderId: {OrderId}, UserId: {UserId}",
            message.OrderId, message.UserId);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _disposed = true;
        }
    }
}
