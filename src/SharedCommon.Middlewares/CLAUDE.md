# SharedCommon.Middlewares

ASP.NET Core middleware collection for cross-cutting pipeline concerns.
Thin, observable, correctly-ordered middleware components.

## API Surface

- `UseCorrelationId()` — extract/generate and propagate CorrelationId
- `UseRequestLogging()` — structured request/response logging
- `UseExceptionHandling()` — global exception → ProblemDetails
- `UseResponseTime()` — add X-Response-Time header
- `UseSecurityHeaders()` — add security headers (CSP, X-Frame-Options, etc.)

## Rules

**Must:**
- Single responsibility per middleware class
- Call `next(context)` unless intentionally short-circuiting
- Use LogContext.PushProperty for CorrelationId scope
- No instance state (middleware is a singleton)
- Thread-safe

**Forbidden:**
- Business logic of any kind
- Database calls directly in middleware
- Auth decisions in custom middleware (use ASP.NET auth)
- Buffering request body (use `EnableBuffering()` only when required)

## Pipeline Order

Register in this order:
1. `UseExceptionHandling()` — outermost
2. `UseSecurityHeaders()`
3. `UseCorrelationId()`
4. `UseRequestLogging()`
5. `UseResponseTime()`
6. Framework auth/routing middleware
7. Custom middleware

## Design Decisions

See: .claude/skills/middleware/SKILL.md

## Test Strategy

- Test each middleware in isolation with `HttpContext` fakes
- Test pipeline short-circuiting behavior
- Test exception handler produces correct ProblemDetails format
