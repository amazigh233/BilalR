namespace Booking.Api.Authentication;

public sealed record JwtAuthenticationOptions(
    string Issuer,
    string Audience,
    string SigningKey,
    TimeSpan TokenLifetime);
