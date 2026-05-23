namespace Booking.Api.Contracts.Reservations;

public sealed record CustomerApiRequest(
    string Name,
    string Email,
    string? PhoneNumber);
