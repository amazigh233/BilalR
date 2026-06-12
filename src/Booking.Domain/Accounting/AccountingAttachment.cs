namespace Booking.Domain.Accounting;

public sealed class AccountingAttachment
{
    private AccountingAttachment()
    {
        FileName = string.Empty;
        ContentType = string.Empty;
        StorageKey = string.Empty;
        Checksum = string.Empty;
    }

    public AccountingAttachment(
        Guid restaurantId,
        Guid accountingEntryId,
        string fileName,
        string contentType,
        string storageKey,
        string checksum,
        long length,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        RestaurantId = restaurantId;
        AccountingEntryId = accountingEntryId;
        FileName = fileName;
        ContentType = contentType;
        StorageKey = storageKey;
        Checksum = checksum;
        Length = length;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid RestaurantId { get; private set; }
    public Guid AccountingEntryId { get; private set; }
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public string StorageKey { get; private set; }
    public string Checksum { get; private set; }
    public long Length { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
