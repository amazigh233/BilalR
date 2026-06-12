using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Booking.Application.Abstractions;
using Booking.Domain.Accounting;
using Booking.Domain.Delivery;

namespace Booking.Application.Accounting;

public sealed class AccountingUseCases(
    IAccountingRepository accountingRepository,
    IDeliveryOrderRepository deliveryOrderRepository)
{
    private static readonly TimeZoneInfo AmsterdamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
    private static readonly (string Name, AccountingEntryType Type)[] DefaultCategories =
    [
        ("Restaurantomzet eten", AccountingEntryType.Revenue),
        ("Restaurantomzet drinken", AccountingEntryType.Revenue),
        ("Bezorgomzet", AccountingEntryType.Revenue),
        ("Overige omzet", AccountingEntryType.Revenue),
        ("Inkoop eten en drinken", AccountingEntryType.Expense),
        ("Personeelskosten", AccountingEntryType.Expense),
        ("Huisvesting", AccountingEntryType.Expense),
        ("Bezorgplatformkosten", AccountingEntryType.Expense),
        ("Betaalproviderkosten", AccountingEntryType.Expense),
        ("Overige kosten", AccountingEntryType.Expense),
        ("Interne overboeking", AccountingEntryType.Transfer)
    ];

    public async Task<IReadOnlyCollection<AccountingCategoryResponse>> GetCategoriesAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultCategoriesAsync(restaurantId, cancellationToken);
        return (await accountingRepository.GetCategoriesAsync(restaurantId, cancellationToken))
            .Select(ToCategoryResponse)
            .ToList();
    }

    public async Task<AccountingCategoryResponse> AddCategoryAsync(
        Guid restaurantId,
        string name,
        AccountingEntryType entryType,
        CancellationToken cancellationToken = default)
    {
        var category = new AccountingCategory(restaurantId, name, entryType, false, DateTime.UtcNow);
        await accountingRepository.AddCategoryAsync(category, cancellationToken);
        return ToCategoryResponse(category);
    }

    public async Task<AccountingCategoryResponse> SetCategoryActiveAsync(
        Guid restaurantId,
        Guid categoryId,
        bool active,
        CancellationToken cancellationToken = default)
    {
        var category = await accountingRepository.GetCategoryAsync(restaurantId, categoryId, cancellationToken)
            ?? throw new KeyNotFoundException("Accounting category was not found.");
        category.SetActive(active);
        await accountingRepository.UpdateCategoryAsync(category, cancellationToken);
        return ToCategoryResponse(category);
    }

    public async Task<AccountingEntryResponse> CreateDraftAsync(
        SaveAccountingEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateCategoriesAsync(request.RestaurantId, request.EntryType, request.Splits, cancellationToken);
        var entry = CreateEntry(request);
        await accountingRepository.AddEntryAsync(entry, cancellationToken);
        return AccountingEntryResponse.FromEntry(entry);
    }

    public async Task<AccountingEntryResponse> UpdateDraftAsync(
        Guid restaurantId,
        Guid entryId,
        SaveAccountingEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var entry = await GetEntryOrThrowAsync(restaurantId, entryId, cancellationToken);
        await ValidateCategoriesAsync(restaurantId, request.EntryType, request.Splits, cancellationToken);
        entry.UpdateDraft(request.EntryType, request.EntryDate, request.Description, ToSplits(request.EntryType, request.Splits));
        await accountingRepository.UpdateEntryAsync(entry, cancellationToken);
        return AccountingEntryResponse.FromEntry(entry);
    }

    public async Task<AccountingEntryResponse> ConfirmAsync(Guid restaurantId, Guid entryId, CancellationToken cancellationToken = default)
    {
        var entry = await GetEntryOrThrowAsync(restaurantId, entryId, cancellationToken);
        entry.Confirm(DateTime.UtcNow);
        await accountingRepository.UpdateEntryAsync(entry, cancellationToken);

        if (entry.SourceTransactionId.HasValue)
        {
            var source = await accountingRepository.GetSourceTransactionAsync(restaurantId, entry.SourceTransactionId.Value, cancellationToken);
            if (source is not null)
            {
                source.MarkReviewed(entry.Id, DateTime.UtcNow);
                await accountingRepository.UpdateSourceTransactionAsync(source, cancellationToken);
            }
        }

        return AccountingEntryResponse.FromEntry(entry);
    }

    public async Task<AccountingEntryResponse> ReviewSourceAsync(
        SaveAccountingEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.SourceTransactionId.HasValue)
        {
            throw new ArgumentException("Source transaction id is required.", nameof(request));
        }

        var source = await accountingRepository.GetSourceTransactionAsync(request.RestaurantId, request.SourceTransactionId.Value, cancellationToken)
            ?? throw new KeyNotFoundException("Source transaction was not found.");
        if (source.Status != AccountingSourceStatus.Pending)
        {
            throw new InvalidOperationException("Only pending source transactions can be reviewed.");
        }
        if (source.Currency != "EUR")
        {
            throw new InvalidOperationException("Alleen EUR-bronregels kunnen worden bevestigd.");
        }

        await ValidateCategoriesAsync(request.RestaurantId, request.EntryType, request.Splits, cancellationToken);
        var entry = CreateEntry(request);
        entry.Confirm(DateTime.UtcNow);
        await accountingRepository.AddEntryAsync(entry, cancellationToken);
        source.MarkReviewed(entry.Id, DateTime.UtcNow);
        await accountingRepository.UpdateSourceTransactionAsync(source, cancellationToken);
        return AccountingEntryResponse.FromEntry(entry);
    }

    public async Task<AccountingEntryResponse> CorrectAsync(
        CorrectAccountingEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var original = await GetEntryOrThrowAsync(request.RestaurantId, request.EntryId, cancellationToken);
        await ValidateCategoriesAsync(request.RestaurantId, request.EntryType, request.Splits, cancellationToken);

        var now = DateTime.UtcNow;
        var reversal = original.CreateReversal(now);
        reversal.Confirm(now);
        original.MarkCorrected(now);
        await accountingRepository.UpdateEntryAsync(original, cancellationToken);
        await accountingRepository.AddEntryAsync(reversal, cancellationToken);

        var replacement = new AccountingEntry(
            request.RestaurantId,
            request.EntryType,
            request.EntryDate,
            request.Description,
            ToSplits(request.EntryType, request.Splits),
            now,
            original.SourceTransactionId,
            original.Id);
        replacement.Confirm(now);
        await accountingRepository.AddEntryAsync(replacement, cancellationToken);
        return AccountingEntryResponse.FromEntry(replacement);
    }

    public async Task<IReadOnlyCollection<AccountingEntryResponse>> GetEntriesAsync(
        Guid restaurantId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default) =>
        (await accountingRepository.GetEntriesAsync(restaurantId, fromDate, toDate, cancellationToken))
            .Select(AccountingEntryResponse.FromEntry)
            .ToList();

    public async Task<IReadOnlyCollection<AccountingSourceTransactionResponse>> GetReviewItemsAsync(Guid restaurantId, CancellationToken cancellationToken = default) =>
        (await accountingRepository.GetSourceTransactionsAsync(restaurantId, AccountingSourceStatus.Pending, cancellationToken))
            .Select(ToSourceResponse)
            .ToList();

    public async Task IgnoreSourceAsync(Guid restaurantId, Guid sourceTransactionId, CancellationToken cancellationToken = default)
    {
        var source = await accountingRepository.GetSourceTransactionAsync(restaurantId, sourceTransactionId, cancellationToken)
            ?? throw new KeyNotFoundException("Source transaction was not found.");
        source.Ignore(DateTime.UtcNow);
        await accountingRepository.UpdateSourceTransactionAsync(source, cancellationToken);
    }

    public async Task MatchSourcesAsync(Guid restaurantId, Guid bankSourceId, Guid matchedSourceId, CancellationToken cancellationToken = default)
    {
        var bank = await accountingRepository.GetSourceTransactionAsync(restaurantId, bankSourceId, cancellationToken)
            ?? throw new KeyNotFoundException("Bank source transaction was not found.");
        var matched = await accountingRepository.GetSourceTransactionAsync(restaurantId, matchedSourceId, cancellationToken)
            ?? throw new KeyNotFoundException("Matched source transaction was not found.");
        if (bank.SourceKind is not (AccountingSourceKind.BankCsv or AccountingSourceKind.GoCardless))
        {
            throw new ArgumentException("Only a bank source transaction can be reconciled.", nameof(bankSourceId));
        }

        await accountingRepository.AddMatchAsync(new AccountingMatch(restaurantId, bank.Id, matched.Id, DateTime.UtcNow), cancellationToken);
        bank.MarkMatched(DateTime.UtcNow);
        await accountingRepository.UpdateSourceTransactionAsync(bank, cancellationToken);
    }

    public async Task<AccountingSummaryResponse> GetSummaryAsync(
        Guid restaurantId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        if (toDate < fromDate) throw new ArgumentException("To date cannot be before from date.");
        var entries = (await accountingRepository.GetEntriesAsync(restaurantId, fromDate, toDate, cancellationToken))
            .Where(entry => entry.Status != AccountingEntryStatus.Draft)
            .ToList();
        var categories = (await GetCategoriesAsync(restaurantId, cancellationToken)).ToDictionary(item => item.Id);

        var revenue = entries.Where(item => item.EntryType == AccountingEntryType.Revenue).Sum(item => item.GrossAmount);
        var expenses = entries.Where(item => item.EntryType == AccountingEntryType.Expense).Sum(item => item.GrossAmount);
        var vat = entries.SelectMany(entry => entry.Splits.Select(split => (entry.EntryType, Split: split)))
            .GroupBy(item => item.Split.VatRate)
            .Select(group => new AccountingVatSummary(
                group.Key,
                group.Where(item => item.EntryType == AccountingEntryType.Revenue).Sum(item => item.Split.GrossAmount),
                group.Where(item => item.EntryType == AccountingEntryType.Revenue).Sum(item => item.Split.NetAmount),
                group.Where(item => item.EntryType == AccountingEntryType.Revenue).Sum(item => item.Split.VatAmount),
                group.Where(item => item.EntryType == AccountingEntryType.Expense).Sum(item => item.Split.GrossAmount),
                group.Where(item => item.EntryType == AccountingEntryType.Expense).Sum(item => item.Split.NetAmount),
                group.Where(item => item.EntryType == AccountingEntryType.Expense).Sum(item => item.Split.DeductibleVatAmount)))
            .OrderBy(item => item.VatRate)
            .ToList();

        var categoryTotals = entries.SelectMany(entry => entry.Splits.Select(split => (entry.EntryType, Split: split)))
            .GroupBy(item => new { item.EntryType, item.Split.CategoryId })
            .Select(group => new AccountingCategoryTotal(
                group.Key.CategoryId,
                group.Key.CategoryId.HasValue && categories.TryGetValue(group.Key.CategoryId.Value, out var category) ? category.Name : "Geen categorie",
                group.Key.EntryType,
                group.Sum(item => item.Split.GrossAmount),
                group.Sum(item => item.Split.NetAmount)))
            .OrderBy(item => item.EntryType)
            .ThenByDescending(item => item.GrossAmount)
            .ToList();

        var openItems = (await accountingRepository.GetSourceTransactionsAsync(restaurantId, AccountingSourceStatus.Pending, cancellationToken)).Count;
        var vatDue = vat.Sum(item => item.VatDue);
        var deductible = vat.Sum(item => item.DeductibleVat);

        var timeline = BuildTimeline(entries, fromDate, toDate);

        var periodLength = toDate.DayNumber - fromDate.DayNumber + 1;
        var previousFrom = fromDate.AddDays(-periodLength);
        var previousTo = fromDate.AddDays(-1);
        var previousEntries = (await accountingRepository.GetEntriesAsync(restaurantId, previousFrom, previousTo, cancellationToken))
            .Where(entry => entry.Status != AccountingEntryStatus.Draft)
            .ToList();
        var previousRevenue = previousEntries.Where(item => item.EntryType == AccountingEntryType.Revenue).Sum(item => item.GrossAmount);
        var previousExpenses = previousEntries.Where(item => item.EntryType == AccountingEntryType.Expense).Sum(item => item.GrossAmount);
        var previous = new AccountingPeriodComparison(
            previousFrom,
            previousTo,
            previousRevenue,
            previousExpenses,
            previousRevenue - previousExpenses);

        return new AccountingSummaryResponse(fromDate, toDate, revenue, expenses, revenue - expenses, vatDue, deductible, vatDue - deductible, openItems, vat, categoryTotals, timeline, previous);
    }

    private static IReadOnlyCollection<AccountingTrendPoint> BuildTimeline(
        IReadOnlyCollection<AccountingEntry> entries,
        DateOnly fromDate,
        DateOnly toDate)
    {
        var dutchCulture = CultureInfo.GetCultureInfo("nl-NL");
        var totalDays = toDate.DayNumber - fromDate.DayNumber + 1;

        Func<DateOnly, DateOnly> bucketStart;
        Func<DateOnly, DateOnly> nextBucket;
        Func<DateOnly, string> label;

        if (totalDays <= 31)
        {
            bucketStart = date => date;
            nextBucket = date => date.AddDays(1);
            label = date => date.ToString("d MMM", dutchCulture);
        }
        else if (totalDays <= 92)
        {
            bucketStart = date => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
            nextBucket = date => date.AddDays(7);
            label = date => $"wk {ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue))}";
        }
        else
        {
            bucketStart = date => new DateOnly(date.Year, date.Month, 1);
            nextBucket = date => date.AddMonths(1);
            label = date => $"{date.ToString("MMM", dutchCulture)} '{date.ToString("yy", CultureInfo.InvariantCulture)}";
        }

        var totals = entries
            .Where(entry => entry.EntryType is AccountingEntryType.Revenue or AccountingEntryType.Expense)
            .GroupBy(entry => bucketStart(entry.EntryDate))
            .ToDictionary(
                group => group.Key,
                group => (
                    Revenue: group.Where(item => item.EntryType == AccountingEntryType.Revenue).Sum(item => item.GrossAmount),
                    Expenses: group.Where(item => item.EntryType == AccountingEntryType.Expense).Sum(item => item.GrossAmount)));

        var points = new List<AccountingTrendPoint>();
        for (var period = bucketStart(fromDate); period <= toDate; period = nextBucket(period))
        {
            var (periodRevenue, periodExpenses) = totals.TryGetValue(period, out var value) ? value : (0m, 0m);
            points.Add(new AccountingTrendPoint(label(period), period, periodRevenue, periodExpenses, periodRevenue - periodExpenses));
        }

        return points;
    }

    public async Task<int> SyncDeliverySalesAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        var orders = await deliveryOrderRepository.GetByRestaurantAsync(restaurantId, cancellationToken);
        var groups = orders
            .Where(order => order.Currency == "EUR")
            .GroupBy(order => new
            {
                Date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(order.PlacedAtUtc, DateTimeKind.Utc),
                    AmsterdamTimeZone)),
                order.Provider
            })
            .ToList();
        var candidates = groups.Select(group =>
        {
            var externalId = $"delivery:{group.Key.Provider}:{group.Key.Date:yyyy-MM-dd}";
            var amount = group.Sum(order => order.TotalAmount);
            var fingerprint = Fingerprint(externalId, group.Key.Date, amount, "EUR");
            return new AccountingSourceTransaction(
                restaurantId,
                AccountingSourceKind.Delivery,
                externalId,
                fingerprint,
                group.Key.Date,
                $"{ProviderLabel(group.Key.Provider)} dagomzet",
                amount,
                "EUR",
                JsonSerializer.Serialize(group.Select(order => order.ExternalOrderId)),
                DateTime.UtcNow);
        }).ToList();
        var existing = await accountingRepository.GetExistingFingerprintsAsync(restaurantId, candidates.Select(item => item.Fingerprint), cancellationToken);
        var newItems = candidates.Where(item => !existing.Contains(item.Fingerprint)).ToList();
        if (newItems.Count > 0) await accountingRepository.AddSourceTransactionsAsync(newItems, cancellationToken);
        return newItems.Count;
    }

    private async Task EnsureDefaultCategoriesAsync(Guid restaurantId, CancellationToken cancellationToken)
    {
        var existing = await accountingRepository.GetCategoriesAsync(restaurantId, cancellationToken);
        if (existing.Count > 0) return;
        await accountingRepository.AddCategoriesAsync(
            DefaultCategories.Select(item => new AccountingCategory(restaurantId, item.Name, item.Type, true, DateTime.UtcNow)),
            cancellationToken);
    }

    private async Task ValidateCategoriesAsync(Guid restaurantId, AccountingEntryType entryType, IEnumerable<AccountingSplitRequest> splits, CancellationToken cancellationToken)
    {
        foreach (var split in splits.Where(item => item.CategoryId.HasValue))
        {
            var category = await accountingRepository.GetCategoryAsync(restaurantId, split.CategoryId!.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Accounting category was not found.");
            if (category.EntryType != entryType) throw new ArgumentException("Category does not match entry type.");
        }
    }

    private async Task<AccountingEntry> GetEntryOrThrowAsync(Guid restaurantId, Guid entryId, CancellationToken cancellationToken) =>
        await accountingRepository.GetEntryAsync(restaurantId, entryId, cancellationToken)
            ?? throw new KeyNotFoundException("Accounting entry was not found.");

    private static AccountingEntry CreateEntry(SaveAccountingEntryRequest request) => new(
        request.RestaurantId,
        request.EntryType,
        request.EntryDate,
        request.Description,
        ToSplits(request.EntryType, request.Splits),
        DateTime.UtcNow,
        request.SourceTransactionId);

    private static IReadOnlyCollection<AccountingEntrySplit> ToSplits(AccountingEntryType type, IEnumerable<AccountingSplitRequest> splits) =>
        splits.Select(split => new AccountingEntrySplit(split.CategoryId, split.VatRate, split.GrossAmount, split.DeductibleVatAmount, type)).ToList();

    private static AccountingCategoryResponse ToCategoryResponse(AccountingCategory category) =>
        new(category.Id, category.Name, category.EntryType, category.IsDefault, category.IsActive);

    private static AccountingSourceTransactionResponse ToSourceResponse(AccountingSourceTransaction source) =>
        new(source.Id, source.SourceKind, source.Status, source.ExternalId, source.TransactionDate, source.Description, source.Amount, source.Currency, source.AccountingEntryId, source.CreatedAtUtc);

    public static string Fingerprint(string externalId, DateOnly date, decimal amount, string currency)
    {
        var input = $"{externalId.Trim()}|{date:yyyy-MM-dd}|{amount.ToString("0.00", CultureInfo.InvariantCulture)}|{currency.ToUpperInvariant()}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }

    private static string ProviderLabel(DeliveryProvider provider) => provider switch
    {
        DeliveryProvider.Thuisbezorgd => "Thuisbezorgd",
        DeliveryProvider.UberEats => "Uber Eats",
        _ => provider.ToString()
    };
}
