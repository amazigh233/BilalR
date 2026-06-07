using Booking.Application.Abstractions;
using Booking.Domain.Delivery;

namespace Booking.Application.Delivery;

public sealed record GetDeliveryIntegrationsRequest(Guid RestaurantId);

public sealed class GetDeliveryIntegrationsUseCase(IDeliveryIntegrationRepository integrationRepository)
{
    public async Task<IReadOnlyCollection<DeliveryIntegrationResponse>> ExecuteAsync(
        GetDeliveryIntegrationsRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await integrationRepository.GetByRestaurantAsync(
            request.RestaurantId,
            cancellationToken);

        // Always return a row for every provider so the UI can show "not connected" too.
        return Enum.GetValues<DeliveryProvider>()
            .Select(provider =>
            {
                var integration = existing.FirstOrDefault(item => item.Provider == provider);
                return integration is null
                    ? DeliveryIntegrationResponse.NotConnected(provider)
                    : DeliveryIntegrationResponse.FromIntegration(integration);
            })
            .ToList();
    }
}
