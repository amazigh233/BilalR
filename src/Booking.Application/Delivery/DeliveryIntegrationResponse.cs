using Booking.Domain.Delivery;

namespace Booking.Application.Delivery;

public sealed record DeliveryIntegrationResponse(
    DeliveryProvider Provider,
    bool Connected,
    bool Enabled,
    DateTime? CreatedAtUtc,
    DateTime? LastRotatedAtUtc)
{
    public static DeliveryIntegrationResponse FromIntegration(DeliveryIntegration integration)
    {
        return new DeliveryIntegrationResponse(
            integration.Provider,
            Connected: true,
            integration.Enabled,
            integration.CreatedAtUtc,
            integration.LastRotatedAtUtc);
    }

    public static DeliveryIntegrationResponse NotConnected(DeliveryProvider provider)
    {
        return new DeliveryIntegrationResponse(provider, Connected: false, Enabled: false, null, null);
    }
}
