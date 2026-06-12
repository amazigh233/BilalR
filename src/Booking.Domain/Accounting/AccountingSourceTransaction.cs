namespace Booking.Domain.Accounting;

public sealed class AccountingSourceTransaction
{
    private AccountingSourceTransaction()
    {
        ExternalId = string.Empty;
        Fingerprint = string.Empty;
        Description = string.Empty;
        Currency = "EUR";
        RawPayload = string.Empty;
    }

    public AccountingSourceTransaction(
        Guid restaurantId,
        AccountingSourceKind sourceKind,
        string externalId,
        string fingerprint,
        DateOnly transactionDate,
        string description,
        decimal amount,
        string currency,
        string rawPayload,
        DateTime createdAtUtc,
        Guid? importBatchId = null)
    {
        if (restaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(restaurantId));
        }

        if (string.IsNullOrWhiteSpace(externalId) || externalId.Trim().Length > 200)
        {
            throw new ArgumentException("External id is required and may contain at most 200 characters.", nameof(externalId));
        }

        if (string.IsNullOrWhiteSpace(fingerprint) || fingerprint.Trim().Length > 128)
        {
            throw new ArgumentException("Fingerprint is required.", nameof(fingerprint));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new ArgumentException("Currency must be a three-letter code.", nameof(currency));
        }

        Id = Guid.NewGuid();
        RestaurantId = restaurantId;
        SourceKind = sourceKind;
        ExternalId = externalId.Trim();
        Fingerprint = fingerprint.Trim();
        TransactionDate = transactionDate;
        Description = string.IsNullOrWhiteSpace(description) ? externalId.Trim() : description.Trim();
        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
        RawPayload = rawPayload ?? string.Empty;
        ImportBatchId = importBatchId;
        Status = AccountingSourceStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid RestaurantId { get; private set; }

    public AccountingSourceKind SourceKind { get; private set; }

    public AccountingSourceStatus Status { get; private set; }

    public string ExternalId { get; private set; }

    public string Fingerprint { get; private set; }

    public DateOnly TransactionDate { get; private set; }

    public string Description { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; }

    public string RawPayload { get; private set; }

    public Guid? ImportBatchId { get; private set; }

    public Guid? AccountingEntryId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ReviewedAtUtc { get; private set; }

    public void MarkReviewed(Guid accountingEntryId, DateTime reviewedAtUtc)
    {
        if (accountingEntryId == Guid.Empty)
        {
            throw new ArgumentException("Accounting entry id is required.", nameof(accountingEntryId));
        }

        Status = AccountingSourceStatus.Reviewed;
        AccountingEntryId = accountingEntryId;
        ReviewedAtUtc = reviewedAtUtc;
    }

    public void Ignore(DateTime reviewedAtUtc)
    {
        Status = AccountingSourceStatus.Ignored;
        ReviewedAtUtc = reviewedAtUtc;
    }

    public void MarkMatched(DateTime reviewedAtUtc)
    {
        Status = AccountingSourceStatus.Matched;
        ReviewedAtUtc = reviewedAtUtc;
    }
}
