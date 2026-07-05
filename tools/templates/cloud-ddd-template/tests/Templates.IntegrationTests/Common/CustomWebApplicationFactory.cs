namespace Templates.Tests.Integration.Common;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Templates.Api;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// Uses Dapper with a test database connection string.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override configuration to use test database
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            var testSettings = new Dictionary<string, string>
            {
                {"ConnectionStrings:DefaultConnection", GetTestConnectionString()}
            };

            configBuilder.AddInMemoryCollection(testSettings);
        });

        base.ConfigureWebHost(builder);
    }

    /// <summary>
    /// Gets the connection string for the test database.
    /// </summary>
    private static string GetTestConnectionString()
    {
        // Use LocalDB for integration tests
        // For CI/CD, use environment variable or specific test database
        var localDbInstance = Environment.GetEnvironmentVariable("TEST_DATABASE")
            ?? @"(localdb)\mssqllocaldb";

        return $"Server={localDbInstance};Database=TemplateTestDb_{Guid.NewGuid():N};Integrated Security=true;";
    }
}
