using OrderService.Models;

namespace OrderService.Services;

public interface IOrderProcessingService : IDisposable
{
    void SubmitOrder(Order order);
}
