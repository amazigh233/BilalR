using Booking.Application.Abstractions;
using Booking.Domain.Reservations;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Persistence.Repositories;

public sealed class ReservationRepository(BookingDbContext dbContext) : IReservationRepository
{
    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        dbContext.Reservations.Add(reservation);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Reservation?> GetByIdAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Reservations
            .Include(reservation => reservation.Customer)
            .FirstOrDefaultAsync(reservation => reservation.Id == reservationId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Reservation>> GetByRestaurantIdAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Reservations
            .Include(reservation => reservation.Customer)
            .Where(reservation => reservation.RestaurantId == restaurantId)
            .OrderBy(reservation => reservation.ReservationDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        dbContext.Reservations.Update(reservation);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
