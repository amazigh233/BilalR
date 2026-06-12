namespace Booking.Domain.Tables;

public sealed class Table
{
    private Table()
    {
    }

    public Table(Guid restaurantId, string name, int capacity, string? section)
    {
        if (restaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(restaurantId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Table name is required.", nameof(name));
        }

        if (name.Length > 50)
        {
            throw new ArgumentException("Table name cannot exceed 50 characters.", nameof(name));
        }

        if (capacity < 1 || capacity > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be between 1 and 50.");
        }

        if (section?.Length > 50)
        {
            throw new ArgumentException("Section name cannot exceed 50 characters.", nameof(section));
        }

        Id = Guid.NewGuid();
        RestaurantId = restaurantId;
        Name = name.Trim();
        Capacity = capacity;
        Section = string.IsNullOrWhiteSpace(section) ? null : section.Trim();
        IsActive = true;
        Shape = TableShape.Square;
    }

    public Guid Id { get; private set; }

    public Guid RestaurantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int Capacity { get; private set; }

    public string? Section { get; private set; }

    public bool IsActive { get; private set; }

    public int PosX { get; private set; }

    public int PosY { get; private set; }

    public int Rotation { get; private set; }

    public TableShape Shape { get; private set; }

    public void UpdateDetails(string name, int capacity, string? section)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Table name is required.", nameof(name));
        }

        if (name.Length > 50)
        {
            throw new ArgumentException("Table name cannot exceed 50 characters.", nameof(name));
        }

        if (capacity < 1 || capacity > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be between 1 and 50.");
        }

        if (section?.Length > 50)
        {
            throw new ArgumentException("Section name cannot exceed 50 characters.", nameof(section));
        }

        Name = name.Trim();
        Capacity = capacity;
        Section = string.IsNullOrWhiteSpace(section) ? null : section.Trim();
    }

    public void UpdateLayout(int posX, int posY, int rotation, TableShape shape)
    {
        PosX = posX;
        PosY = posY;
        Rotation = rotation;
        Shape = shape;
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
