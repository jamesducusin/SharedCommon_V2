using Serilog;
using Templates.Api.Infrastructure;
using Templates.Api.Infrastructure.Middleware;
using Templates.Application;
using Templates.Application.Common.Telemetry;
using Templates.Infrastructure;
using Templates.Infrastructure.Persistence.Migrations;
using Templates.Infrastructure.Resilience;
using SharedCommon.Logging;
using SharedCommon.Middlewares;
using SharedCommon.ResponseBuilder;
using SharedCommon.HealthChecks;
using SharedCommon.Validation;
using SharedCommon.Observability;
using SharedCommon.Security;
using SharedCommon.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.WithProperty("ApplicationName", "Templates.Api")
        .Enrich.FromLogContext());

// Add Cerberus shared services (core infrastructure)
builder.Services
    .AddSharedLogging()
    .AddSharedObservability()
    .AddSharedSecurity(builder.Configuration)
    .AddSharedAuth(builder.Configuration)
    .AddSharedHealthChecks()
    .AddSharedValidation()
    .AddSharedResponseBuilder()
    .AddSharedMiddlewares();

// Phase 2: Add observability and resilience services
builder.Services
    .AddScoped<ITelemetryService, TelemetryService>()
    .AddDatabaseMigrations()
    .AddResiliencePolicies(builder.Configuration);

// Optional: Add optional Cerberus packages based on configuration
if (builder.Configuration.GetValue<bool>("Features:Caching:Enabled"))
    builder.Services.AddSharedCaching(builder.Configuration);

if (builder.Configuration.GetValue<bool>("Features:Messaging:Enabled"))
    builder.Services.AddSharedMessaging(builder.Configuration);

if (builder.Configuration.GetValue<bool>("Features:Cloud:Enabled"))
    builder.Services.AddSharedCloud(builder.Configuration);

// Add application services
builder.Services.AddApplicationServices();

// Add infrastructure services
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add API services
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

// Phase 2: Run database migrations on startup
try
{
    await app.MigrateAsync();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Database migration failed. Application will not start.");
    throw;
}

// Middleware pipeline (order matters)
app.UseExceptionHandling()
    .UseSharedCorrelationId()
    .UseSharedRequestLogging();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("DefaultPolicy");

// Authentication & Authorization (must be after routing, before endpoint mapping)
app.UseAuthentication();
app.UseAuthorization();

// Map health checks
app.MapSharedHealthChecks();

// Map feature endpoints
app.MapHealthEndpoint();

await app.RunAsync();
