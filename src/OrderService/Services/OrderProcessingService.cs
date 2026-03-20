using System.Reactive.Linq;
using System.Reactive.Subjects;
using OrderService.Models;

namespace OrderService.Services;

public class OrderProcessingService : IOrderProcessingService
{
    private readonly Subject<Order> _orderSubject = new();
    private readonly IDisposable _subscription;
    private readonly ILogger<OrderProcessingService> _logger;
    private bool _disposed;

    public OrderProcessingService(ILogger<OrderProcessingService> logger)
    {
        _logger = logger;

        _subscription = _orderSubject
            .Where(o => o.TotalAmount > 0)
            .Do(o => _logger.LogInformation(
                "Processing order {OrderId} reactively — Amount: {Amount}, User: {UserId}",
                o.Id, o.TotalAmount, o.UserId))
            .Delay(TimeSpan.FromMilliseconds(100))
            .Subscribe(
                onNext: ProcessOrder,
                onError: ex => _logger.LogError(ex, "Error in reactive order stream"));

        _logger.LogInformation("OrderProcessingService Rx.NET stream initialized");
    }

    public void SubmitOrder(Order order)
    {
        _logger.LogInformation("Order {OrderId} submitted to Rx.NET stream", order.Id);
        _orderSubject.OnNext(order);
    }

    private void ProcessOrder(Order order)
    {
        order.Status = "Processing";
        _logger.LogInformation("Order {OrderId} status updated to Processing via Rx.NET", order.Id);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _subscription?.Dispose();
            _orderSubject?.Dispose();
            _disposed = true;
        }
    }
}
