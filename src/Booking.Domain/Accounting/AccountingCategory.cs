namespace Booking.Domain.Accounting;

public sealed class AccountingCategory
{
    private AccountingCategory()
    {
        Name = string.Empty;
    }

    public AccountingCategory(
        Guid restaurantId,
        string name,
        AccountingEntryType entryType,
        bool isDefault,
        DateTime createdAtUtc)
    {
        if (restaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(restaurantId));
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
        {
            throw new ArgumentException("Category name is required and may contain at most 100 characters.", nameof(name));
        }

        Id = Guid.NewGuid();
        RestaurantId = restaurantId;
        Name = name.Trim();
        EntryType = entryType;
        IsDefault = isDefault;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid RestaurantId { get; private set; }

    public string Name { get; private set; }

    public AccountingEntryType EntryType { get; private set; }

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public void SetActive(bool active)
    {
        IsActive = active;
    }
}
