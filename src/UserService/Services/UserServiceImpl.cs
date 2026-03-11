using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Models;

namespace UserService.Services;

public class UserServiceImpl : IUserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserServiceImpl> _logger;

    public UserServiceImpl(AppDbContext context, ILogger<UserServiceImpl> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        _logger.LogInformation("Retrieving all users");
        return await _context.Users.AsNoTracking().ToListAsync();
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Retrieving user with ID: {UserId}", id);
        return await _context.Users.FindAsync(id);
    }

    public async Task<User> CreateAsync(User user)
    {
        user.Id = Guid.NewGuid();
        user.CreatedAt = DateTime.UtcNow;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created user with ID: {UserId}", user.Id);
        return user;
    }

    public async Task<User?> UpdateAsync(Guid id, User user)
    {
        var existingUser = await _context.Users.FindAsync(id);
        if (existingUser is null)
        {
            _logger.LogWarning("User with ID: {UserId} not found for update", id);
            return null;
        }

        existingUser.Name = user.Name;
        existingUser.Email = user.Email;
        existingUser.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated user with ID: {UserId}", id);
        return existingUser;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null)
        {
            _logger.LogWarning("User with ID: {UserId} not found for deletion", id);
            return false;
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted user with ID: {UserId}", id);
        return true;
    }
}
