using Booking.Application.Abstractions;
using Booking.Domain.Delivery;

namespace Booking.Application.Delivery;

public sealed record ResolveDeliveryIntegrationRequest(DeliveryProvider Provider, string Secret);

public sealed class ResolveDeliveryIntegrationUseCase(IDeliveryIntegrationRepository integrationRepository)
{
    /// <summary>
    /// Authenticates an inbound webhook by its plaintext secret and returns the owning restaurant id,
    /// or <c>null</c> when there is no matching enabled integration.
    /// </summary>
    public async Task<Guid?> ExecuteAsync(
        ResolveDeliveryIntegrationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Secret))
        {
            return null;
        }

        var hash = DeliverySecrets.Hash(request.Secret);
        var integration = await integrationRepository.FindBySecretHashAsync(
            request.Provider,
            hash,
            cancellationToken);

        return integration?.RestaurantId;
    }
}
