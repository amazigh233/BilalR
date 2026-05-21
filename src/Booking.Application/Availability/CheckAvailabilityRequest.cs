namespace Booking.Application.Availability;

public sealed record CheckAvailabilityRequest(
    Guid RestaurantId,
    DateTime ReservationDateTime,
    int PartySize);
