using Booking.Domain.Accounting;

namespace Booking.Application.Accounting;

public sealed record AccountingSplitRequest(
    Guid? CategoryId,
    int VatRate,
    decimal GrossAmount,
    decimal DeductibleVatAmount);

public sealed record SaveAccountingEntryRequest(
    Guid RestaurantId,
    AccountingEntryType EntryType,
    DateOnly EntryDate,
    string Description,
    IReadOnlyCollection<AccountingSplitRequest> Splits,
    Guid? SourceTransactionId = null);

public sealed record CorrectAccountingEntryRequest(
    Guid RestaurantId,
    Guid EntryId,
    AccountingEntryType EntryType,
    DateOnly EntryDate,
    string Description,
    IReadOnlyCollection<AccountingSplitRequest> Splits);

public sealed record AccountingSplitResponse(
    Guid? CategoryId,
    int VatRate,
    decimal GrossAmount,
    decimal NetAmount,
    decimal VatAmount,
    decimal DeductibleVatAmount);

public sealed record AccountingEntryResponse(
    Guid Id,
    AccountingEntryType EntryType,
    AccountingEntryStatus Status,
    DateOnly EntryDate,
    string Description,
    decimal GrossAmount,
    string Currency,
    Guid? SourceTransactionId,
    Guid? CorrectionOfEntryId,
    DateTime CreatedAtUtc,
    DateTime? ConfirmedAtUtc,
    IReadOnlyCollection<AccountingSplitResponse> Splits)
{
    public static AccountingEntryResponse FromEntry(AccountingEntry entry) => new(
        entry.Id,
        entry.EntryType,
        entry.Status,
        entry.EntryDate,
        entry.Description,
        entry.GrossAmount,
        entry.Currency,
        entry.SourceTransactionId,
        entry.CorrectionOfEntryId,
        entry.CreatedAtUtc,
        entry.ConfirmedAtUtc,
        entry.Splits.Select(split => new AccountingSplitResponse(
            split.CategoryId,
            split.VatRate,
            split.GrossAmount,
            split.NetAmount,
            split.VatAmount,
            split.DeductibleVatAmount)).ToList());
}

public sealed record AccountingCategoryResponse(
    Guid Id,
    string Name,
    AccountingEntryType EntryType,
    bool IsDefault,
    bool IsActive);

public sealed record AccountingSourceTransactionResponse(
    Guid Id,
    AccountingSourceKind SourceKind,
    AccountingSourceStatus Status,
    string ExternalId,
    DateOnly TransactionDate,
    string Description,
    decimal Amount,
    string Currency,
    Guid? AccountingEntryId,
    DateTime CreatedAtUtc);

public sealed record AccountingVatSummary(
    int VatRate,
    decimal RevenueGross,
    decimal RevenueNet,
    decimal VatDue,
    decimal ExpenseGross,
    decimal ExpenseNet,
    decimal DeductibleVat);

public sealed record AccountingCategoryTotal(
    Guid? CategoryId,
    string CategoryName,
    AccountingEntryType EntryType,
    decimal GrossAmount,
    decimal NetAmount);

public sealed record AccountingTrendPoint(
    string Label,
    DateOnly PeriodStart,
    decimal Revenue,
    decimal Expenses,
    decimal Result);

public sealed record AccountingPeriodComparison(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal Revenue,
    decimal Expenses,
    decimal Result);

public sealed record AccountingSummaryResponse(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal Revenue,
    decimal Expenses,
    decimal Result,
    decimal VatDue,
    decimal DeductibleVat,
    decimal VatBalance,
    int OpenReviewItems,
    IReadOnlyCollection<AccountingVatSummary> Vat,
    IReadOnlyCollection<AccountingCategoryTotal> Categories,
    IReadOnlyCollection<AccountingTrendPoint> Timeline,
    AccountingPeriodComparison Previous);

public sealed record AccountingImportMapping(
    string DateColumn,
    string? DescriptionColumn,
    string? AmountColumn,
    string? DebitColumn,
    string? CreditColumn,
    string? ReferenceColumn,
    string? Vat0Column,
    string? Vat9Column,
    string? Vat21Column,
    string DateFormat = "yyyy-MM-dd",
    char Delimiter = ',');

public sealed record AccountingImportPreviewRow(
    int RowNumber,
    DateOnly Date,
    string Description,
    decimal Amount,
    string ExternalId);

public sealed record AccountingImportResult(
    string Checksum,
    int TotalRows,
    int ImportedRows,
    int DuplicateRows,
    IReadOnlyCollection<AccountingImportPreviewRow> Preview);

public sealed record AccountingImportBatchResponse(
    Guid Id,
    AccountingImportKind ImportKind,
    string FileName,
    int RowCount,
    DateTime CreatedAtUtc);

public sealed record AccountingConnectionResponse(
    Guid Id,
    AccountingConnectionProvider Provider,
    AccountingConnectionStatus Status,
    string ExternalId,
    DateTime? LastSyncedAtUtc,
    string? LastError);
