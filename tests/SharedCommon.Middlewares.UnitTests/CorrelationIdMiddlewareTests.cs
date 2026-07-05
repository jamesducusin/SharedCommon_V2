namespace SharedCommon.Middlewares.UnitTests;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;
using SharedCommon.Core;

/// <summary>
/// Behavioral tests for <see cref="CorrelationIdMiddleware"/>.
/// Verifies correlation ID generation, propagation, and response header behavior.
/// </summary>
public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithProvidedHeader_UsesProvidedId()
    {
        // Arrange
        const string providedId = "test-correlation-123";
        var context = CreateHttpContext();
        context.Request.Headers["X-Correlation-ID"] = providedId;

        var options = Options.Create(new MiddlewareOptions
        {
            CorrelationId = new CorrelationIdMiddlewareOptions
            {
                Enabled = true,
                HeaderName = "X-Correlation-ID",
                GenerateIfMissing = true
            }
        });

        var nextCalled = false;
        var middleware = new CorrelationIdMiddleware(
            ctx => { nextCalled = true; return Task.CompletedTask; },
            options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(providedId, context.Response.Headers["X-Correlation-ID"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_WithoutHeader_GeneratesNewId()
    {
        // Arrange
        var context = CreateHttpContext();
        var options = Options.Create(new MiddlewareOptions
        {
            CorrelationId = new CorrelationIdMiddlewareOptions
            {
                Enabled = true,
                HeaderName = "X-Correlation-ID",
                GenerateIfMissing = true
            }
        });

        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseId = context.Response.Headers["X-Correlation-ID"].ToString();
        Assert.NotEmpty(responseId);
        Assert.True(Guid.TryParse(responseId, out _), $"Generated ID '{responseId}' should be a valid GUID");
    }

    [Fact]
    public async Task InvokeAsync_WritesCorrelationIdToResponseHeader()
    {
        // Arrange
        const string providedId = "response-header-test";
        var context = CreateHttpContext();
        context.Request.Headers["X-Correlation-ID"] = providedId;

        var options = Options.Create(new MiddlewareOptions
        {
            CorrelationId = new CorrelationIdMiddlewareOptions
            {
                Enabled = true,
                HeaderName = "X-Correlation-ID",
                GenerateIfMissing = true
            }
        });

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask, options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.TryGetValue("X-Correlation-ID", out var values));
        Assert.Equal(providedId, values.ToString());
    }

    [Fact]
    public async Task InvokeAsync_PopulatesRequestContextWhenAvailable()
    {
        // Arrange
        const string providedId = "context-population-test";
        var context = CreateHttpContext();
        context.Request.Headers["X-Correlation-ID"] = providedId;

        var requestContext = new RequestContext();
        context.RequestServices = Substitute.For<IServiceProvider>();
        context.RequestServices.GetService(typeof(IRequestContext)).Returns(requestContext);

        var options = Options.Create(new MiddlewareOptions
        {
            CorrelationId = new CorrelationIdMiddlewareOptions
            {
                Enabled = true,
                HeaderName = "X-Correlation-ID",
                GenerateIfMissing = true
            }
        });

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask, options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.NotNull(requestContext.CorrelationId);
        Assert.Equal(providedId, requestContext.CorrelationId.Value);
    }

    [Fact]
    public async Task InvokeAsync_WithGenerateIfMissingFalse_SkipsWhenHeaderAbsent()
    {
        // Arrange
        var context = CreateHttpContext();
        var options = Options.Create(new MiddlewareOptions
        {
            CorrelationId = new CorrelationIdMiddlewareOptions
            {
                Enabled = true,
                HeaderName = "X-Correlation-ID",
                GenerateIfMissing = false
            }
        });

        var nextCalled = false;
        var middleware = new CorrelationIdMiddleware(
            ctx => { nextCalled = true; return Task.CompletedTask; },
            options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.False(context.Response.Headers.ContainsKey("X-Correlation-ID"));
    }

    [Fact]
    public async Task InvokeAsync_WithCustomHeaderName_UsesCustomHeader()
    {
        // Arrange
        const string customHeaderName = "X-Request-ID";
        const string customId = "custom-header-test";
        var context = CreateHttpContext();
        context.Request.Headers[customHeaderName] = customId;

        var options = Options.Create(new MiddlewareOptions
        {
            CorrelationId = new CorrelationIdMiddlewareOptions
            {
                Enabled = true,
                HeaderName = customHeaderName,
                GenerateIfMissing = true
            }
        });

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask, options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(customId, context.Response.Headers[customHeaderName].ToString());
    }

    [Fact]
    public async Task InvokeAsync_AllowsNextMiddlewareToExecute()
    {
        // Arrange
        var context = CreateHttpContext();
        var nextCalled = false;
        var options = Options.Create(new MiddlewareOptions
        {
            CorrelationId = new CorrelationIdMiddlewareOptions
            {
                Enabled = true,
                HeaderName = "X-Correlation-ID",
                GenerateIfMissing = true
            }
        });

        var middleware = new CorrelationIdMiddleware(
            ctx => { nextCalled = true; return Task.CompletedTask; },
            options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/";
        context.RequestServices = Substitute.For<IServiceProvider>();
        return context;
    }
}
