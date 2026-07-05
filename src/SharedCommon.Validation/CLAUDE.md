# SharedCommon.Validation

Input validation using FluentValidation with DI integration.
Provides automatic validation pipeline for commands and requests.

## API Surface

- `IValidator<T>` — FluentValidation interface (re-exported)
- `ValidationBehavior<TRequest, TResponse>` — MediatR pipeline behavior
- `AddSharedValidation(Assembly[])` — registers all validators in assemblies
- `ValidationResult` — maps FluentValidation result to `Result<T>`

## Rules

**Must:**
- One validator class per command/request type
- Validators registered via `AddSharedValidation` (never manual registration)
- Validation errors returned as `Result.Failure` with structured `Error` list
- Custom messages for every rule (no default FluentValidation messages in production)

**Forbidden:**
- Validation logic in controllers (use validators)
- Business rule validation in FluentValidation validators (those go in domain)
- Async validators hitting the database for uniqueness checks in hot paths

## Design Decisions

Input validation (format, required, range) lives in validators.
Business rule validation (uniqueness, domain constraints) lives in domain services.

## Test Strategy

- Unit test each validator with valid and invalid inputs
- Test each rule independently
- Use `TestValidationResult` from FluentValidation.TestHelper

## Extension Points

- Custom validators implement `AbstractValidator<T>`
- Cross-property rules via `RuleFor(...).Must(...)`
