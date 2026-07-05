using SharedCommon.Core.Exceptions;

namespace SharedCommon.Core.UnitTests;

public class ExceptionsTests
{
    // ── NotFoundException ─────────────────────────────────────────────────────

    [Fact]
    public void NotFoundException_DefaultCode_IsNotFound()
    {
        var ex = new NotFoundException("Order not found");
        Assert.Equal("NOT_FOUND", ex.Code);
        Assert.Equal("Order not found", ex.Message);
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public void NotFoundException_CustomCode_IsPreserved()
    {
        var ex = new NotFoundException("ORDER_MISSING", "Order does not exist");
        Assert.Equal("ORDER_MISSING", ex.Code);
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public void NotFoundException_InheritsDomainException()
    {
        var ex = new NotFoundException("not found");
        Assert.IsAssignableFrom<DomainException>(ex);
    }

    // ── UnauthorizedException ─────────────────────────────────────────────────

    [Fact]
    public void UnauthorizedException_DefaultMessage_IsSet()
    {
        var ex = new UnauthorizedException();
        Assert.Equal("UNAUTHORIZED", ex.Code);
        Assert.Equal(401, ex.StatusCode);
        Assert.False(string.IsNullOrEmpty(ex.Message));
    }

    [Fact]
    public void UnauthorizedException_CustomMessage_IsPreserved()
    {
        var ex = new UnauthorizedException("Token expired");
        Assert.Equal("Token expired", ex.Message);
        Assert.Equal(401, ex.StatusCode);
    }

    // ── ForbiddenException ────────────────────────────────────────────────────

    [Fact]
    public void ForbiddenException_DefaultMessage_IsSet()
    {
        var ex = new ForbiddenException();
        Assert.Equal("FORBIDDEN", ex.Code);
        Assert.Equal(403, ex.StatusCode);
        Assert.False(string.IsNullOrEmpty(ex.Message));
    }

    [Fact]
    public void ForbiddenException_CustomMessage_IsPreserved()
    {
        var ex = new ForbiddenException("Insufficient permissions");
        Assert.Equal("Insufficient permissions", ex.Message);
        Assert.Equal(403, ex.StatusCode);
    }

    // ── ConflictException ─────────────────────────────────────────────────────

    [Fact]
    public void ConflictException_DefaultCode_IsConflict()
    {
        var ex = new ConflictException("Already exists");
        Assert.Equal("CONFLICT", ex.Code);
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public void ConflictException_CustomCode_IsPreserved()
    {
        var ex = new ConflictException("DUPLICATE_EMAIL", "Email already registered");
        Assert.Equal("DUPLICATE_EMAIL", ex.Code);
        Assert.Equal("Email already registered", ex.Message);
        Assert.Equal(409, ex.StatusCode);
    }

    // ── TooManyRequestsException ──────────────────────────────────────────────

    [Fact]
    public void TooManyRequestsException_DefaultMessage_IsSet()
    {
        var ex = new TooManyRequestsException();
        Assert.Equal("RATE_LIMITED", ex.Code);
        Assert.Equal(429, ex.StatusCode);
        Assert.False(string.IsNullOrEmpty(ex.Message));
    }

    [Fact]
    public void TooManyRequestsException_CustomMessage_IsPreserved()
    {
        var ex = new TooManyRequestsException("Slow down, please retry in 60 seconds");
        Assert.Equal("Slow down, please retry in 60 seconds", ex.Message);
        Assert.Equal(429, ex.StatusCode);
    }

    // ── DomainException base ──────────────────────────────────────────────────

    [Fact]
    public void AllExceptions_AreExceptions()
    {
        Assert.IsAssignableFrom<Exception>(new NotFoundException("x"));
        Assert.IsAssignableFrom<Exception>(new UnauthorizedException());
        Assert.IsAssignableFrom<Exception>(new ForbiddenException());
        Assert.IsAssignableFrom<Exception>(new ConflictException("x"));
        Assert.IsAssignableFrom<Exception>(new TooManyRequestsException());
    }

    [Fact]
    public void StatusCodes_AreCorrect()
    {
        Assert.Equal(404, new NotFoundException("x").StatusCode);
        Assert.Equal(401, new UnauthorizedException().StatusCode);
        Assert.Equal(403, new ForbiddenException().StatusCode);
        Assert.Equal(409, new ConflictException("x").StatusCode);
        Assert.Equal(429, new TooManyRequestsException().StatusCode);
    }
}
