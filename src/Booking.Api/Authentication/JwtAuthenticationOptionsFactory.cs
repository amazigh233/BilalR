using System.Security.Cryptography;

namespace Booking.Api.Authentication;

public static class JwtAuthenticationOptionsFactory
{
    public static JwtAuthenticationOptions Create(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger)
    {
        var issuer = configuration["Authentication:Jwt:Issuer"] ?? "Booking.Api";
        var audience = configuration["Authentication:Jwt:Audience"] ?? "Booking.BlazorApp";
        var signingKey = configuration["Authentication:Jwt:SigningKey"];

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "Authentication:Jwt:SigningKey is required outside Development.");
            }

            signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            logger.LogWarning(
                "Using an ephemeral JWT signing key for Development. Existing login sessions expire when the API restarts.");
        }

        var lifetimeMinutes = configuration.GetValue<int?>("Authentication:Jwt:TokenLifetimeMinutes") ?? 480;
        var tokenLifetime = TimeSpan.FromMinutes(Math.Max(5, lifetimeMinutes));

        return new JwtAuthenticationOptions(
            issuer,
            audience,
            signingKey,
            tokenLifetime);
    }
}
