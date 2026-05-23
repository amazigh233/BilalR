using Booking.Application.Abstractions;

namespace Booking.Application.Reservations;

public sealed class GetReservationUseCase(IReservationRepository reservationRepository)
{
    public async Task<ReservationResponse> ExecuteAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        if (reservationId == Guid.Empty)
        {
            throw new ArgumentException("Reservation id is required.", nameof(reservationId));
        }

        var reservation = await reservationRepository.GetByIdAsync(reservationId, cancellationToken);
        if (reservation is null)
        {
            throw new KeyNotFoundException("Reservation was not found.");
        }

        return ReservationResponse.FromReservation(reservation);
    }
}
