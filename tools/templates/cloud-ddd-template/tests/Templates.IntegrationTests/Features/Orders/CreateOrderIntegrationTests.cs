namespace Templates.Tests.Integration.Features.Orders;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;
using Templates.Tests.Integration.Common;
using Templates.Application.Features.Orders.Create;
using SharedCommon.Core;

/// <summary>
/// Integration tests for order creation.
/// </summary>
public class CreateOrderIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CreateOrderIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_CreateOrder_WithValidRequest_Returns201()
    {
        // Arrange
        var request = new CreateOrderCommand(
            Guid.NewGuid(),
            new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), 2, 99.99m)
            });

        // Act
        var response = await _client.PostAsJsonAsync("/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsAsync<ApiResponse<CreateOrderResponse>>();
        content.Success.Should().BeTrue();
        content.Data.OrderId.Should().NotBe(Guid.Empty);
        content.Data.TotalAmount.Should().Be(199.98m);
    }

    [Fact]
    public async Task Post_CreateOrder_WithEmptyCustomerId_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateOrderCommand(
            Guid.Empty,
            new List<CreateOrderItemDto> { new(Guid.NewGuid(), 1, 100) });

        // Act
        var response = await _client.PostAsJsonAsync("/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
