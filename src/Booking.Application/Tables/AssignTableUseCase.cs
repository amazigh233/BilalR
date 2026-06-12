using Booking.Application.Abstractions;
using Booking.Domain.Reservations;

namespace Booking.Application.Tables;

public sealed record AssignTableRequest(Guid RestaurantId, Guid ReservationId, Guid? TableId);

public sealed class AssignTableUseCase(
    IReservationRepository reservationRepository,
    ITableRepository tableRepository)
{
    public async Task ExecuteAsync(AssignTableRequest request, CancellationToken cancellationToken = default)
    {
        var reservation = await reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Reservering '{request.ReservationId}' bestaat niet.");

        if (reservation.RestaurantId != request.RestaurantId)
        {
            throw new KeyNotFoundException($"Reservering '{request.ReservationId}' bestaat niet.");
        }

        if (request.TableId is null)
        {
            reservation.UnassignTable();
            await reservationRepository.UpdateAsync(reservation, cancellationToken);
            return;
        }

        var table = await tableRepository.GetByIdAsync(request.TableId.Value, cancellationToken)
            ?? throw new KeyNotFoundException($"Tafel '{request.TableId}' bestaat niet.");

        if (table.RestaurantId != request.RestaurantId)
        {
            throw new KeyNotFoundException($"Tafel '{request.TableId}' bestaat niet.");
        }

        if (!table.IsActive)
        {
            throw new InvalidOperationException("Deze tafel is gedeactiveerd en kan niet worden toegewezen.");
        }

        // Check for conflicting reservations on this table within a 2-hour window.
        var windowStart = reservation.ReservationDateTime;
        var windowEnd = reservation.ReservationDateTime.AddHours(2);
        var dayStart = reservation.ReservationDateTime.Date;
        var dayEnd = dayStart.AddDays(1);

        var dayReservations = await reservationRepository.GetByRestaurantAndDateRangeAsync(
            request.RestaurantId,
            dayStart,
            dayEnd,
            cancellationToken);

        var conflict = dayReservations.FirstOrDefault(other =>
            other.Id != reservation.Id
            && other.TableId == request.TableId
            && other.Status != ReservationStatus.Cancelled
            && other.Status != ReservationStatus.NoShow
            && other.ReservationDateTime < windowEnd
            && other.ReservationDateTime.AddHours(2) > windowStart);

        if (conflict is not null)
        {
            throw new InvalidOperationException(
                $"Tafel is al bezet om {conflict.ReservationDateTime:HH:mm} uur ({conflict.ReservationDateTime:dd MMM}).");
        }

        reservation.AssignTable(request.TableId.Value);
        await reservationRepository.UpdateAsync(reservation, cancellationToken);
    }
}
