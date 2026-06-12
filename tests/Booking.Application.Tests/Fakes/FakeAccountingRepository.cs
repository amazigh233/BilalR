using Booking.Application.Abstractions;
using Booking.Domain.Accounting;

namespace Booking.Application.Tests.Fakes;

public sealed class FakeAccountingRepository : IAccountingRepository
{
    public List<AccountingCategory> Categories { get; } = [];
    public List<AccountingEntry> Entries { get; } = [];
    public List<AccountingSourceTransaction> Sources { get; } = [];
    public List<AccountingImportBatch> Imports { get; } = [];
    public List<AccountingConnection> Connections { get; } = [];
    public List<AccountingAttachment> Attachments { get; } = [];
    public List<AccountingMatch> Matches { get; } = [];

    public Task<IReadOnlyCollection<AccountingCategory>> GetCategoriesAsync(Guid restaurantId, CancellationToken cancellationToken = default) => Result(Categories.Where(x => x.RestaurantId == restaurantId));
    public Task<AccountingCategory?> GetCategoryAsync(Guid restaurantId, Guid categoryId, CancellationToken cancellationToken = default) => Task.FromResult(Categories.FirstOrDefault(x => x.RestaurantId == restaurantId && x.Id == categoryId));
    public Task AddCategoriesAsync(IEnumerable<AccountingCategory> categories, CancellationToken cancellationToken = default) { Categories.AddRange(categories); return Task.CompletedTask; }
    public Task AddCategoryAsync(AccountingCategory category, CancellationToken cancellationToken = default) { Categories.Add(category); return Task.CompletedTask; }
    public Task UpdateCategoryAsync(AccountingCategory category, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<IReadOnlyCollection<AccountingEntry>> GetEntriesAsync(Guid restaurantId, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default) => Result(Entries.Where(x => x.RestaurantId == restaurantId && (!fromDate.HasValue || x.EntryDate >= fromDate) && (!toDate.HasValue || x.EntryDate <= toDate)));
    public Task<AccountingEntry?> GetEntryAsync(Guid restaurantId, Guid entryId, CancellationToken cancellationToken = default) => Task.FromResult(Entries.FirstOrDefault(x => x.RestaurantId == restaurantId && x.Id == entryId));
    public Task AddEntryAsync(AccountingEntry entry, CancellationToken cancellationToken = default) { Entries.Add(entry); return Task.CompletedTask; }
    public Task AddEntriesAsync(IEnumerable<AccountingEntry> entries, CancellationToken cancellationToken = default) { Entries.AddRange(entries); return Task.CompletedTask; }
    public Task UpdateEntryAsync(AccountingEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<IReadOnlyCollection<AccountingSourceTransaction>> GetSourceTransactionsAsync(Guid restaurantId, AccountingSourceStatus? status = null, CancellationToken cancellationToken = default) => Result(Sources.Where(x => x.RestaurantId == restaurantId && (!status.HasValue || x.Status == status)));
    public Task<AccountingSourceTransaction?> GetSourceTransactionAsync(Guid restaurantId, Guid sourceTransactionId, CancellationToken cancellationToken = default) => Task.FromResult(Sources.FirstOrDefault(x => x.RestaurantId == restaurantId && x.Id == sourceTransactionId));
    public Task AddSourceTransactionsAsync(IEnumerable<AccountingSourceTransaction> transactions, CancellationToken cancellationToken = default) { Sources.AddRange(transactions); return Task.CompletedTask; }
    public Task UpdateSourceTransactionAsync(AccountingSourceTransaction transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<HashSet<string>> GetExistingFingerprintsAsync(Guid restaurantId, IEnumerable<string> fingerprints, CancellationToken cancellationToken = default) => Task.FromResult(Sources.Where(x => x.RestaurantId == restaurantId && fingerprints.Contains(x.Fingerprint)).Select(x => x.Fingerprint).ToHashSet());
    public Task<bool> ImportChecksumExistsAsync(Guid restaurantId, string checksum, CancellationToken cancellationToken = default) => Task.FromResult(Imports.Any(x => x.RestaurantId == restaurantId && x.FileChecksum == checksum));
    public Task AddImportBatchAsync(AccountingImportBatch batch, CancellationToken cancellationToken = default) { Imports.Add(batch); return Task.CompletedTask; }
    public Task<IReadOnlyCollection<AccountingImportBatch>> GetImportBatchesAsync(Guid restaurantId, CancellationToken cancellationToken = default) => Result(Imports.Where(x => x.RestaurantId == restaurantId));
    public Task AddMatchAsync(AccountingMatch match, CancellationToken cancellationToken = default) { Matches.Add(match); return Task.CompletedTask; }
    public Task<IReadOnlyCollection<AccountingConnection>> GetConnectionsAsync(Guid restaurantId, CancellationToken cancellationToken = default) => Result(Connections.Where(x => x.RestaurantId == restaurantId));
    public Task<IReadOnlyCollection<AccountingConnection>> GetConnectionsDueForSyncAsync(DateTime beforeUtc, CancellationToken cancellationToken = default) => Result(Connections.Where(x => x.Status == AccountingConnectionStatus.Connected && (!x.LastSyncedAtUtc.HasValue || x.LastSyncedAtUtc < beforeUtc)));
    public Task<AccountingConnection?> GetConnectionAsync(Guid restaurantId, AccountingConnectionProvider provider, string externalId, CancellationToken cancellationToken = default) => Task.FromResult(Connections.FirstOrDefault(x => x.RestaurantId == restaurantId && x.Provider == provider && x.ExternalId == externalId));
    public Task AddConnectionAsync(AccountingConnection connection, CancellationToken cancellationToken = default) { Connections.Add(connection); return Task.CompletedTask; }
    public Task UpdateConnectionAsync(AccountingConnection connection, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task AddAttachmentAsync(AccountingAttachment attachment, CancellationToken cancellationToken = default) { Attachments.Add(attachment); return Task.CompletedTask; }
    public Task<AccountingAttachment?> GetAttachmentAsync(Guid restaurantId, Guid attachmentId, CancellationToken cancellationToken = default) => Task.FromResult(Attachments.FirstOrDefault(x => x.RestaurantId == restaurantId && x.Id == attachmentId));
    public Task<IReadOnlyCollection<AccountingAttachment>> GetAttachmentsAsync(Guid restaurantId, Guid entryId, CancellationToken cancellationToken = default) => Result(Attachments.Where(x => x.RestaurantId == restaurantId && x.AccountingEntryId == entryId));

    private static Task<IReadOnlyCollection<T>> Result<T>(IEnumerable<T> values) => Task.FromResult<IReadOnlyCollection<T>>(values.ToList());
}
