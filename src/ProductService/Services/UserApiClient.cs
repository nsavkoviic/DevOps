namespace ProductService.Services;

public class UserApiClient : IUserApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserApiClient> _logger;

    public UserApiClient(HttpClient httpClient, ILogger<UserApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> UserExistsAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation("Checking if user {UserId} exists via REST call to UserService", userId);
            using var response = await _httpClient.GetAsync($"/api/users/{userId}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("User {UserId} found in UserService", userId);
                return true;
            }

            // Only treat 404/410 as "user not found"
            if (response.StatusCode is System.Net.HttpStatusCode.NotFound
                or System.Net.HttpStatusCode.Gone)
            {
                _logger.LogWarning("User {UserId} not found in UserService. Status: {StatusCode}", userId, response.StatusCode);
                return false;
            }

            // For all other non-success codes (5xx, 401, 403, etc.), throw to signal upstream failure
            _logger.LogError("Unexpected response from UserService for user {UserId}. Status: {StatusCode}", userId, response.StatusCode);
            response.EnsureSuccessStatusCode();
            return false; // unreachable, but satisfies compiler
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error communicating with UserService for user {UserId}", userId);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request to UserService timed out for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error communicating with UserService for user {UserId}", userId);
            throw;
        }
    }
}
