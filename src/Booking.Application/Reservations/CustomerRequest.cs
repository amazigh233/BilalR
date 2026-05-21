namespace Booking.Application.Reservations;

public sealed record CustomerRequest(
    string Name,
    string Email,
    string? PhoneNumber);
