using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace SharedCommon.Auth;

/// <summary>DI registration extensions for SharedCommon.Auth.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SharedCommon authentication services to the DI container.
    ///
    /// Registers:
    /// <list type="bullet">
    ///   <item><see cref="AuthOptions"/> — validated at startup.</item>
    ///   <item>ASP.NET Core JWT Bearer authentication scheme.</item>
    ///   <item><see cref="IAuthService"/> / <see cref="JwtAuthService"/> — singleton.</item>
    ///   <item><see cref="ICurrentUser"/> / <see cref="CurrentUser"/> — scoped per request.</item>
    /// </list>
    ///
    /// Example:
    /// <code>
    /// builder.Services.AddSharedCommonAuth(builder.Configuration);
    /// // In pipeline:
    /// app.UseAuthentication();
    /// app.UseAuthorization();
    /// </code>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either argument is null.</exception>
    public static IServiceCollection AddSharedCommonAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<AuthOptions>()
            .BindConfiguration(AuthOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpContextAccessor();

        services.AddSingleton<IAuthService, JwtAuthService>();
        services.AddScoped<ICurrentUser, CurrentUser>();

        // Register ASP.NET Core JWT Bearer scheme for [Authorize] to work.
        var jwtSection = configuration.GetSection($"{AuthOptions.SectionName}:Jwt");
        var secretKey = jwtSection["SecretKey"] ?? string.Empty;
        var issuer = jwtSection["Issuer"] ?? string.Empty;
        var audience = jwtSection["Audience"] ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(secretKey) && secretKey.Length >= 32)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(opts =>
                {
                    opts.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,
                        ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
                        ValidIssuer = issuer,
                        ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                        ValidAudience = audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });

            services.AddAuthorization();
        }

        return services;
    }
}
