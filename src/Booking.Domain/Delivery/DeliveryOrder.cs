namespace Booking.Domain.Delivery;

public sealed class DeliveryOrder
{
    private readonly List<DeliveryOrderLine> _items = [];

    private DeliveryOrder()
    {
    }

    public DeliveryOrder(
        Guid restaurantId,
        DeliveryProvider provider,
        string externalOrderId,
        string customerName,
        string? customerPhone,
        string? deliveryAddress,
        string? note,
        string status,
        decimal totalAmount,
        string currency,
        DateTime placedAtUtc,
        DateTime createdAtUtc,
        IEnumerable<DeliveryOrderLine> items)
    {
        if (restaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(restaurantId));
        }

        if (string.IsNullOrWhiteSpace(externalOrderId))
        {
            throw new ArgumentException("External order id is required.", nameof(externalOrderId));
        }

        if (externalOrderId.Length > 100)
        {
            throw new ArgumentException("External order id cannot exceed 100 characters.", nameof(externalOrderId));
        }

        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new ArgumentException("Customer name is required.", nameof(customerName));
        }

        if (customerName.Length > 200)
        {
            throw new ArgumentException("Customer name cannot exceed 200 characters.", nameof(customerName));
        }

        if (customerPhone?.Length > 40)
        {
            throw new ArgumentException("Customer phone cannot exceed 40 characters.", nameof(customerPhone));
        }

        if (deliveryAddress?.Length > 400)
        {
            throw new ArgumentException("Delivery address cannot exceed 400 characters.", nameof(deliveryAddress));
        }

        if (note?.Length > 1000)
        {
            throw new ArgumentException("Note cannot exceed 1000 characters.", nameof(note));
        }

        if (status?.Length > 60)
        {
            throw new ArgumentException("Status cannot exceed 60 characters.", nameof(status));
        }

        if (totalAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalAmount), "Total amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length > 3)
        {
            throw new ArgumentException("Currency must be a 1-3 letter code.", nameof(currency));
        }

        Id = Guid.NewGuid();
        RestaurantId = restaurantId;
        Provider = provider;
        ExternalOrderId = externalOrderId.Trim();
        CustomerName = customerName.Trim();
        CustomerPhone = string.IsNullOrWhiteSpace(customerPhone) ? null : customerPhone.Trim();
        DeliveryAddress = string.IsNullOrWhiteSpace(deliveryAddress) ? null : deliveryAddress.Trim();
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        Status = string.IsNullOrWhiteSpace(status) ? "Unknown" : status.Trim();
        TotalAmount = totalAmount;
        Currency = currency.Trim().ToUpperInvariant();
        PlacedAtUtc = placedAtUtc;
        CreatedAtUtc = createdAtUtc;
        _items.AddRange(items);
    }

    public Guid Id { get; private set; }

    public Guid RestaurantId { get; private set; }

    public DeliveryProvider Provider { get; private set; }

    public string ExternalOrderId { get; private set; } = null!;

    public string CustomerName { get; private set; } = null!;

    public string? CustomerPhone { get; private set; }

    public string? DeliveryAddress { get; private set; }

    public string? Note { get; private set; }

    public string Status { get; private set; } = null!;

    public decimal TotalAmount { get; private set; }

    public string Currency { get; private set; } = null!;

    public DateTime PlacedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<DeliveryOrderLine> Items => _items.AsReadOnly();
}
