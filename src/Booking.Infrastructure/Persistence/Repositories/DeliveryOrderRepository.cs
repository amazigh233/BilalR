using Booking.Application.Abstractions;
using Booking.Domain.Delivery;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Persistence.Repositories;

public sealed class DeliveryOrderRepository(BookingDbContext dbContext) : IDeliveryOrderRepository
{
    public async Task AddAsync(DeliveryOrder order, CancellationToken cancellationToken = default)
    {
        dbContext.DeliveryOrders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid restaurantId,
        DeliveryProvider provider,
        string externalOrderId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DeliveryOrders
            .IgnoreQueryFilters()
            .AnyAsync(
                order => order.RestaurantId == restaurantId
                    && order.Provider == provider
                    && order.ExternalOrderId == externalOrderId,
                cancellationToken);
    }

    public async Task<DeliveryOrder?> GetAsync(
        Guid restaurantId,
        DeliveryProvider provider,
        string externalOrderId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DeliveryOrders
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                order => order.RestaurantId == restaurantId
                    && order.Provider == provider
                    && order.ExternalOrderId == externalOrderId,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<DeliveryOrder>> GetByRestaurantAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DeliveryOrders
            .Where(order => order.RestaurantId == restaurantId)
            .OrderByDescending(order => order.PlacedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
