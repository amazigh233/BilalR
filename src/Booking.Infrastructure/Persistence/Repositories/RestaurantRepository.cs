using Booking.Application.Abstractions;
using Booking.Domain.Restaurants;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Persistence.Repositories;

public sealed class RestaurantRepository(BookingDbContext dbContext) : IRestaurantRepository
{
    public async Task AddAsync(Restaurant restaurant, CancellationToken cancellationToken = default)
    {
        dbContext.Restaurants.Add(restaurant);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Restaurant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Restaurants
            .AsNoTracking()
            .OrderBy(restaurant => restaurant.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Restaurant?> GetByIdAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Restaurants
            .Include(restaurant => restaurant.OpeningHours)
            .FirstOrDefaultAsync(restaurant => restaurant.Id == restaurantId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<OpeningHour>> GetOpeningHoursAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.OpeningHours
            .Where(openingHour => EF.Property<Guid>(openingHour, "RestaurantId") == restaurantId)
            .OrderBy(openingHour => openingHour.DayOfWeek)
            .ThenBy(openingHour => openingHour.OpensAt)
            .ToListAsync(cancellationToken);
    }

    public async Task SetOpeningHoursAsync(
        Guid restaurantId,
        IReadOnlyCollection<OpeningHour> openingHours,
        CancellationToken cancellationToken = default)
    {
        var existingOpeningHours = await dbContext.OpeningHours
            .Where(openingHour => EF.Property<Guid>(openingHour, "RestaurantId") == restaurantId)
            .ToListAsync(cancellationToken);

        dbContext.OpeningHours.RemoveRange(existingOpeningHours);

        foreach (var openingHour in openingHours)
        {
            dbContext.OpeningHours.Add(openingHour);
            dbContext.Entry(openingHour).Property("RestaurantId").CurrentValue = restaurantId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
