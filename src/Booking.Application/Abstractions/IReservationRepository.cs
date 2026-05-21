using Booking.Domain.Reservations;

namespace Booking.Application.Abstractions;

public interface IReservationRepository
{
    Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default);

    Task<Reservation?> GetByIdAsync(Guid reservationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Reservation>> GetByRestaurantIdAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default);
}
