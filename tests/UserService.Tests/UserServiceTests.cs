using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Data;
using UserService.Models;
using UserService.Services;

namespace UserService.Tests;

public class UserServiceTests
{
    private readonly AppDbContext _context;
    private readonly UserServiceImpl _sut;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        var logger = new Mock<ILogger<UserServiceImpl>>();
        _sut = new UserServiceImpl(_context, logger.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoUsers()
    {
        var result = await _sut.GetAllAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_AddsUserToDatabase()
    {
        var user = new User { Name = "Test User", Email = "test@example.com" };

        var created = await _sut.CreateAsync(user);

        created.Should().NotBeNull();
        created.Name.Should().Be("Test User");
        created.Email.Should().Be("test@example.com");

        var dbUser = await _context.Users.FindAsync(created.Id);
        dbUser.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUser_WhenExists()
    {
        var user = new User { Name = "Found User", Email = "found@example.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(user.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Found User");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesUser_WhenExists()
    {
        var user = new User { Name = "Original", Email = "orig@example.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var updateData = new User { Name = "Updated", Email = "updated@example.com" };
        var result = await _sut.UpdateAsync(user.Id, updateData);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated");
        result.Email.Should().Be("updated@example.com");
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenNotExists()
    {
        var updateData = new User { Name = "Ghost", Email = "ghost@example.com" };
        var result = await _sut.UpdateAsync(Guid.NewGuid(), updateData);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenExists()
    {
        var user = new User { Name = "ToDelete", Email = "del@example.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _sut.DeleteAsync(user.Id);

        result.Should().BeTrue();
        var dbUser = await _context.Users.FindAsync(user.Id);
        dbUser.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotExists()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid());
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        _context.Users.AddRange(
            new User { Name = "User1", Email = "u1@test.com" },
            new User { Name = "User2", Email = "u2@test.com" });
        await _context.SaveChangesAsync();

        var result = await _sut.GetAllAsync();
        result.Should().HaveCount(2);
    }
}
