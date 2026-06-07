using Booking.Domain.Delivery;

namespace Booking.Api.Contracts.Delivery;

public sealed record DeliveryWebhookApiRequest(
    string ExternalOrderId,
    string CustomerName,
    string? CustomerPhone,
    string? DeliveryAddress,
    string? Note,
    string Status,
    DateTime PlacedAtUtc,
    decimal TotalAmount,
    string? Currency,
    IReadOnlyCollection<DeliveryWebhookLineApiItem>? Items);

public sealed record DeliveryWebhookLineApiItem(
    string Name,
    int Quantity,
    decimal UnitPrice);

public sealed record DeliveryOrderApiResponse(
    Guid Id,
    DeliveryProvider Provider,
    string ExternalOrderId,
    string CustomerName,
    string? CustomerPhone,
    string? DeliveryAddress,
    string? Note,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime PlacedAtUtc,
    DateTime CreatedAtUtc,
    IReadOnlyList<DeliveryOrderLineApiItem> Items);

public sealed record DeliveryOrderLineApiItem(string Name, int Quantity, decimal UnitPrice);

public sealed record DeliveryIntegrationApiResponse(
    DeliveryProvider Provider,
    bool Connected,
    bool Enabled,
    DateTime? CreatedAtUtc,
    DateTime? LastRotatedAtUtc);

public sealed record ConnectDeliveryApiResponse(
    DeliveryProvider Provider,
    string WebhookUrl,
    string Secret);
