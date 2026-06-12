namespace Booking.TConnectConnector;

public sealed record ConnectorOrder(
    string ExternalOrderId,
    string CustomerName,
    string? CustomerPhone,
    string? DeliveryAddress,
    string? Note,
    string Status,
    DateTime PlacedAtUtc,
    decimal TotalAmount,
    string? Currency,
    IReadOnlyCollection<ConnectorOrderLine> Items);

public sealed record ConnectorOrderLine(
    string Name,
    int Quantity,
    decimal UnitPrice);
