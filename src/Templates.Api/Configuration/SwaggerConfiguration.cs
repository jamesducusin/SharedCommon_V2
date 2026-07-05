namespace Templates.Api.Configuration;

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

/// <summary>
/// Swagger/OpenAPI configuration for the Templates API.
/// Auto-generates API documentation from code.
/// </summary>
public static class SwaggerConfiguration
{
    /// <summary>
    /// Register Swagger/OpenAPI documentation.
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddSwaggerGen(options =>
        {
            // API version info
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Templates API",
                Version = "v1.0.0",
                Description = "Cloud-native DDD platform with distributed tracing, resilience, and observability",
                Contact = new OpenApiContact
                {
                    Name = "Platform Engineering",
                    Email = "platform@example.com"
                },
                License = new OpenApiLicense
                {
                    Name = "MIT"
                }
            });

            // JWT Bearer authentication
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Include XML comments
            var xmlFiles = Directory.GetFiles(
                Path.Combine(AppContext.BaseDirectory),
                "*.xml");

            foreach (var xmlFile in xmlFiles)
            {
                options.IncludeXmlComments(xmlFile);
            }

            // Ignore null response models
            options.IgnoreObsoleteActions();
            options.IgnoreObsoleteProperties();

            // Custom operation filter for correlation IDs
            options.OperationFilter<CorrelationIdOperationFilter>();

            // Custom schema filter for examples
            options.SchemaFilter<ExampleSchemaFilter>();
        });

        return services;
    }

    /// <summary>
    /// Use Swagger UI and OpenAPI endpoint middleware.
    /// </summary>
    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
        {
            app.UseSwagger(options =>
            {
                options.SerializeAsV2 = false;  // Use OpenAPI 3.0
            });

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Templates API v1");
                options.RoutePrefix = "swagger";
                
                // Swagger UI customization
                options.DefaultModelsExpandDepth(2);
                options.DefaultModelExpandDepth(2);
                options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                options.EnableFilter();
            });

            // OpenAPI JSON endpoint
            app.MapGet("/openapi/v1.json", (ISwaggerProvider provider) =>
            {
                return provider.GetSwagger("v1");
            })
            .WithName("GetOpenApi")
            .Produces("application/json")
            .WithOpenApi();
        }

        return app;
    }
}

/// <summary>
/// Adds Correlation-ID header to all OpenAPI operations.
/// </summary>
public class CorrelationIdOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Correlation-ID",
            In = ParameterLocation.Header,
            Description = "Correlation ID for distributed tracing. Auto-generated if not provided.",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Format = "uuid",
                Example = "123e4567-e89b-12d3-a456-426614174000"
            },
            Required = false
        });
    }
}

/// <summary>
/// Adds example values to schema models.
/// </summary>
public class ExampleSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        // Add examples for common types
        if (context.Type == typeof(Guid))
        {
            schema.Example = "123e4567-e89b-12d3-a456-426614174000";
        }
        else if (context.Type == typeof(DateTime))
        {
            schema.Example = "2026-05-30T14:30:00Z";
        }
        else if (context.Type.Name == "OrderDto")
        {
            schema.Example = new
            {
                id = "550e8400-e29b-41d4-a716-446655440000",
                orderNumber = "ORD-001",
                customerId = "650e8400-e29b-41d4-a716-446655440000",
                total = 150.00,
                status = "Completed",
                createdAt = "2026-05-25T10:00:00Z",
                items = new[]
                {
                    new
                    {
                        productId = "750e8400-e29b-41d4-a716-446655440000",
                        productName = "Widget A",
                        quantity = 2,
                        unitPrice = 50.00
                    }
                }
            };
        }
    }
}

/// <summary>
/// Example API endpoints with full OpenAPI documentation.
/// </summary>
public static class ExampleEndpoints
{
    /// <summary>
    /// Register documented example endpoints.
    /// </summary>
    public static WebApplication MapExampleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/orders")
            .WithName("Orders")
            .WithOpenApi()
            .WithTags("Orders");

        group.MapGet("/",
            GetOrders)
            .WithName("ListOrders")
            .WithOpenApi(operation =>
            {
                operation.Summary = "List all orders";
                operation.Description = "Retrieve a paginated list of all orders with optional filtering";
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "skip",
                    In = ParameterLocation.Query,
                    Description = "Number of records to skip",
                    Schema = new OpenApiSchema { Type = "integer", Default = 0 }
                });
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "take",
                    In = ParameterLocation.Query,
                    Description = "Number of records to take",
                    Schema = new OpenApiSchema { Type = "integer", Default = 10 }
                });
                return operation;
            })
            .Produces<IEnumerable<object>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapGet("/{id}",
            GetOrderById)
            .WithName("GetOrder")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get order by ID";
                operation.Description = "Retrieve a specific order by its unique identifier. Includes all order items.";
                return operation;
            })
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/",
            CreateOrder)
            .WithName("CreateOrder")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Create new order";
                operation.Description = "Create a new order with validation. Returns the created order with assigned ID.";
                return operation;
            })
            .Accepts<object>("application/json")
            .Produces<object>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapDelete("/{id}",
            DeleteOrder)
            .WithName("DeleteOrder")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Delete order";
                operation.Description = "Delete an existing order. Only orders in Pending or Draft status can be deleted.";
                return operation;
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static IResult GetOrders(int skip = 0, int take = 10)
        => Results.Ok(new { skip, take, total = 100, orders = new[] { } });

    private static IResult GetOrderById(Guid id)
        => Results.Ok(new { id, orderNumber = "ORD-001", status = "Completed" });

    private static IResult CreateOrder()
        => Results.Created("/api/v1/orders/123", new { id = Guid.NewGuid() });

    private static IResult DeleteOrder(Guid id)
        => Results.NoContent();
}
