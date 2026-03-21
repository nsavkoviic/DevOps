namespace E2E.Tests;

/// <summary>
/// E2E tests are designed to be run against a fully running docker-compose environment.
/// Use 'docker compose up -d' before running these tests.
/// 
/// These tests verify the full request pipeline through the API Gateway
/// to the individual microservices.
/// </summary>
public class ApiGatewayE2ETests : IDisposable
{
    private readonly HttpClient _client;

    public ApiGatewayE2ETests()
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000")
        };
    }

    [Fact(Skip = "Requires running docker-compose environment")]
    public async Task HealthCheck_UserService_ReturnsHealthy()
    {
        var response = await _client.GetAsync("http://localhost:5001/health");
        response.EnsureSuccessStatusCode();
    }

    [Fact(Skip = "Requires running docker-compose environment")]
    public async Task HealthCheck_ProductService_ReturnsHealthy()
    {
        var response = await _client.GetAsync("http://localhost:5002/health");
        response.EnsureSuccessStatusCode();
    }

    [Fact(Skip = "Requires running docker-compose environment")]
    public async Task HealthCheck_OrderService_ReturnsHealthy()
    {
        var response = await _client.GetAsync("http://localhost:5003/health");
        response.EnsureSuccessStatusCode();
    }

    [Fact(Skip = "Requires running docker-compose environment")]
    public async Task HealthCheck_NotificationService_ReturnsHealthy()
    {
        var response = await _client.GetAsync("http://localhost:5004/health");
        response.EnsureSuccessStatusCode();
    }

    [Fact(Skip = "Requires running docker-compose environment")]
    public async Task Gateway_GetUsers_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/users");
        response.EnsureSuccessStatusCode();
    }

    [Fact(Skip = "Requires running docker-compose environment")]
    public async Task Gateway_GetProducts_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/products");
        response.EnsureSuccessStatusCode();
    }

    [Fact(Skip = "Requires running docker-compose environment")]
    public async Task Gateway_GetOrders_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/orders");
        response.EnsureSuccessStatusCode();
    }

    [Fact(Skip = "Requires running docker-compose environment")]
    public async Task Gateway_FullFlow_CreateUserThenProduct()
    {
        // Step 1: Create user via gateway
        var userPayload = new StringContent(
            """{"name":"E2E User","email":"e2e@test.com"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var userResponse = await _client.PostAsync("/api/users", userPayload);
        userResponse.EnsureSuccessStatusCode();

        // Step 2: Create product via gateway (requires valid user)
        var productPayload = new StringContent(
            """{"name":"E2E Product","description":"Test","price":99.99,"stock":10,"createdByUserId":"<replace-with-user-id>"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        // Note: In a real E2E test, parse the userId from Step 1 response
        // and replace the placeholder above.
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
