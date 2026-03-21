using OrderService.Models;

namespace OrderService.Services;

public interface IMessagePublisher
{
    void PublishOrderCreated(OrderCreatedMessage message);
}
