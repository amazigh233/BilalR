using Booking.Domain.Delivery;

namespace Booking.Application.Delivery;

public sealed record DeliveryOrderResponse(
    Guid Id,
    Guid RestaurantId,
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
    IReadOnlyList<DeliveryOrderLineResponse> Items)
{
    public static DeliveryOrderResponse FromOrder(DeliveryOrder order)
    {
        return new DeliveryOrderResponse(
            order.Id,
            order.RestaurantId,
            order.Provider,
            order.ExternalOrderId,
            order.CustomerName,
            order.CustomerPhone,
            order.DeliveryAddress,
            order.Note,
            order.Status,
            order.TotalAmount,
            order.Currency,
            order.PlacedAtUtc,
            order.CreatedAtUtc,
            order.Items.Select(item => new DeliveryOrderLineResponse(item.Name, item.Quantity, item.UnitPrice)).ToList());
    }
}

public sealed record DeliveryOrderLineResponse(string Name, int Quantity, decimal UnitPrice);
