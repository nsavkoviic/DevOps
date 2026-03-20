using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using UserService.Protos;

namespace OrderService.Services;

public class OrderServiceImpl : IOrderService
{
    private readonly AppDbContext _context;
    private readonly ILogger<OrderServiceImpl> _logger;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IOrderProcessingService _orderProcessingService;
    private readonly IConfiguration _configuration;

    public OrderServiceImpl(
        AppDbContext context,
        ILogger<OrderServiceImpl> logger,
        IMessagePublisher messagePublisher,
        IOrderProcessingService orderProcessingService,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _messagePublisher = messagePublisher;
        _orderProcessingService = orderProcessingService;
        _configuration = configuration;
    }

    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        _logger.LogInformation("Retrieving all orders");
        return await _context.Orders.AsNoTracking().ToListAsync();
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Retrieving order with ID: {OrderId}", id);
        return await _context.Orders.FindAsync(id);
    }

    public async Task<Order> CreateAsync(Order order)
    {
        // Step 1: Validate user via gRPC call to UserService
        var userName = await GetUserNameViaGrpcAsync(order.UserId);
        _logger.LogInformation("gRPC validated user: {UserName} for order creation", userName);

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created order with ID: {OrderId} for User: {UserId}", order.Id, order.UserId);

        // Step 3: Submit to Rx.NET reactive processing pipeline
        _orderProcessingService.SubmitOrder(order);

        // Step 4: Publish OrderCreated event to RabbitMQ
        var message = new OrderCreatedMessage
        {
            OrderId = order.Id,
            UserId = order.UserId,
            ProductName = order.ProductName,
            Quantity = order.Quantity,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt
        };
        _messagePublisher.PublishOrderCreated(message);

        return order;
    }

    public async Task<Order?> UpdateAsync(Guid id, Order order)
    {
        var existingOrder = await _context.Orders.FindAsync(id);
        if (existingOrder is null)
        {
            _logger.LogWarning("Order with ID: {OrderId} not found for update", id);
            return null;
        }

        existingOrder.ProductName = order.ProductName;
        existingOrder.Quantity = order.Quantity;
        existingOrder.TotalAmount = order.TotalAmount;
        existingOrder.Status = order.Status;
        existingOrder.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated order with ID: {OrderId}", id);
        return existingOrder;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order is null)
        {
            _logger.LogWarning("Order with ID: {OrderId} not found for deletion", id);
            return false;
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted order with ID: {OrderId}", id);
        return true;
    }

    private async Task<string> GetUserNameViaGrpcAsync(Guid userId)
    {
        var grpcAddress = _configuration["Services:UserServiceGrpc"] ?? "http://localhost:5011";
        _logger.LogInformation("Calling UserService gRPC at {GrpcAddress} for user {UserId}", grpcAddress, userId);

        using var channel = GrpcChannel.ForAddress(grpcAddress);
        var client = new UserGrpc.UserGrpcClient(channel);

        var reply = await client.GetUserAsync(new GetUserRequest { Id = userId.ToString() });
        _logger.LogInformation("gRPC response received — User: {UserName}, Email: {Email}", reply.Name, reply.Email);

        return reply.Name;
    }
}
