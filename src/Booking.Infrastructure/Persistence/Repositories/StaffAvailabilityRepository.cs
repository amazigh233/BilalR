using Booking.Application.Abstractions;
using Booking.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Persistence.Repositories;

public sealed class StaffAvailabilityRepository(BookingDbContext dbContext) : IStaffAvailabilityRepository
{
    public async Task<IReadOnlyCollection<StaffAvailability>> GetByStaffAsync(
        Guid restaurantId,
        Guid staffUserId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.StaffAvailabilities
            .Where(availability => availability.RestaurantId == restaurantId
                && availability.StaffUserId == staffUserId)
            .OrderBy(availability => availability.DayOfWeek)
            .ThenBy(availability => availability.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<StaffAvailability>> GetByRestaurantAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.StaffAvailabilities
            .Where(availability => availability.RestaurantId == restaurantId)
            .OrderBy(availability => availability.DayOfWeek)
            .ThenBy(availability => availability.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceForStaffAsync(
        Guid restaurantId,
        Guid staffUserId,
        IReadOnlyCollection<StaffAvailability> availabilities,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.StaffAvailabilities
            .Where(availability => availability.RestaurantId == restaurantId
                && availability.StaffUserId == staffUserId)
            .ToListAsync(cancellationToken);

        dbContext.StaffAvailabilities.RemoveRange(existing);
        dbContext.StaffAvailabilities.AddRange(availabilities);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
