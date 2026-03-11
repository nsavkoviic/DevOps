namespace ProductService.Services;

public interface IUserApiClient
{
    Task<bool> UserExistsAsync(Guid userId);
}
