using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProductService.Data;
using ProductService.Models;
using ProductService.Services;

namespace ProductService.Tests;

public class ProductServiceTests
{
    private readonly AppDbContext _context;
    private readonly ProductServiceImpl _sut;
    private readonly Mock<IUserApiClient> _userApiClientMock;

    public ProductServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        var logger = new Mock<ILogger<ProductServiceImpl>>();
        _userApiClientMock = new Mock<IUserApiClient>();
        _sut = new ProductServiceImpl(_context, logger.Object, _userApiClientMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoProducts()
    {
        var result = await _sut.GetAllAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_AddsProduct_WhenUserExists()
    {
        var userId = Guid.NewGuid();
        _userApiClientMock.Setup(x => x.UserExistsAsync(userId)).ReturnsAsync(true);

        var product = new Product
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 29.99m,
            Stock = 100,
            CreatedByUserId = userId
        };

        var created = await _sut.CreateAsync(product);

        created.Should().NotBeNull();
        created.Name.Should().Be("Test Product");
        created.Price.Should().Be(29.99m);

        var dbProduct = await _context.Products.FindAsync(created.Id);
        dbProduct.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_ThrowsInvalidOperation_WhenUserNotExists()
    {
        var userId = Guid.NewGuid();
        _userApiClientMock.Setup(x => x.UserExistsAsync(userId)).ReturnsAsync(false);

        var product = new Product
        {
            Name = "Orphan Product",
            Price = 10.00m,
            Stock = 5,
            CreatedByUserId = userId
        };

        var act = async () => await _sut.CreateAsync(product);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{userId}*");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsProduct_WhenExists()
    {
        var product = new Product { Name = "Found", Price = 50m, Stock = 10, CreatedByUserId = Guid.NewGuid() };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(product.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Found");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesProduct_WhenExists()
    {
        var product = new Product { Name = "Original", Price = 10m, Stock = 5, CreatedByUserId = Guid.NewGuid() };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var updateData = new Product { Name = "Updated", Description = "New desc", Price = 20m, Stock = 50 };
        var result = await _sut.UpdateAsync(product.Id, updateData);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated");
        result.Price.Should().Be(20m);
        result.Stock.Should().Be(50);
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenNotExists()
    {
        var updateData = new Product { Name = "Ghost", Price = 0m, Stock = 0 };
        var result = await _sut.UpdateAsync(Guid.NewGuid(), updateData);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenExists()
    {
        var product = new Product { Name = "ToDelete", Price = 5m, Stock = 1, CreatedByUserId = Guid.NewGuid() };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var result = await _sut.DeleteAsync(product.Id);

        result.Should().BeTrue();
        var dbProduct = await _context.Products.FindAsync(product.Id);
        dbProduct.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotExists()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid());
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_CallsUserApiClient()
    {
        var userId = Guid.NewGuid();
        _userApiClientMock.Setup(x => x.UserExistsAsync(userId)).ReturnsAsync(true);

        var product = new Product { Name = "VerifyCall", Price = 1m, Stock = 1, CreatedByUserId = userId };
        await _sut.CreateAsync(product);

        _userApiClientMock.Verify(x => x.UserExistsAsync(userId), Times.Once);
    }
}
