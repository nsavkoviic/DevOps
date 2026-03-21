using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace E2E.Tests;

/// <summary>
/// E2E tests run against a fully running docker-compose environment.
/// Set the environment variable RUN_E2E_TESTS=true before running.
/// 
/// Usage:
///   docker compose up -d
///   $env:RUN_E2E_TESTS="true"
///   dotnet test tests/E2E.Tests/
/// </summary>
[Trait("Category", "E2E")]
public class ApiGatewayE2ETests : IDisposable
{
    private readonly HttpClient _client;

    private static string? SkipReason =>
        Environment.GetEnvironmentVariable("RUN_E2E_TESTS")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true
            ? null
            : "Set RUN_E2E_TESTS=true to run E2E tests (requires docker-compose)";

    public ApiGatewayE2ETests()
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000"),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    [SkippableFact]
    public async Task HealthCheck_UserService_ReturnsHealthy()
    {
        Skip.If(SkipReason is not null, SkipReason);

        var response = await _client.GetAsync("http://localhost:5001/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task HealthCheck_ProductService_ReturnsHealthy()
    {
        Skip.If(SkipReason is not null, SkipReason);

        var response = await _client.GetAsync("http://localhost:5002/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task HealthCheck_OrderService_ReturnsHealthy()
    {
        Skip.If(SkipReason is not null, SkipReason);

        var response = await _client.GetAsync("http://localhost:5003/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task HealthCheck_NotificationService_ReturnsHealthy()
    {
        Skip.If(SkipReason is not null, SkipReason);

        var response = await _client.GetAsync("http://localhost:5004/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task Gateway_GetUsers_ReturnsSuccess()
    {
        Skip.If(SkipReason is not null, SkipReason);

        var response = await _client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task Gateway_GetProducts_ReturnsSuccess()
    {
        Skip.If(SkipReason is not null, SkipReason);

        var response = await _client.GetAsync("/api/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task Gateway_GetOrders_ReturnsSuccess()
    {
        Skip.If(SkipReason is not null, SkipReason);

        var response = await _client.GetAsync("/api/orders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task Gateway_FullFlow_CreateUserThenProduct()
    {
        Skip.If(SkipReason is not null, SkipReason);

        // Step 1: Create a user via the gateway
        var userPayload = new StringContent(
            """{"name":"E2E User","email":"e2e@test.com"}""",
            Encoding.UTF8,
            "application/json");

        var userResponse = await _client.PostAsync("/api/users", userPayload);
        userResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var userBody = await userResponse.Content.ReadAsStringAsync();
        var userResult = JsonSerializer.Deserialize<ApiResponse<UserDto>>(userBody, JsonOpts);
        userResult.Should().NotBeNull();
        userResult!.Success.Should().BeTrue();
        userResult.Data.Should().NotBeNull();
        userResult.Data!.Id.Should().NotBeEmpty();

        var userId = userResult.Data.Id;

        // Step 2: Create a product associated with that user
        var productJson = JsonSerializer.Serialize(new
        {
            name = "E2E Product",
            description = "Created via E2E test",
            price = 49.99,
            stock = 10,
            createdByUserId = userId
        });
        var productPayload = new StringContent(productJson, Encoding.UTF8, "application/json");

        var productResponse = await _client.PostAsync("/api/products", productPayload);
        productResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var productBody = await productResponse.Content.ReadAsStringAsync();
        var productResult = JsonSerializer.Deserialize<ApiResponse<ProductDto>>(productBody, JsonOpts);
        productResult.Should().NotBeNull();
        productResult!.Success.Should().BeTrue();
        productResult.Data.Should().NotBeNull();
        productResult.Data!.Name.Should().Be("E2E Product");
        productResult.Data.CreatedByUserId.Should().Be(userId);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    // ── JSON helpers ─────────────────────────────────────────────
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    private sealed class UserDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private sealed class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid CreatedByUserId { get; set; }
    }
}
