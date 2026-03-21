using Microsoft.AspNetCore.Mvc;
using ProductService.Models;
using ProductService.Services;

namespace ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Product>>>> GetAll()
    {
        _logger.LogInformation("GET /api/products called");
        var products = await _productService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<Product>>.Ok(products));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        _logger.LogInformation("GET /api/products/{ProductId} called", id);
        var product = await _productService.GetByIdAsync(id);

        if (product is null)
        {
            return NotFound(ApiResponse<Product>.Fail($"Product with ID {id} not found"));
        }

        return Ok(ApiResponse<Product>.Ok(product));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        _logger.LogInformation("POST /api/products called");

        try
        {
            var createdProduct = await _productService.CreateAsync(product);
            return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id },
                ApiResponse<Product>.Ok(createdProduct, "Product created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create product: {Message}", ex.Message);
            return BadRequest(ApiResponse<Product>.Fail(ex.Message));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "UserService is unavailable while creating product");
            return StatusCode(StatusCodes.Status502BadGateway,
                ApiResponse<Product>.Fail("UserService is currently unavailable. Please try again later."));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Product product)
    {
        _logger.LogInformation("PUT /api/products/{ProductId} called", id);
        var updatedProduct = await _productService.UpdateAsync(id, product);

        if (updatedProduct is null)
        {
            return NotFound(ApiResponse<Product>.Fail($"Product with ID {id} not found"));
        }

        return Ok(ApiResponse<Product>.Ok(updatedProduct, "Product updated successfully"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.LogInformation("DELETE /api/products/{ProductId} called", id);
        var deleted = await _productService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound(ApiResponse<Product>.Fail($"Product with ID {id} not found"));
        }

        return Ok(ApiResponse<string>.Ok("Deleted", "Product deleted successfully"));
    }
}
