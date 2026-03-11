using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;

namespace ProductService.Services;

public class ProductServiceImpl : IProductService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProductServiceImpl> _logger;
    private readonly IUserApiClient _userApiClient;

    public ProductServiceImpl(AppDbContext context, ILogger<ProductServiceImpl> logger, IUserApiClient userApiClient)
    {
        _context = context;
        _logger = logger;
        _userApiClient = userApiClient;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        _logger.LogInformation("Retrieving all products");
        return await _context.Products.AsNoTracking().ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Retrieving product with ID: {ProductId}", id);
        return await _context.Products.FindAsync(id);
    }

    public async Task<Product> CreateAsync(Product product)
    {
        // Validate that the user exists via REST call to UserService
        var userExists = await _userApiClient.UserExistsAsync(product.CreatedByUserId);
        if (!userExists)
        {
            _logger.LogWarning("User with ID: {UserId} not found when creating product", product.CreatedByUserId);
            throw new InvalidOperationException($"User with ID {product.CreatedByUserId} does not exist");
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created product with ID: {ProductId} by User: {UserId}", product.Id, product.CreatedByUserId);
        return product;
    }

    public async Task<Product?> UpdateAsync(Guid id, Product product)
    {
        var existingProduct = await _context.Products.FindAsync(id);
        if (existingProduct is null)
        {
            _logger.LogWarning("Product with ID: {ProductId} not found for update", id);
            return null;
        }

        existingProduct.Name = product.Name;
        existingProduct.Description = product.Description;
        existingProduct.Price = product.Price;
        existingProduct.Stock = product.Stock;
        existingProduct.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated product with ID: {ProductId}", id);
        return existingProduct;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
        {
            _logger.LogWarning("Product with ID: {ProductId} not found for deletion", id);
            return false;
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted product with ID: {ProductId}", id);
        return true;
    }
}
