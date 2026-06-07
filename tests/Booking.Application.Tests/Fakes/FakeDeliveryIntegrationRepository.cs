using Booking.Application.Abstractions;
using Booking.Domain.Delivery;

namespace Booking.Application.Tests.Fakes;

public sealed class FakeDeliveryIntegrationRepository : IDeliveryIntegrationRepository
{
    public List<DeliveryIntegration> Integrations { get; } = [];

    public Task<IReadOnlyCollection<DeliveryIntegration>> GetByRestaurantAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        var result = Integrations.Where(item => item.RestaurantId == restaurantId).ToList();
        return Task.FromResult<IReadOnlyCollection<DeliveryIntegration>>(result);
    }

    public Task<DeliveryIntegration?> GetByProviderAsync(
        Guid restaurantId,
        DeliveryProvider provider,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Integrations.FirstOrDefault(
            item => item.RestaurantId == restaurantId && item.Provider == provider));
    }

    public Task<DeliveryIntegration?> FindBySecretHashAsync(
        DeliveryProvider provider,
        string webhookSecretHash,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Integrations.FirstOrDefault(
            item => item.Provider == provider
                && item.WebhookSecretHash == webhookSecretHash
                && item.Enabled));
    }

    public Task AddAsync(DeliveryIntegration integration, CancellationToken cancellationToken = default)
    {
        Integrations.Add(integration);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DeliveryIntegration integration, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
