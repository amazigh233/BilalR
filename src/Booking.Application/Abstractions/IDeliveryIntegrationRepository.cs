using Booking.Domain.Delivery;

namespace Booking.Application.Abstractions;

public interface IDeliveryIntegrationRepository
{
    Task<IReadOnlyCollection<DeliveryIntegration>> GetByRestaurantAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default);

    Task<DeliveryIntegration?> GetByProviderAsync(
        Guid restaurantId,
        DeliveryProvider provider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an enabled integration purely by its secret hash, ignoring the tenant query filter.
    /// Used by the anonymous webhook to map an inbound call to a restaurant.
    /// </summary>
    Task<DeliveryIntegration?> FindBySecretHashAsync(
        DeliveryProvider provider,
        string webhookSecretHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(DeliveryIntegration integration, CancellationToken cancellationToken = default);

    Task UpdateAsync(DeliveryIntegration integration, CancellationToken cancellationToken = default);
}
