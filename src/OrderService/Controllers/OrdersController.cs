using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using OrderService.Models;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Order>>>> GetAll()
    {
        _logger.LogInformation("GET /api/orders called");
        var orders = await _orderService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<Order>>.Ok(orders));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        _logger.LogInformation("GET /api/orders/{OrderId} called", id);
        var order = await _orderService.GetByIdAsync(id);

        if (order is null)
        {
            return NotFound(ApiResponse<Order>.Fail($"Order with ID {id} not found"));
        }

        return Ok(ApiResponse<Order>.Ok(order));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Order order)
    {
        _logger.LogInformation("POST /api/orders called");

        try
        {
            var createdOrder = await _orderService.CreateAsync(order);
            return CreatedAtAction(nameof(GetOrder), new { id = createdOrder.Id },
                ApiResponse<Order>.Ok(createdOrder, "Order created successfully"));
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(ex, "gRPC call to UserService failed: {Status}", ex.Status);

            if (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
            {
                return BadRequest(ApiResponse<Order>.Fail($"User not found: {ex.Status.Detail}"));
            }

            return StatusCode(502,
                ApiResponse<Order>.Fail("UserService is currently unavailable via gRPC. Please try again later."));
        }
        catch (Exception ex) when (ex is RabbitMQ.Client.Exceptions.BrokerUnreachableException
                                    or RabbitMQ.Client.Exceptions.ConnectFailureException)
        {
            _logger.LogError(ex, "RabbitMQ is unavailable while creating order");
            return StatusCode(503,
                ApiResponse<Order>.Fail("Message queue is currently unavailable. Please try again later."));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Order order)
    {
        _logger.LogInformation("PUT /api/orders/{OrderId} called", id);
        var updatedOrder = await _orderService.UpdateAsync(id, order);

        if (updatedOrder is null)
        {
            return NotFound(ApiResponse<Order>.Fail($"Order with ID {id} not found"));
        }

        return Ok(ApiResponse<Order>.Ok(updatedOrder, "Order updated successfully"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.LogInformation("DELETE /api/orders/{OrderId} called", id);
        var deleted = await _orderService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound(ApiResponse<Order>.Fail($"Order with ID {id} not found"));
        }

        return Ok(ApiResponse<string>.Ok("Deleted", "Order deleted successfully"));
    }
}
