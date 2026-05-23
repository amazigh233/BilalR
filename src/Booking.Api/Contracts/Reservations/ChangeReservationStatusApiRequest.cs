using Booking.Domain.Reservations;

namespace Booking.Api.Contracts.Reservations;

public sealed record ChangeReservationStatusApiRequest(ReservationStatus Status);
