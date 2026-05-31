namespace Booking.Api.Contracts.Auth;

public sealed record LoginApiRequest(
    string Email,
    string Password);
