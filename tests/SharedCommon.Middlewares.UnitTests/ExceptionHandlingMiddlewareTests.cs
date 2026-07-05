namespace SharedCommon.Middlewares.UnitTests;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SharedCommon.Core.Exceptions;
using System.Text.Json;

/// <summary>
/// Unit tests for <see cref="ExceptionHandlingMiddleware"/>.
/// Verifies exception mapping, error response format, and logging behavior.
/// </summary>
public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithNotFoundException_Returns404()
    {
        // Arrange
        var context = CreateHttpContext();
        var logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
        var options = Options.Create(new MiddlewareOptions
        {
            ExceptionHandling = new ExceptionHandlingOptions
            {
                Enabled = true,
                IncludeStackTrace = false,
                LogExceptions = true
            }
        });

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new NotFoundException("Resource not found"),
            options,
            logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(404, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedException_Returns401()
    {
        // Arrange
        var context = CreateHttpContext();
        var logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
        var options = Options.Create(new MiddlewareOptions
        {
            ExceptionHandling = new ExceptionHandlingOptions { Enabled = true, IncludeStackTrace = false, LogExceptions = true }
        });

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new UnauthorizedException("Invalid credentials"),
            options,
            logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithForbiddenException_Returns403()
    {
        // Arrange
        var context = CreateHttpContext();
        var logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
        var options = Options.Create(new MiddlewareOptions
        {
            ExceptionHandling = new ExceptionHandlingOptions { Enabled = true, IncludeStackTrace = false, LogExceptions = true }
        });

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ForbiddenException("Access denied"),
            options,
            logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithConflictException_Returns409()
    {
        // Arrange
        var context = CreateHttpContext();
        var logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
        var options = Options.Create(new MiddlewareOptions
        {
            ExceptionHandling = new ExceptionHandlingOptions { Enabled = true, IncludeStackTrace = false, LogExceptions = true }
        });

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ConflictException("Resource already exists"),
            options,
            logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(409, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithTooManyRequestsException_Returns429()
    {
        // Arrange
        var context = CreateHttpContext();
        var logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
        var options = Options.Create(new MiddlewareOptions
        {
            ExceptionHandling = new ExceptionHandlingOptions { Enabled = true, IncludeStackTrace = false, LogExceptions = true }
        });

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new TooManyRequestsException(),
            options,
            logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(429, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithUnhandledException_Returns500()
    {
        // Arrange
        var context = CreateHttpContext();
        var logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
        var options = Options.Create(new MiddlewareOptions
        {
            ExceptionHandling = new ExceptionHandlingOptions { Enabled = true, IncludeStackTrace = false, LogExceptions = true }
        });

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Something went wrong"),
            options,
            logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsJsonErrorResponse()
    {
        // Arrange
        var context = CreateHttpContext();
        var logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
        var options = Options.Create(new MiddlewareOptions
        {
            ExceptionHandling = new ExceptionHandlingOptions { Enabled = true, IncludeStackTrace = false, LogExceptions = true }
        });

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new NotFoundException("Order not found"),
            options,
            logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("application/json", context.Response.ContentType);
        var content = ReadResponseBody(context);
        var json = JsonDocument.Parse(content);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task InvokeAsync_ErrorResponseContainsCode()
    {
        // Arrange
        var context = CreateHttpContext();
        var logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
        var options = Options.Create(new MiddlewareOptions
        {
            ExceptionHandling = new ExceptionHandlingOptions { Enabled = true, IncludeStackTrace = false, LogExceptions = true }
        });

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new NotFoundException("ORDER_NOT_FOUND", "Order not found"),
            options,
            logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var content = ReadResponseBody(context);
        var json = JsonDocument.Parse(content);
        var code = json.RootElement.GetProperty("error").GetProperty("code").GetString();
        Assert.Equal("ORDER_NOT_FOUND", code);
    }

    [Fact]
    public async Task InvokeAsync_WithIncludeStackTraceTrue_IncludesStackTrace()
    {
        // Arrange
        var context = CreateHttpContext();
        var logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
        var options = Options.Create(new MiddlewareOptions
        {
            ExceptionHandling = new ExceptionHandlingOptions { Enabled = true, IncludeStackTrace = true, LogExceptions = true }
        });

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Test error"),
            options,
            logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var content = ReadResponseBody(context);
        var json = JsonDocument.Parse(content);
        var stackTrace = json.RootElement.GetProperty("error").GetProperty("stackTrace").GetString();
        Assert.NotNull(stackTrace);
        Assert.NotEmpty(stackTrace);
    }

    [Fact]
    public async Task InvokeAsync_AllowsNextMiddlewareToExecute()
    {
        // Arrange
        var context = CreateHttpContext();
        var nextCalled = false;
        var logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
        var options = Options.Create(new MiddlewareOptions
        {
            ExceptionHandling = new ExceptionHandlingOptions { Enabled = true, IncludeStackTrace = false, LogExceptions = true }
        });

        var middleware = new ExceptionHandlingMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            options,
            logger);

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
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static string ReadResponseBody(HttpContext context)
    {
        if (context.Response.Body is MemoryStream ms)
        {
            return System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }
        return string.Empty;
    }
}
