using Booking.Application.Abstractions;
using Booking.Domain.Reservations;

namespace Booking.Application.Tests.Fakes;

public sealed class FakeReservationRepository : IReservationRepository
{
    public List<Reservation> Reservations { get; } = [];

    public Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        Reservations.Add(reservation);
        return Task.CompletedTask;
    }

    public Task<Reservation?> GetByIdAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Reservations.FirstOrDefault(reservation => reservation.Id == reservationId));
    }

    public Task<IReadOnlyCollection<Reservation>> GetByRestaurantIdAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        var reservations = Reservations
            .Where(reservation => reservation.RestaurantId == restaurantId)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<Reservation>>(reservations);
    }

    public Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
