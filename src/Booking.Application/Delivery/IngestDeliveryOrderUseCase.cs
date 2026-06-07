using Booking.Application.Abstractions;
using Booking.Domain.Delivery;

namespace Booking.Application.Delivery;

public sealed record IngestDeliveryOrderLine(string Name, int Quantity, decimal UnitPrice);

public sealed record IngestDeliveryOrderRequest(
    Guid RestaurantId,
    DeliveryProvider Provider,
    string ExternalOrderId,
    string CustomerName,
    string? CustomerPhone,
    string? DeliveryAddress,
    string? Note,
    string Status,
    decimal TotalAmount,
    string? Currency,
    DateTime PlacedAtUtc,
    IReadOnlyCollection<IngestDeliveryOrderLine> Items);

public sealed class IngestDeliveryOrderUseCase(
    IDeliveryOrderRepository deliveryOrderRepository,
    TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<DeliveryOrderResponse> ExecuteAsync(
        IngestDeliveryOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        // Idempotent: a platform may retry the same order; never create a duplicate.
        var existing = await deliveryOrderRepository.GetAsync(
            request.RestaurantId,
            request.Provider,
            request.ExternalOrderId,
            cancellationToken);

        if (existing is not null)
        {
            return DeliveryOrderResponse.FromOrder(existing);
        }

        var lines = request.Items
            .Select(item => new DeliveryOrderLine(item.Name, item.Quantity, item.UnitPrice))
            .ToList();

        var order = new DeliveryOrder(
            request.RestaurantId,
            request.Provider,
            request.ExternalOrderId,
            request.CustomerName,
            request.CustomerPhone,
            request.DeliveryAddress,
            request.Note,
            request.Status,
            request.TotalAmount,
            string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency,
            request.PlacedAtUtc == default ? _timeProvider.GetUtcNow().UtcDateTime : request.PlacedAtUtc,
            _timeProvider.GetUtcNow().UtcDateTime,
            lines);

        await deliveryOrderRepository.AddAsync(order, cancellationToken);

        return DeliveryOrderResponse.FromOrder(order);
    }
}
