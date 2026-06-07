using Booking.Application.Abstractions;
using Booking.Domain.Delivery;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Persistence.Repositories;

public sealed class DeliveryIntegrationRepository(BookingDbContext dbContext) : IDeliveryIntegrationRepository
{
    public async Task<IReadOnlyCollection<DeliveryIntegration>> GetByRestaurantAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DeliveryIntegrations
            .Where(integration => integration.RestaurantId == restaurantId)
            .OrderBy(integration => integration.Provider)
            .ToListAsync(cancellationToken);
    }

    public async Task<DeliveryIntegration?> GetByProviderAsync(
        Guid restaurantId,
        DeliveryProvider provider,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DeliveryIntegrations
            .FirstOrDefaultAsync(
                integration => integration.RestaurantId == restaurantId && integration.Provider == provider,
                cancellationToken);
    }

    public async Task<DeliveryIntegration?> FindBySecretHashAsync(
        DeliveryProvider provider,
        string webhookSecretHash,
        CancellationToken cancellationToken = default)
    {
        // Anonymous webhook path: no authenticated tenant, so the global query filter must be bypassed.
        return await dbContext.DeliveryIntegrations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                integration => integration.Provider == provider
                    && integration.WebhookSecretHash == webhookSecretHash
                    && integration.Enabled,
                cancellationToken);
    }

    public async Task AddAsync(DeliveryIntegration integration, CancellationToken cancellationToken = default)
    {
        dbContext.DeliveryIntegrations.Add(integration);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DeliveryIntegration integration, CancellationToken cancellationToken = default)
    {
        dbContext.DeliveryIntegrations.Update(integration);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
