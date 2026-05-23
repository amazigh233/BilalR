namespace Booking.Api.Contracts.Availability;

public sealed record AvailabilityApiResponse(
    bool IsAvailable,
    string? Reason);
