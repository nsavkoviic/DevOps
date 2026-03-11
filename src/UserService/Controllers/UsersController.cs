using Microsoft.AspNetCore.Mvc;
using UserService.Models;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<User>>>> GetAll()
    {
        _logger.LogInformation("GET /api/users called");
        var users = await _userService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<User>>.Ok(users));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        _logger.LogInformation("GET /api/users/{UserId} called", id);
        var user = await _userService.GetByIdAsync(id);

        if (user is null)
        {
            return NotFound(ApiResponse<User>.Fail($"User with ID {id} not found"));
        }

        return Ok(ApiResponse<User>.Ok(user));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<User>>> Create([FromBody] User user)
    {
        _logger.LogInformation("POST /api/users called");
        var createdUser = await _userService.CreateAsync(user);
        return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id },
            ApiResponse<User>.Ok(createdUser, "User created successfully"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] User user)
    {
        _logger.LogInformation("PUT /api/users/{UserId} called", id);
        var updatedUser = await _userService.UpdateAsync(id, user);

        if (updatedUser is null)
        {
            return NotFound(ApiResponse<User>.Fail($"User with ID {id} not found"));
        }

        return Ok(ApiResponse<User>.Ok(updatedUser, "User updated successfully"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.LogInformation("DELETE /api/users/{UserId} called", id);
        var deleted = await _userService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound(ApiResponse<User>.Fail($"User with ID {id} not found"));
        }

        return Ok(ApiResponse<string>.Ok("Deleted", "User deleted successfully"));
    }
}
