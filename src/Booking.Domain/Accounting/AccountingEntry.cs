namespace Booking.Domain.Accounting;

public sealed class AccountingEntry
{
    private readonly List<AccountingEntrySplit> _splits = [];

    private AccountingEntry()
    {
        Description = string.Empty;
        Currency = "EUR";
    }

    public AccountingEntry(
        Guid restaurantId,
        AccountingEntryType entryType,
        DateOnly entryDate,
        string description,
        IEnumerable<AccountingEntrySplit> splits,
        DateTime createdAtUtc,
        Guid? sourceTransactionId = null,
        Guid? correctionOfEntryId = null)
    {
        if (restaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(restaurantId));
        }

        Id = Guid.NewGuid();
        RestaurantId = restaurantId;
        EntryType = entryType;
        EntryDate = entryDate;
        Description = NormalizeDescription(description);
        Currency = "EUR";
        SourceTransactionId = sourceTransactionId;
        CorrectionOfEntryId = correctionOfEntryId;
        CreatedAtUtc = createdAtUtc;
        Status = AccountingEntryStatus.Draft;
        SetSplits(splits);
    }

    public Guid Id { get; private set; }

    public Guid RestaurantId { get; private set; }

    public AccountingEntryType EntryType { get; private set; }

    public AccountingEntryStatus Status { get; private set; }

    public DateOnly EntryDate { get; private set; }

    public string Description { get; private set; }

    public string Currency { get; private set; }

    public decimal GrossAmount { get; private set; }

    public Guid? SourceTransactionId { get; private set; }

    public Guid? CorrectionOfEntryId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ConfirmedAtUtc { get; private set; }

    public DateTime? CorrectedAtUtc { get; private set; }

    public IReadOnlyCollection<AccountingEntrySplit> Splits => _splits.AsReadOnly();

    public void UpdateDraft(
        AccountingEntryType entryType,
        DateOnly entryDate,
        string description,
        IEnumerable<AccountingEntrySplit> splits)
    {
        EnsureDraft();
        EntryType = entryType;
        EntryDate = entryDate;
        Description = NormalizeDescription(description);
        SetSplits(splits);
    }

    public void Confirm(DateTime confirmedAtUtc)
    {
        EnsureDraft();
        Status = AccountingEntryStatus.Confirmed;
        ConfirmedAtUtc = confirmedAtUtc;
    }

    public void MarkCorrected(DateTime correctedAtUtc)
    {
        if (Status != AccountingEntryStatus.Confirmed)
        {
            throw new InvalidOperationException("Only confirmed entries can be corrected.");
        }

        Status = AccountingEntryStatus.Corrected;
        CorrectedAtUtc = correctedAtUtc;
    }

    public AccountingEntry CreateReversal(DateTime createdAtUtc)
    {
        if (Status != AccountingEntryStatus.Confirmed)
        {
            throw new InvalidOperationException("Only confirmed entries can be reversed.");
        }

        var reversedSplits = _splits.Select(split => new AccountingEntrySplit(
            split.CategoryId,
            split.VatRate,
            -split.GrossAmount,
            -split.DeductibleVatAmount,
            EntryType));

        return new AccountingEntry(
            RestaurantId,
            EntryType,
            EntryDate,
            $"Correctie: {Description}",
            reversedSplits,
            createdAtUtc,
            SourceTransactionId,
            Id);
    }

    private void SetSplits(IEnumerable<AccountingEntrySplit> splits)
    {
        ArgumentNullException.ThrowIfNull(splits);
        var values = splits.ToList();
        if (values.Count == 0)
        {
            throw new ArgumentException("At least one accounting split is required.", nameof(splits));
        }

        _splits.Clear();
        _splits.AddRange(values);
        GrossAmount = _splits.Sum(split => split.GrossAmount);
    }

    private void EnsureDraft()
    {
        if (Status != AccountingEntryStatus.Draft)
        {
            throw new InvalidOperationException("Confirmed accounting entries are immutable.");
        }
    }

    private static string NormalizeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 300)
        {
            throw new ArgumentException("Description is required and may contain at most 300 characters.", nameof(description));
        }

        return description.Trim();
    }
}
