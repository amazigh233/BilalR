using Booking.Domain.Accounting;

namespace Booking.Application.Abstractions;

public interface IAccountingRepository
{
    Task<IReadOnlyCollection<AccountingCategory>> GetCategoriesAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default);

    Task<AccountingCategory?> GetCategoryAsync(
        Guid restaurantId,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task AddCategoriesAsync(
        IEnumerable<AccountingCategory> categories,
        CancellationToken cancellationToken = default);

    Task AddCategoryAsync(AccountingCategory category, CancellationToken cancellationToken = default);

    Task UpdateCategoryAsync(AccountingCategory category, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccountingEntry>> GetEntriesAsync(
        Guid restaurantId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);

    Task<AccountingEntry?> GetEntryAsync(
        Guid restaurantId,
        Guid entryId,
        CancellationToken cancellationToken = default);

    Task AddEntryAsync(AccountingEntry entry, CancellationToken cancellationToken = default);

    Task AddEntriesAsync(IEnumerable<AccountingEntry> entries, CancellationToken cancellationToken = default);

    Task UpdateEntryAsync(AccountingEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccountingSourceTransaction>> GetSourceTransactionsAsync(
        Guid restaurantId,
        AccountingSourceStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<AccountingSourceTransaction?> GetSourceTransactionAsync(
        Guid restaurantId,
        Guid sourceTransactionId,
        CancellationToken cancellationToken = default);

    Task AddSourceTransactionsAsync(
        IEnumerable<AccountingSourceTransaction> transactions,
        CancellationToken cancellationToken = default);

    Task UpdateSourceTransactionAsync(
        AccountingSourceTransaction transaction,
        CancellationToken cancellationToken = default);

    Task<HashSet<string>> GetExistingFingerprintsAsync(
        Guid restaurantId,
        IEnumerable<string> fingerprints,
        CancellationToken cancellationToken = default);

    Task<bool> ImportChecksumExistsAsync(
        Guid restaurantId,
        string checksum,
        CancellationToken cancellationToken = default);

    Task AddImportBatchAsync(AccountingImportBatch batch, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccountingImportBatch>> GetImportBatchesAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default);

    Task AddMatchAsync(AccountingMatch match, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccountingConnection>> GetConnectionsAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccountingConnection>> GetConnectionsDueForSyncAsync(
        DateTime beforeUtc,
        CancellationToken cancellationToken = default);

    Task<AccountingConnection?> GetConnectionAsync(
        Guid restaurantId,
        AccountingConnectionProvider provider,
        string externalId,
        CancellationToken cancellationToken = default);

    Task AddConnectionAsync(AccountingConnection connection, CancellationToken cancellationToken = default);

    Task UpdateConnectionAsync(AccountingConnection connection, CancellationToken cancellationToken = default);

    Task AddAttachmentAsync(AccountingAttachment attachment, CancellationToken cancellationToken = default);

    Task<AccountingAttachment?> GetAttachmentAsync(
        Guid restaurantId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccountingAttachment>> GetAttachmentsAsync(
        Guid restaurantId,
        Guid entryId,
        CancellationToken cancellationToken = default);
}
