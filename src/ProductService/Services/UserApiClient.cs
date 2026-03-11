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

            _logger.LogWarning("User {UserId} not found in UserService. Status: {StatusCode}", userId, response.StatusCode);
            return false;
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
