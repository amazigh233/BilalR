using Booking.Application.Abstractions;
using Booking.Domain.Delivery;

namespace Booking.Application.Delivery;

public sealed record SetDeliveryProviderEnabledRequest(
    Guid RestaurantId,
    DeliveryProvider Provider,
    bool Enabled);

public sealed class SetDeliveryProviderEnabledUseCase(IDeliveryIntegrationRepository integrationRepository)
{
    public async Task<DeliveryIntegrationResponse> ExecuteAsync(
        SetDeliveryProviderEnabledRequest request,
        CancellationToken cancellationToken = default)
    {
        var integration = await integrationRepository.GetByProviderAsync(
            request.RestaurantId,
            request.Provider,
            cancellationToken);

        if (integration is null)
        {
            throw new KeyNotFoundException("Delivery provider is not connected.");
        }

        if (request.Enabled)
        {
            integration.Enable();
        }
        else
        {
            integration.Disable();
        }

        await integrationRepository.UpdateAsync(integration, cancellationToken);

        return DeliveryIntegrationResponse.FromIntegration(integration);
    }
}
