using System.Text;
using System.Text.Json;
using NotificationService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Workers;

public class NotificationWorker : BackgroundService
{
    private readonly ILogger<NotificationWorker> _logger;
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IModel? _channel;

    public NotificationWorker(ILogger<NotificationWorker> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationWorker starting — connecting to RabbitMQ...");

        stoppingToken.Register(() =>
        {
            _logger.LogInformation("NotificationWorker stopping — disposing RabbitMQ resources");
            _channel?.Dispose();
            _connection?.Dispose();
        });

        // Retry connection with delay to handle startup ordering
        _ = ConnectWithRetryAsync(stoppingToken);

        return Task.CompletedTask;
    }

    private async Task ConnectWithRetryAsync(CancellationToken stoppingToken)
    {
        var maxRetries = 10;
        var retryDelay = TimeSpan.FromSeconds(5);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _config["RabbitMQ:Host"] ?? "localhost",
                    UserName = _config["RabbitMQ:Username"] ?? "devops",
                    Password = _config["RabbitMQ:Password"] ?? "devops123"
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(
                    queue: "order-created",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        var message = JsonSerializer.Deserialize<OrderCreatedMessage>(json);

                        if (message is not null)
                        {
                            _logger.LogInformation(
                                "📧 Notification sent for OrderId: {OrderId}, User: {UserId}, Product: {ProductName}, Amount: {Amount}",
                                message.OrderId, message.UserId, message.ProductName, message.TotalAmount);
                        }
                        else
                        {
                            _logger.LogWarning("Received null message from order-created queue");
                        }

                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message from order-created queue");
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                _channel.BasicConsume(
                    queue: "order-created",
                    autoAck: false,
                    consumer: consumer);

                _logger.LogInformation("NotificationWorker connected to RabbitMQ and consuming from 'order-created' queue");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to connect to RabbitMQ (attempt {Attempt}/{MaxRetries}). Retrying in {Delay}s...",
                    attempt, maxRetries, retryDelay.TotalSeconds);

                if (attempt == maxRetries)
                {
                    _logger.LogError("Failed to connect to RabbitMQ after {MaxRetries} attempts", maxRetries);
                    return;
                }

                await Task.Delay(retryDelay, stoppingToken);
            }
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
