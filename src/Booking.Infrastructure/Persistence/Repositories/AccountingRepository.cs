using Booking.Application.Abstractions;
using Booking.Domain.Accounting;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Persistence.Repositories;

public sealed class AccountingRepository(BookingDbContext dbContext) : IAccountingRepository
{
    public async Task<IReadOnlyCollection<AccountingCategory>> GetCategoriesAsync(Guid restaurantId, CancellationToken cancellationToken = default) =>
        await dbContext.AccountingCategories.IgnoreQueryFilters().Where(item => item.RestaurantId == restaurantId).OrderBy(item => item.EntryType).ThenBy(item => item.Name).ToListAsync(cancellationToken);

    public Task<AccountingCategory?> GetCategoryAsync(Guid restaurantId, Guid categoryId, CancellationToken cancellationToken = default) =>
        dbContext.AccountingCategories.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.RestaurantId == restaurantId && item.Id == categoryId, cancellationToken);

    public async Task AddCategoriesAsync(IEnumerable<AccountingCategory> categories, CancellationToken cancellationToken = default)
    {
        dbContext.AccountingCategories.AddRange(categories);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddCategoryAsync(AccountingCategory category, CancellationToken cancellationToken = default)
    {
        dbContext.AccountingCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateCategoryAsync(AccountingCategory category, CancellationToken cancellationToken = default)
    {
        dbContext.AccountingCategories.Update(category);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AccountingEntry>> GetEntriesAsync(Guid restaurantId, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.AccountingEntries.IgnoreQueryFilters().Where(item => item.RestaurantId == restaurantId);
        if (fromDate.HasValue) query = query.Where(item => item.EntryDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(item => item.EntryDate <= toDate.Value);
        return await query.OrderByDescending(item => item.EntryDate).ThenByDescending(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public Task<AccountingEntry?> GetEntryAsync(Guid restaurantId, Guid entryId, CancellationToken cancellationToken = default) =>
        dbContext.AccountingEntries.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.RestaurantId == restaurantId && item.Id == entryId, cancellationToken);

    public async Task AddEntryAsync(AccountingEntry entry, CancellationToken cancellationToken = default)
    {
        dbContext.AccountingEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddEntriesAsync(IEnumerable<AccountingEntry> entries, CancellationToken cancellationToken = default)
    {
        dbContext.AccountingEntries.AddRange(entries);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateEntryAsync(AccountingEntry entry, CancellationToken cancellationToken = default)
    {
        dbContext.AccountingEntries.Update(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AccountingSourceTransaction>> GetSourceTransactionsAsync(Guid restaurantId, AccountingSourceStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.AccountingSourceTransactions.IgnoreQueryFilters().Where(item => item.RestaurantId == restaurantId);
        if (status.HasValue) query = query.Where(item => item.Status == status.Value);
        return await query.OrderByDescending(item => item.TransactionDate).ThenByDescending(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public Task<AccountingSourceTransaction?> GetSourceTransactionAsync(Guid restaurantId, Guid sourceTransactionId, CancellationToken cancellationToken = default) =>
        dbContext.AccountingSourceTransactions.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.RestaurantId == restaurantId && item.Id == sourceTransactionId, cancellationToken);

    public async Task AddSourceTransactionsAsync(IEnumerable<AccountingSourceTransaction> transactions, CancellationToken cancellationToken = default)
    {
        dbContext.AccountingSourceTransactions.AddRange(transactions);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSourceTransactionAsync(AccountingSourceTransaction transaction, CancellationToken cancellationToken = default)
    {
        dbContext.AccountingSourceTransactions.Update(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<HashSet<string>> GetExistingFingerprintsAsync(Guid restaurantId, IEnumerable<string> fingerprints, CancellationToken cancellationToken = default)
    {
        var values = fingerprints.Distinct().ToList();
        return (await dbContext.AccountingSourceTransactions.IgnoreQueryFilters().Where(item => item.RestaurantId == restaurantId && values.Contains(item.Fingerprint)).Select(item => item.Fingerprint).ToListAsync(cancellationToken)).ToHashSet();
    }

    public Task<bool> ImportChecksumExistsAsync(Guid restaurantId, string checksum, CancellationToken cancellationToken = default) =>
        dbContext.AccountingImportBatches.IgnoreQueryFilters().AnyAsync(item => item.RestaurantId == restaurantId && item.FileChecksum == checksum, cancellationToken);

    public async Task AddImportBatchAsync(AccountingImportBatch batch, CancellationToken cancellationToken = default)
    {
        dbContext.AccountingImportBatches.Add(batch);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AccountingImportBatch>> GetImportBatchesAsync(Guid restaurantId, CancellationToken cancellationToken = default) =>
        await dbContext.AccountingImportBatches.IgnoreQueryFilters().Where(item => item.RestaurantId == restaurantId).OrderByDescending(item => item.CreatedAtUtc).ToListAsync(cancellationToken);

    public async Task AddMatchAsync(AccountingMatch match, CancellationToken cancellationToken = default)
    {
        dbContext.AccountingMatches.Add(match);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AccountingConnection>> GetConnectionsAsync(Guid restaurantId, CancellationToken cancellationToken = default) =>
        await dbContext.AccountingConnections.IgnoreQueryFilters().Where(item => item.RestaurantId == restaurantId).OrderBy(item => item.Provider).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<AccountingConnection>> GetConnectionsDueForSyncAsync(DateTime beforeUtc, CancellationToken cancellationToken = default) =>
        await dbContext.AccountingConnections
            .IgnoreQueryFilters()
            .Where(item => item.Status == AccountingConnectionStatus.Connected &&
                (!item.LastSyncedAtUtc.HasValue || item.LastSyncedAtUtc < beforeUtc))
            .ToListAsync(cancellationToken);

    public Task<AccountingConnection?> GetConnectionAsync(Guid restaurantId, AccountingConnectionProvider provider, string externalId, CancellationToken cancellationToken = default) =>
        dbContext.AccountingConnections.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.RestaurantId == restaurantId && item.Provider == provider && item.ExternalId == externalId, cancellationToken);

    public async Task AddConnectionAsync(AccountingConnection connection, CancellationToken cancellationToken = default)
    {
        dbContext.AccountingConnections.Add(connection);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateConnectionAsync(AccountingConnection connection, CancellationToken cancellationToken = default)
    {
        dbContext.AccountingConnections.Update(connection);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAttachmentAsync(AccountingAttachment attachment, CancellationToken cancellationToken = default)
    {
        dbContext.AccountingAttachments.Add(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<AccountingAttachment?> GetAttachmentAsync(Guid restaurantId, Guid attachmentId, CancellationToken cancellationToken = default) =>
        dbContext.AccountingAttachments.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.RestaurantId == restaurantId && item.Id == attachmentId, cancellationToken);

    public async Task<IReadOnlyCollection<AccountingAttachment>> GetAttachmentsAsync(Guid restaurantId, Guid entryId, CancellationToken cancellationToken = default) =>
        await dbContext.AccountingAttachments.IgnoreQueryFilters().Where(item => item.RestaurantId == restaurantId && item.AccountingEntryId == entryId).OrderByDescending(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
}
