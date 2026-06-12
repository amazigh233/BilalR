namespace Booking.Domain.Accounting;

public sealed class AccountingImportBatch
{
    private AccountingImportBatch()
    {
        FileName = string.Empty;
        FileChecksum = string.Empty;
        MappingJson = string.Empty;
        StorageKey = string.Empty;
    }

    public AccountingImportBatch(
        Guid restaurantId,
        AccountingImportKind importKind,
        string fileName,
        string fileChecksum,
        string mappingJson,
        string storageKey,
        int rowCount,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        RestaurantId = restaurantId;
        ImportKind = importKind;
        FileName = fileName;
        FileChecksum = fileChecksum;
        MappingJson = mappingJson;
        StorageKey = storageKey;
        RowCount = rowCount;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid RestaurantId { get; private set; }
    public AccountingImportKind ImportKind { get; private set; }
    public string FileName { get; private set; }
    public string FileChecksum { get; private set; }
    public string MappingJson { get; private set; }
    public string StorageKey { get; private set; }
    public int RowCount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
