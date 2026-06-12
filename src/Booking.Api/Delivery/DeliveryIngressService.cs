using Booking.Api.Contracts.Delivery;
using Booking.Application.Delivery;
using Booking.Domain.Delivery;

namespace Booking.Api.Delivery;

public enum DeliveryIngressResult
{
    Accepted,
    InvalidSecret
}

public sealed class DeliveryIngressService(
    ResolveDeliveryIntegrationUseCase resolveDeliveryIntegrationUseCase,
    IngestDeliveryOrderUseCase ingestDeliveryOrderUseCase)
{
    public async Task<DeliveryIngressResult> ReceiveAsync(
        DeliveryProvider provider,
        string secret,
        DeliveryWebhookApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ExternalOrderId) ||
            string.IsNullOrWhiteSpace(request.CustomerName))
        {
            throw new ArgumentException("externalOrderId and customerName are required.");
        }

        var restaurantId = await resolveDeliveryIntegrationUseCase.ExecuteAsync(
            new ResolveDeliveryIntegrationRequest(provider, secret),
            cancellationToken);

        if (restaurantId is null)
        {
            return DeliveryIngressResult.InvalidSecret;
        }

        var items = (request.Items ?? [])
            .Select(item => new IngestDeliveryOrderLine(item.Name, item.Quantity, item.UnitPrice))
            .ToList();

        await ingestDeliveryOrderUseCase.ExecuteAsync(
            new IngestDeliveryOrderRequest(
                restaurantId.Value,
                provider,
                request.ExternalOrderId,
                request.CustomerName,
                request.CustomerPhone,
                request.DeliveryAddress,
                request.Note,
                request.Status,
                request.TotalAmount,
                request.Currency,
                request.PlacedAtUtc,
                items),
            cancellationToken);

        return DeliveryIngressResult.Accepted;
    }
}
