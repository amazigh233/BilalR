using Booking.Domain.Reservations;

namespace Booking.Application.Reservations;

public sealed record ChangeReservationStatusRequest(
    Guid ReservationId,
    ReservationStatus Status);
