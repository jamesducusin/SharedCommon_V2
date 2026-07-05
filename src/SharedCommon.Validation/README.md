# SharedCommon.Validation

FluentValidation with automatic validator discovery and an optional global MVC action filter that validates all controller inputs before the action runs.

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.Validation
```

## Registration

Pass the assemblies that contain your `AbstractValidator<T>` implementations:

```csharp
builder.Services.AddSharedCommonValidation(
    builder.Configuration,
    typeof(Program).Assembly);          // scan your service assembly

// Scan multiple assemblies:
builder.Services.AddSharedCommonValidation(
    builder.Configuration,
    typeof(Program).Assembly,
    typeof(OrderValidator).Assembly);
```

## Configuration

```json
{
  "SharedCommon": {
    "Validation": {
      "AutomaticControllerValidation": true
    }
  }
}
```

| Property | Default | Notes |
|----------|---------|-------|
| `Enabled` | `true` | Set `false` to disable the entire pipeline. |
| `AutomaticControllerValidation` | `true` | Registers a global MVC filter. All controller actions validated before they run. |
| `LanguageManager.Enabled` | `false` | Localized validation messages. |
| `LanguageManager.DefaultLanguage` | `en` | IETF language tag. |

## Usage

### Define a validator

```csharp
using FluentValidation;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required.")
            .ForEach(item => item
                .NotNull()
                .ChildRules(rules =>
                {
                    rules.RuleFor(i => i.ProductId).NotEmpty();
                    rules.RuleFor(i => i.Quantity).GreaterThan(0);
                }));

        RuleFor(x => x.ShippingAddress.PostalCode)
            .Matches(@"^\d{5}(-\d{4})?$")
            .WithMessage("Postal code must be 5 or 9 digits.");
    }
}
```

### Automatic validation (AutomaticControllerValidation = true)

When enabled, the global filter intercepts the request before the action method runs. If validation fails, it returns a `422 Unprocessable Entity` with field-level errors automatically — no boilerplate needed:

```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateOrderCommand cmd, CancellationToken ct)
{
    // If cmd is invalid, the filter returns 422 before this runs
    var order = await _service.CreateAsync(cmd, ct);
    return Ok(order);
}
```

### Manual validation

Inject `IValidator<T>` directly when you need explicit control:

```csharp
public class OrderService(IValidator<CreateOrderCommand> validator)
{
    public async Task<Result<Order>> CreateAsync(CreateOrderCommand cmd, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(cmd, ct);
        if (!validation.IsValid)
        {
            var errors = validation.ToDictionary();   // Dictionary<string, string[]>
            return Result<Order>.Invalid(errors);
        }

        // proceed ...
    }
}
```

### Using rule sets

Convention-based rule sets are named by `ValidationOptions.RuleSets`:

```csharp
// Apply only the "CreateRuleSet" rules
await validator.ValidateAsync(cmd, opts =>
    opts.IncludeRuleSets("CreateRuleSet"), ct);
```

## What Gets Registered

| Service | Lifetime | Notes |
|---------|----------|-------|
| `IValidator<T>` (all found) | Scoped | Discovered from the assemblies you pass. |
| `AutoValidationFilter` | — | Registered as a global MVC filter when `AutomaticControllerValidation` is true. |
