using Booking.Domain.Reservations;

namespace Booking.Api.Contracts.Reservations;

public sealed record ReservationApiResponse(
    Guid Id,
    Guid RestaurantId,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhoneNumber,
    DateTime ReservationDateTime,
    int PartySize,
    ReservationStatus Status,
    DateTime CreatedAtUtc);
