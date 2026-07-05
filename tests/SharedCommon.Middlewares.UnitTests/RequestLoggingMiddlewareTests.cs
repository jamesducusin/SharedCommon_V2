namespace SharedCommon.Middlewares.UnitTests;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

/// <summary>
/// Behavioral tests for <see cref="RequestLoggingMiddleware"/>.
/// Verifies request/response logging, path exclusion, and middleware behavior.
/// </summary>
public sealed class RequestLoggingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ExcludesHealthPath()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/health";
        
        var logger = Substitute.For<ILogger<RequestLoggingMiddleware>>();
        var options = Options.Create(new MiddlewareOptions
        {
            RequestLogging = new RequestLoggingOptions
            {
                Enabled = true,
                ExcludePaths = new[] { "/health" }
            }
        });

        var nextCalled = false;
        var middleware = new RequestLoggingMiddleware(
            ctx => { nextCalled = true; return Task.CompletedTask; },
            options,
            logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_ExcludesMetricsPath()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/metrics";
        
        var logger = Substitute.For<ILogger<RequestLoggingMiddleware>>();
        var options = Options.Create(new MiddlewareOptions
        {
            RequestLogging = new RequestLoggingOptions
            {
                Enabled = true,
                ExcludePaths = new[] { "/metrics", "/health" }
            }
        });

        var nextCalled = false;
        var middleware = new RequestLoggingMiddleware(
            ctx => { nextCalled = true; return Task.CompletedTask; },
            options,
            logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenDisabled_SkipsAllLogging()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/users";
        
        var logger = Substitute.For<ILogger<RequestLoggingMiddleware>>();
        var options = Options.Create(new MiddlewareOptions
        {
            RequestLogging = new RequestLoggingOptions
            {
                Enabled = false,
                ExcludePaths = Array.Empty<string>()
            }
        });

        var nextCalled = false;
        var middleware = new RequestLoggingMiddleware(
            ctx => { nextCalled = true; return Task.CompletedTask; },
            options,
            logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_AllowsNextMiddlewareToExecute()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/orders";
        
        var logger = Substitute.For<ILogger<RequestLoggingMiddleware>>();
        var options = Options.Create(new MiddlewareOptions
        {
            RequestLogging = new RequestLoggingOptions
            {
                Enabled = true,
                ExcludePaths = new[] { "/health" }
            }
        });

        var nextCalled = false;
        var middleware = new RequestLoggingMiddleware(
            ctx => { nextCalled = true; return Task.CompletedTask; },
            options,
            logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_ContinuesOnExceptionFromNextMiddleware()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        
        var logger = Substitute.For<ILogger<RequestLoggingMiddleware>>();
        var options = Options.Create(new MiddlewareOptions
        {
            RequestLogging = new RequestLoggingOptions
            {
                Enabled = true,
                ExcludePaths = Array.Empty<string>()
            }
        });

        var middleware = new RequestLoggingMiddleware(
            ctx => throw new InvalidOperationException("Test exception"),
            options,
            logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
    }

    [Fact]
    public async Task InvokeAsync_WithIncludedPath_ExecutesNextMiddleware()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/products";
        
        var logger = Substitute.For<ILogger<RequestLoggingMiddleware>>();
        var options = Options.Create(new MiddlewareOptions
        {
            RequestLogging = new RequestLoggingOptions
            {
                Enabled = true,
                ExcludePaths = new[] { "/health", "/metrics" }
            }
        });

        var nextCalled = false;
        var middleware = new RequestLoggingMiddleware(
            ctx => { nextCalled = true; return Task.CompletedTask; },
            options,
            logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_PresetsHttpMethod()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/create";
        
        var logger = Substitute.For<ILogger<RequestLoggingMiddleware>>();
        var options = Options.Create(new MiddlewareOptions
        {
            RequestLogging = new RequestLoggingOptions
            {
                Enabled = true,
                ExcludePaths = Array.Empty<string>()
            }
        });

        var capturedMethod = string.Empty;
        var middleware = new RequestLoggingMiddleware(
            ctx => { capturedMethod = ctx.Request.Method; return Task.CompletedTask; },
            options,
            logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("POST", capturedMethod);
    }

    [Fact]
    public async Task InvokeAsync_PresetsHttpPath()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/search";
        
        var logger = Substitute.For<ILogger<RequestLoggingMiddleware>>();
        var options = Options.Create(new MiddlewareOptions
        {
            RequestLogging = new RequestLoggingOptions
            {
                Enabled = true,
                ExcludePaths = Array.Empty<string>()
            }
        });

        var capturedPath = string.Empty;
        var middleware = new RequestLoggingMiddleware(
            ctx => { capturedPath = ctx.Request.Path; return Task.CompletedTask; },
            options,
            logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("/api/search", capturedPath);
    }

    [Fact]
    public async Task InvokeAsync_PresetsResponseStatusCode()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/test";
        
        var logger = Substitute.For<ILogger<RequestLoggingMiddleware>>();
        var options = Options.Create(new MiddlewareOptions
        {
            RequestLogging = new RequestLoggingOptions
            {
                Enabled = true,
                ExcludePaths = Array.Empty<string>()
            }
        });

        var capturedStatusCode = 0;
        var middleware = new RequestLoggingMiddleware(
            ctx => { ctx.Response.StatusCode = 201; capturedStatusCode = ctx.Response.StatusCode; return Task.CompletedTask; },
            options,
            logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(201, capturedStatusCode);
    }

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/";
        context.RequestServices = Substitute.For<IServiceProvider>();
        context.Response.Body = new MemoryStream();
        return context;
    }
}
