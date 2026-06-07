using Booking.Application.Abstractions;
using Booking.Domain.Delivery;

namespace Booking.Application.Delivery;

public sealed record ConnectDeliveryProviderRequest(Guid RestaurantId, DeliveryProvider Provider);

/// <summary>The plaintext secret is returned exactly once; only its hash is stored.</summary>
public sealed record ConnectDeliveryProviderResult(DeliveryProvider Provider, string Secret);

public sealed class ConnectDeliveryProviderUseCase(
    IDeliveryIntegrationRepository integrationRepository,
    TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<ConnectDeliveryProviderResult> ExecuteAsync(
        ConnectDeliveryProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var secret = DeliverySecrets.Generate();
        var hash = DeliverySecrets.Hash(secret);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var existing = await integrationRepository.GetByProviderAsync(
            request.RestaurantId,
            request.Provider,
            cancellationToken);

        if (existing is null)
        {
            var integration = new DeliveryIntegration(request.RestaurantId, request.Provider, hash, now);
            await integrationRepository.AddAsync(integration, cancellationToken);
        }
        else
        {
            existing.Rotate(hash, now);
            await integrationRepository.UpdateAsync(existing, cancellationToken);
        }

        return new ConnectDeliveryProviderResult(request.Provider, secret);
    }
}
