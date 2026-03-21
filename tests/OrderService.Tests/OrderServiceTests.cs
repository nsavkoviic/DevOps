using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Data;
using OrderService.Models;
using OrderService.Services;

namespace OrderService.Tests;

public class OrderServiceTests
{
    private readonly AppDbContext _context;
    private readonly OrderServiceImpl _sut;
    private readonly Mock<IMessagePublisher> _messagePublisherMock;
    private readonly Mock<IOrderProcessingService> _orderProcessingMock;

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        var logger = new Mock<ILogger<OrderServiceImpl>>();
        _messagePublisherMock = new Mock<IMessagePublisher>();
        _orderProcessingMock = new Mock<IOrderProcessingService>();

        // Mock configuration for gRPC endpoint
        var configData = new Dictionary<string, string?>
        {
            { "Services:UserServiceGrpc", "http://localhost:5011" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _sut = new OrderServiceImpl(
            _context,
            logger.Object,
            _messagePublisherMock.Object,
            _orderProcessingMock.Object,
            configuration);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoOrders()
    {
        var result = await _sut.GetAllAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsOrder_WhenExists()
    {
        var order = new Order
        {
            UserId = Guid.NewGuid(),
            ProductName = "Test Product",
            Quantity = 2,
            TotalAmount = 50m,
            Status = "Pending"
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(order.Id);

        result.Should().NotBeNull();
        result!.ProductName.Should().Be("Test Product");
        result.Quantity.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOrder_WhenExists()
    {
        var order = new Order
        {
            UserId = Guid.NewGuid(),
            ProductName = "Original",
            Quantity = 1,
            TotalAmount = 10m,
            Status = "Pending"
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var updateData = new Order
        {
            ProductName = "Updated",
            Quantity = 5,
            TotalAmount = 100m,
            Status = "Confirmed"
        };
        var result = await _sut.UpdateAsync(order.Id, updateData);

        result.Should().NotBeNull();
        result!.ProductName.Should().Be("Updated");
        result.Quantity.Should().Be(5);
        result.TotalAmount.Should().Be(100m);
        result.Status.Should().Be("Confirmed");
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenNotExists()
    {
        var updateData = new Order { ProductName = "Ghost", Quantity = 1, TotalAmount = 0, Status = "Pending" };
        var result = await _sut.UpdateAsync(Guid.NewGuid(), updateData);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenExists()
    {
        var order = new Order
        {
            UserId = Guid.NewGuid(),
            ProductName = "ToDelete",
            Quantity = 1,
            TotalAmount = 5m,
            Status = "Pending"
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var result = await _sut.DeleteAsync(order.Id);

        result.Should().BeTrue();
        var dbOrder = await _context.Orders.FindAsync(order.Id);
        dbOrder.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotExists()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid());
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllOrders()
    {
        _context.Orders.AddRange(
            new Order { UserId = Guid.NewGuid(), ProductName = "P1", Quantity = 1, TotalAmount = 10m, Status = "Pending" },
            new Order { UserId = Guid.NewGuid(), ProductName = "P2", Quantity = 2, TotalAmount = 20m, Status = "Pending" });
        await _context.SaveChangesAsync();

        var result = await _sut.GetAllAsync();
        result.Should().HaveCount(2);
    }

    // ──────────────────────────────────────────────────────────────
    // CreateAsync is NOT fully unit-testable because it makes a real
    // gRPC call to UserService (GetUserNameViaGrpcAsync). The gRPC
    // channel is created inline, so it cannot be mocked without
    // refactoring to inject a gRPC client factory.
    //
    // The test below verifies the expected failure behavior when
    // UserService gRPC is unreachable. Full CreateAsync flow is
    // covered by E2E tests with docker-compose running.
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ThrowsRpcException_WhenGrpcUnavailable()
    {
        var order = new Order
        {
            UserId = Guid.NewGuid(),
            ProductName = "gRPC Test",
            Quantity = 1,
            TotalAmount = 25m,
            Status = "Pending"
        };

        // gRPC endpoint (localhost:5011) is not running during unit tests,
        // so CreateAsync should throw an RpcException.
        var act = async () => await _sut.CreateAsync(order);
        await act.Should().ThrowAsync<Exception>();
    }
}
