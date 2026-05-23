namespace Booking.Application.Reservations;

public sealed record CreateReservationRequest(
    Guid RestaurantId,
    DateTime ReservationDateTime,
    int PartySize,
    CustomerRequest Customer,
    string? Note = null);
