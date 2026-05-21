namespace Booking.Application.Availability;

public sealed record CheckAvailabilityResponse(
    bool IsAvailable,
    string? Reason);
