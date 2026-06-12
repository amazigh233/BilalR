namespace Booking.Api.Accounting;

public interface IAccountingAssetStorage
{
    const long MaximumAttachmentSize = 10 * 1024 * 1024;
    const long MaximumImportSize = 10 * 1024 * 1024;

    Task<StoredAccountingAsset> SaveImportAsync(
        Guid restaurantId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<StoredAccountingAsset> SaveAttachmentAsync(
        Guid restaurantId,
        Guid entryId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<AccountingAsset> GetAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}

public sealed record StoredAccountingAsset(
    string StorageKey,
    string FileName,
    string ContentType,
    string Checksum,
    long Length);

public sealed record AccountingAsset(
    Stream Content,
    string FileName,
    string ContentType,
    DateTimeOffset LastModifiedUtc);
