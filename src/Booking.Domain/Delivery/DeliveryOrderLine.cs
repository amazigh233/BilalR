namespace Booking.Domain.Delivery;

public sealed class DeliveryOrderLine
{
    private DeliveryOrderLine()
    {
    }

    public DeliveryOrderLine(string name, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Line name is required.", nameof(name));
        }

        if (name.Length > 200)
        {
            throw new ArgumentException("Line name cannot exceed 200 characters.", nameof(name));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than 0.");
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");
        }

        Name = name.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public string Name { get; private set; } = null!;

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }
}
