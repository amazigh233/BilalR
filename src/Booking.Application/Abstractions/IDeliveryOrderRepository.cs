using Booking.Domain.Delivery;

namespace Booking.Application.Abstractions;

public interface IDeliveryOrderRepository
{
    Task AddAsync(DeliveryOrder order, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid restaurantId,
        DeliveryProvider provider,
        string externalOrderId,
        CancellationToken cancellationToken = default);

    Task<DeliveryOrder?> GetAsync(
        Guid restaurantId,
        DeliveryProvider provider,
        string externalOrderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DeliveryOrder>> GetByRestaurantAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default);
}
