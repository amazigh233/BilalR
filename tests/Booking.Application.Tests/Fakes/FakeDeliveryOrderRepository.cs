using Booking.Application.Abstractions;
using Booking.Domain.Delivery;

namespace Booking.Application.Tests.Fakes;

public sealed class FakeDeliveryOrderRepository : IDeliveryOrderRepository
{
    public List<DeliveryOrder> Orders { get; } = [];

    public Task AddAsync(DeliveryOrder order, CancellationToken cancellationToken = default)
    {
        Orders.Add(order);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(
        Guid restaurantId,
        DeliveryProvider provider,
        string externalOrderId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Orders.Any(order => order.RestaurantId == restaurantId
            && order.Provider == provider
            && order.ExternalOrderId == externalOrderId));
    }

    public Task<DeliveryOrder?> GetAsync(
        Guid restaurantId,
        DeliveryProvider provider,
        string externalOrderId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Orders.FirstOrDefault(order => order.RestaurantId == restaurantId
            && order.Provider == provider
            && order.ExternalOrderId == externalOrderId));
    }

    public Task<IReadOnlyCollection<DeliveryOrder>> GetByRestaurantAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        var result = Orders
            .Where(order => order.RestaurantId == restaurantId)
            .OrderByDescending(order => order.PlacedAtUtc)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<DeliveryOrder>>(result);
    }
}
