using Booking.Application.Accounting;
using Booking.Domain.Accounting;

namespace Booking.Api.Contracts.Accounting;

public sealed record SaveAccountingEntryApiRequest(
    AccountingEntryType EntryType,
    DateOnly EntryDate,
    string Description,
    IReadOnlyCollection<AccountingSplitRequest> Splits);

public sealed record AddAccountingCategoryApiRequest(string Name, AccountingEntryType EntryType);

public sealed record SetAccountingCategoryActiveApiRequest(bool Active);

public sealed record MatchAccountingSourcesApiRequest(Guid BankSourceTransactionId, Guid MatchedSourceTransactionId);

public sealed record StartGoCardlessApiRequest(string InstitutionId);

public sealed record CompleteGoCardlessApiRequest(string RequisitionId);

public sealed record CompleteMollieApiRequest(string State, string Code);

public sealed record AccountingAttachmentApiResponse(
    Guid Id,
    Guid AccountingEntryId,
    string FileName,
    string ContentType,
    long Length,
    DateTime CreatedAtUtc);

public sealed record AccountingInstitutionApiResponse(string Id, string Name);

public sealed record AccountingConnectorAvailabilityApiResponse(
    bool GoCardlessConfigured,
    bool MollieConfigured);

public sealed record AccountingImportProfileApiResponse(
    Guid Id,
    string Name,
    AccountingImportKind ImportKind,
    AccountingImportMapping Mapping,
    DateTime CreatedAtUtc);
