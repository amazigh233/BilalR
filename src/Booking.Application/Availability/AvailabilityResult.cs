namespace Booking.Application.Availability;

public sealed record AvailabilityResult(
    bool IsAvailable,
    string? Reason = null);
