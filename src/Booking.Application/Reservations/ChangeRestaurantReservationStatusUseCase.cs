using Booking.Application.Abstractions;
using Booking.Domain.Reservations;

namespace Booking.Application.Reservations;

public sealed class ChangeRestaurantReservationStatusUseCase(IReservationRepository reservationRepository)
{
    public async Task<ReservationResponse> ExecuteAsync(
        ChangeRestaurantReservationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.RestaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(request));
        }

        if (request.ReservationId == Guid.Empty)
        {
            throw new ArgumentException("Reservation id is required.", nameof(request));
        }

        var reservation = await reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken);
        if (reservation is null || reservation.RestaurantId != request.RestaurantId)
        {
            throw new KeyNotFoundException("Reservation was not found.");
        }

        ApplyStatus(reservation, request.Status);

        await reservationRepository.UpdateAsync(reservation, cancellationToken);

        return ReservationResponse.FromReservation(reservation);
    }

    private static void ApplyStatus(Reservation reservation, ReservationStatus status)
    {
        switch (status)
        {
            case ReservationStatus.New:
                break;
            case ReservationStatus.Confirmed:
                reservation.Confirm();
                break;
            case ReservationStatus.Cancelled:
                reservation.Cancel();
                break;
            case ReservationStatus.NoShow:
                reservation.MarkAsNoShow();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported reservation status.");
        }
    }
}
