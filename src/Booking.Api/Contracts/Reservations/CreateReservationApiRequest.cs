namespace Booking.Api.Contracts.Reservations;

public sealed record CreateReservationApiRequest(
    Guid RestaurantId,
    DateTime ReservationDateTime,
    int PartySize,
    CustomerApiRequest Customer,
    string? Note = null);
