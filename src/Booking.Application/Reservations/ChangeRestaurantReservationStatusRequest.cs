using Booking.Domain.Reservations;

namespace Booking.Application.Reservations;

public sealed record ChangeRestaurantReservationStatusRequest(
    Guid RestaurantId,
    Guid ReservationId,
    ReservationStatus Status);
