using Booking.Application.Abstractions;
using Booking.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Persistence.Repositories;

public sealed class ShiftRepository(BookingDbContext dbContext) : IShiftRepository
{
    public async Task AddAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        dbContext.Shifts.Add(shift);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Shift?> GetByIdAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Shifts
            .FirstOrDefaultAsync(shift => shift.Id == shiftId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Shift>> GetByRestaurantAndDateRangeAsync(
        Guid restaurantId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Shifts
            .Where(shift => shift.RestaurantId == restaurantId
                && shift.ShiftDate >= fromDate
                && shift.ShiftDate <= toDate)
            .OrderBy(shift => shift.ShiftDate)
            .ThenBy(shift => shift.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Shift>> GetByStaffAndDateRangeAsync(
        Guid restaurantId,
        Guid staffUserId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Shifts
            .Where(shift => shift.RestaurantId == restaurantId
                && shift.StaffUserId == staffUserId
                && shift.ShiftDate >= fromDate
                && shift.ShiftDate <= toDate)
            .OrderBy(shift => shift.ShiftDate)
            .ThenBy(shift => shift.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        dbContext.Shifts.Update(shift);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        dbContext.Shifts.Remove(shift);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
