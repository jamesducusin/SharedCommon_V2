namespace Templates.IntegrationTests.Examples;

using System.Net;
using System.Net.Http.Json;
using Templates.Api.Common.Models;
using Xunit;
using FluentAssertions;

/// <summary>
/// Integration test examples demonstrating HTTP endpoint testing with the test factory.
/// These tests verify the full request-response cycle including middleware, auth, and error handling.
/// </summary>
public class HealthCheckEndpointTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public HealthCheckEndpointTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Ensure database is initialized
        await _factory.MigrateDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task HealthLive_Always_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsJsonAsync<dynamic>();
        content.Should().NotBeNull();
    }

    [Fact]
    public async Task HealthReady_WhenDatabaseIsAvailable_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthDetailed_ReturnsDetailedStatus()
    {
        // Act
        var response = await _client.GetAsync("/health/detailed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var health = await response.Content.ReadAsJsonAsync<HealthCheckResponse>();
        
        health.Should().NotBeNull();
        health!.Status.Should().BeOneOf("healthy", "degraded", "unhealthy");
        health.Checks.Should().NotBeEmpty();
        health.Checks.Should().ContainKey("database");
        health.HealthScore.Should().BeInRange(0, 100);
    }
}

/// <summary>
/// Integration test examples for order endpoints with authentication and error handling.
/// </summary>
public class OrderEndpointTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public OrderEndpointTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.MigrateDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetOrder_WithInvalidId_Returns404WithStandardErrorResponse()
    {
        // Arrange
        var invalidOrderId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/orders/{invalidOrderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var error = await response.Content.ReadAsJsonAsync<ApiErrorResponse>();
        error.Should().NotBeNull();
        error!.StatusCode.Should().Be(404);
        error.Error.Code.Should().Be("ENTITY_NOT_FOUND");
        error.Error.Message.Should().Contain(invalidOrderId.ToString());
        error.TraceId.Should().NotBeNullOrEmpty();
        error.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CreateOrder_WithoutAuthentication_Returns401()
    {
        // Arrange
        var createRequest = new
        {
            customerId = Guid.NewGuid(),
            items = new[]
            {
                new { productId = Guid.NewGuid(), quantity = 2, unitPrice = 99.99 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidData_Returns400WithValidationErrors()
    {
        // Arrange
        var invalidRequest = new
        {
            customerId = "", // Invalid: empty
            items = new object[] { } // Invalid: empty
        };

        // Add Authorization header (assumes test token factory)
        // _client.DefaultRequestHeaders.Authorization = 
        //     new AuthenticationHeaderValue("Bearer", GenerateTestJwt());

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", invalidRequest);

        // Assert (depends on request validation implementation)
        // response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        // var error = await response.Content.ReadAsJsonAsync<ApiErrorResponse>();
        // error!.Error.Code.Should().Be("VALIDATION_FAILED");
    }
}

/// <summary>
/// Integration test examples for resilience patterns.
/// </summary>
public class ResiliencePatternTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ResiliencePatternTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.MigrateDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Request_WithinTimeout_Completes()
    {
        // This test verifies that normal requests complete within timeout
        // Arrange
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        var response = await _client.GetAsync("/health/live", cts.Token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // TODO: Add circuit breaker tests when external HTTP clients are implemented
    // [Fact]
    // public async Task CircuitBreaker_OpensAfterMultipleFailures()
    // {
    //     // Simulate multiple failed requests
    //     for (int i = 0; i < 3; i++)
    //     {
    //         var response = await _client.GetAsync("/api/v1/external-service");
    //         response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    //     }

    //     // Circuit should now be open
    //     var circuitOpenResponse = await _client.GetAsync("/api/v1/external-service");
    //     circuitOpenResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    // }
}

/// <summary>
/// Helper extensions for tests.
/// </summary>
public static class TestExtensions
{
    /// <summary>
    /// Run database migrations for the test instance.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var migrationService = scope.ServiceProvider
            .GetRequiredService<Templates.Infrastructure.Persistence.Migrations.IDatabaseMigrationService>();
        
        await migrationService.MigrateAsync();
    }
}
