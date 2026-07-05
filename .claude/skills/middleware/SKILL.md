# Middleware Skill

Build thin, observable, correctly-ordered middleware.

## When to Use This Skill

Triggers:
- Adding request/response pipeline middleware
- Implementing cross-cutting concerns (auth, logging, correlation)
- Ordering middleware components
- Debugging middleware pipeline issues

Ask Claude explicitly: "Use middleware skill"

## Input (What You Provide)

- Middleware purpose
- Where it fits in the pipeline

## Output (What You Get)

- Middleware implementation
- Registration extension method
- Correct pipeline order

## Checklist

- [ ] Single responsibility (one concern per middleware)
- [ ] No business logic
- [ ] Calls `next(context)` unless intentionally short-circuiting
- [ ] Logs at entry and exit (structured)
- [ ] Adds CorrelationId to log scope
- [ ] Handles exceptions gracefully
- [ ] Does not buffer request body unless necessary
- [ ] Thread-safe (no instance state)

## Correct Pipeline Order

```
1. Exception handling (outermost)
2. HTTPS redirection
3. Static files
4. Routing
5. CORS
6. Authentication
7. Authorization
8. CorrelationId
9. Request logging
10. Custom middleware
11. Endpoint execution (innermost)
```

## Middleware Template

```csharp
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"]
            .FirstOrDefault() ?? Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
```

## Common Mistakes

❌ Business logic in middleware
- Why: Not testable, not reusable, wrong layer
- Fix: Move to application service, call from controller

❌ Instance state on middleware class
- Why: Middleware is a singleton; state causes race conditions
- Fix: Use scoped services injected via `InvokeAsync` parameters

## References

See: src/SharedCommon.Middlewares/CLAUDE.md
See: docs/standards/coding-standards.md
