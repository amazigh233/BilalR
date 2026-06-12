namespace Booking.BlazorApp.ApiClients;

public sealed record ApiErrorResponse(string Message);

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    AuthenticatedUserDto User);

public sealed record AuthenticatedUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    Guid RestaurantId,
    string RestaurantName,
    IReadOnlyCollection<string> Roles);

public sealed record RestaurantDto(
    Guid Id,
    string Name,
    string? PhoneNumber,
    string? Email,
    string WidgetPrimaryColor,
    string WidgetAccentColor,
    string? WidgetWelcomeText,
    string? WidgetLogoUrl);

public sealed record CreateRestaurantRequest(
    string Name,
    string? PhoneNumber,
    string? Email);

public sealed record UpdateRestaurantRequest(
    string Name,
    string? PhoneNumber,
    string? Email);

public sealed record WidgetOriginsDto(
    Guid RestaurantId,
    IReadOnlyCollection<string> Origins);

public sealed record SetWidgetOriginsRequest(
    IReadOnlyCollection<string> Origins);

public sealed record WidgetBrandingDto(
    Guid RestaurantId,
    string PrimaryColor,
    string AccentColor,
    string? WelcomeText,
    string? LogoUrl);

public sealed record SetWidgetBrandingRequest(
    string PrimaryColor,
    string AccentColor,
    string? WelcomeText,
    string? LogoUrl);

public sealed record CreateRestaurantAccountRequest(
    string RestaurantName,
    string OwnerName,
    string OwnerEmail,
    string OwnerPassword,
    string? PhoneNumber);

public sealed record RestaurantAccountDto(
    Guid RestaurantId,
    Guid OwnerUserId,
    string OwnerEmail);

public sealed record CreateStaffRequest(
    string Name,
    string Email,
    string TemporaryPassword,
    string? PhoneNumber,
    decimal? HourlyWage);

public sealed record UpdateStaffRequest(
    string Name,
    string? PhoneNumber,
    decimal? HourlyWage);

public sealed record StaffUserDto(
    Guid UserId,
    string Name,
    string Email,
    string? PhoneNumber,
    decimal? HourlyWage,
    string Role,
    Guid RestaurantId,
    bool IsActive);

public sealed record OpeningHoursDto(
    Guid RestaurantId,
    IReadOnlyCollection<OpeningHourDto> OpeningHours);

public sealed record OpeningHourDto(
    Guid Id,
    DayOfWeek DayOfWeek,
    TimeOnly OpensAt,
    TimeOnly ClosesAt);

public sealed record SetOpeningHoursRequest(
    IReadOnlyCollection<OpeningHourRequest> OpeningHours);

public sealed record OpeningHourRequest(
    DayOfWeek DayOfWeek,
    TimeOnly OpensAt,
    TimeOnly ClosesAt);

public sealed record AvailabilityDto(
    bool IsAvailable,
    string? Reason);

public enum ReservationStatus
{
    New,
    Confirmed,
    Cancelled,
    NoShow
}

public sealed record ReservationDto(
    Guid Id,
    Guid RestaurantId,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhoneNumber,
    DateTime ReservationDateTime,
    int PartySize,
    string? Note,
    ReservationStatus Status,
    DateTime CreatedAtUtc,
    Guid? TableId);

public enum TableShape { Square = 0, Round = 1, Rectangle = 2 }

public sealed record TableDto(
    Guid Id,
    string Name,
    int Capacity,
    string? Section,
    bool IsActive,
    int PosX,
    int PosY,
    int Rotation,
    TableShape Shape);

public sealed record CreateTableRequest(string Name, int Capacity, string? Section);

public sealed record UpdateTableRequest(string Name, int Capacity, string? Section);

public sealed record UpdateTableLayoutRequest(int PosX, int PosY, int Rotation, TableShape Shape);

public sealed record CustomerRequest(
    string Name,
    string Email,
    string? PhoneNumber);

public sealed record CreateReservationRequest(
    Guid RestaurantId,
    DateTime ReservationDateTime,
    int PartySize,
    CustomerRequest Customer,
    string? Note);

public sealed record ChangeReservationStatusRequest(ReservationStatus Status);

public sealed record AnalyticsDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int TotalReservations,
    int NewCount,
    int ConfirmedCount,
    int CancelledCount,
    int NoShowCount,
    double NoShowRate,
    int TotalGuests,
    double AveragePartySize,
    IReadOnlyList<DailyCountDto> PerDay,
    IReadOnlyList<WeekdayCountDto> PerWeekday,
    IReadOnlyList<HourlyCountDto> PerHour);

public sealed record DailyCountDto(DateOnly Date, int Count);

public sealed record WeekdayCountDto(DayOfWeek DayOfWeek, int Count);

public sealed record HourlyCountDto(int Hour, int Count);

public sealed record ShiftDto(
    Guid Id,
    Guid RestaurantId,
    Guid StaffUserId,
    DateOnly ShiftDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Note);

public sealed record CreateShiftRequest(
    Guid StaffUserId,
    DateOnly ShiftDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Note);

public sealed record UpdateShiftRequest(
    Guid StaffUserId,
    DateOnly ShiftDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Note);

public sealed record AvailabilitySlotDto(
    Guid StaffUserId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record SetAvailabilityRequest(IReadOnlyCollection<AvailabilitySlotItem> Slots);

public sealed record AvailabilitySlotItem(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);

public enum LeaveStatus
{
    Pending,
    Approved,
    Denied
}

public sealed record LeaveRequestDto(
    Guid Id,
    Guid StaffUserId,
    DateOnly FromDate,
    DateOnly ToDate,
    string? Reason,
    LeaveStatus Status,
    DateTime CreatedAtUtc,
    DateTime? DecidedAtUtc);

public sealed record CreateLeaveRequest(
    DateOnly FromDate,
    DateOnly ToDate,
    string? Reason);

public enum DeliveryProvider
{
    Thuisbezorgd,
    UberEats
}

public sealed record DeliveryOrderDto(
    Guid Id,
    DeliveryProvider Provider,
    string ExternalOrderId,
    string CustomerName,
    string? CustomerPhone,
    string? DeliveryAddress,
    string? Note,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime PlacedAtUtc,
    DateTime CreatedAtUtc,
    IReadOnlyList<DeliveryOrderLineDto> Items);

public sealed record DeliveryOrderLineDto(string Name, int Quantity, decimal UnitPrice);

public sealed record DeliveryIntegrationDto(
    DeliveryProvider Provider,
    bool Connected,
    bool Enabled,
    DateTime? CreatedAtUtc,
    DateTime? LastRotatedAtUtc);

public sealed record ConnectDeliveryDto(
    DeliveryProvider Provider,
    string WebhookUrl,
    string Secret,
    string? JetConnectOrderUrl,
    string? TConnectOrderUrl);

public enum AccountingEntryType { Revenue, Expense, Transfer }
public enum AccountingEntryStatus { Draft, Confirmed, Corrected }
public enum AccountingSourceKind { Manual, BankCsv, PosCsv, GoCardless, Mollie, Delivery }
public enum AccountingSourceStatus { Pending, Reviewed, Ignored, Matched }
public enum AccountingImportKind { Bank, Pos }
public enum AccountingConnectionProvider { GoCardless, Mollie }
public enum AccountingConnectionStatus { Pending, Connected, ReconnectRequired, Disabled }

public sealed record AccountingSplitRequest(Guid? CategoryId, int VatRate, decimal GrossAmount, decimal DeductibleVatAmount);
public sealed record SaveAccountingEntryRequest(AccountingEntryType EntryType, DateOnly EntryDate, string Description, IReadOnlyCollection<AccountingSplitRequest> Splits);
public sealed record AccountingSplitDto(Guid? CategoryId, int VatRate, decimal GrossAmount, decimal NetAmount, decimal VatAmount, decimal DeductibleVatAmount);
public sealed record AccountingEntryDto(Guid Id, AccountingEntryType EntryType, AccountingEntryStatus Status, DateOnly EntryDate, string Description, decimal GrossAmount, string Currency, Guid? SourceTransactionId, Guid? CorrectionOfEntryId, DateTime CreatedAtUtc, DateTime? ConfirmedAtUtc, IReadOnlyCollection<AccountingSplitDto> Splits);
public sealed record AccountingCategoryDto(Guid Id, string Name, AccountingEntryType EntryType, bool IsDefault, bool IsActive);
public sealed record AccountingSourceTransactionDto(Guid Id, AccountingSourceKind SourceKind, AccountingSourceStatus Status, string ExternalId, DateOnly TransactionDate, string Description, decimal Amount, string Currency, Guid? AccountingEntryId, DateTime CreatedAtUtc);
public sealed record AccountingVatSummaryDto(int VatRate, decimal RevenueGross, decimal RevenueNet, decimal VatDue, decimal ExpenseGross, decimal ExpenseNet, decimal DeductibleVat);
public sealed record AccountingCategoryTotalDto(Guid? CategoryId, string CategoryName, AccountingEntryType EntryType, decimal GrossAmount, decimal NetAmount);
public sealed record AccountingTrendPointDto(string Label, DateOnly PeriodStart, decimal Revenue, decimal Expenses, decimal Result);
public sealed record AccountingPeriodComparisonDto(DateOnly FromDate, DateOnly ToDate, decimal Revenue, decimal Expenses, decimal Result);
public sealed record AccountingSummaryDto(DateOnly FromDate, DateOnly ToDate, decimal Revenue, decimal Expenses, decimal Result, decimal VatDue, decimal DeductibleVat, decimal VatBalance, int OpenReviewItems, IReadOnlyCollection<AccountingVatSummaryDto> Vat, IReadOnlyCollection<AccountingCategoryTotalDto> Categories, IReadOnlyCollection<AccountingTrendPointDto> Timeline, AccountingPeriodComparisonDto? Previous);
public sealed record AccountingImportMapping(string DateColumn, string? DescriptionColumn, string? AmountColumn, string? DebitColumn, string? CreditColumn, string? ReferenceColumn, string? Vat0Column, string? Vat9Column, string? Vat21Column, string DateFormat = "yyyy-MM-dd", char Delimiter = ',');
public sealed record AccountingImportPreviewRowDto(int RowNumber, DateOnly Date, string Description, decimal Amount, string ExternalId);
public sealed record AccountingImportResultDto(string Checksum, int TotalRows, int ImportedRows, int DuplicateRows, IReadOnlyCollection<AccountingImportPreviewRowDto> Preview);
public sealed record AccountingImportProfileDto(Guid Id, string Name, AccountingImportKind ImportKind, AccountingImportMapping Mapping, DateTime CreatedAtUtc);
public sealed record AccountingConnectionDto(Guid Id, AccountingConnectionProvider Provider, AccountingConnectionStatus Status, string ExternalId, DateTime? LastSyncedAtUtc, string? LastError);
public sealed record AccountingConnectionStartDto(AccountingConnectionProvider Provider, string ExternalId, string AuthorizationUrl);
public sealed record AccountingConnectorAvailabilityDto(bool GoCardlessConfigured, bool MollieConfigured);
public sealed record AccountingInstitutionDto(string Id, string Name);
public sealed record AccountingAttachmentDto(Guid Id, Guid AccountingEntryId, string FileName, string ContentType, long Length, DateTime CreatedAtUtc);

public enum GoogleBusinessConnectionStatus { Pending, Connected, ReconnectRequired, Disabled }

public sealed record GbpConnectDto(string AuthorizationUrl);

public sealed record GbpCompleteRequest(string State, string Code);

public sealed record GbpLocationSummaryDto(
    string AccountName,
    string LocationName,
    string Title,
    string? StoreCode);

public sealed record GbpSelectLocationRequest(string AccountName, string LocationName);

public sealed record GbpStatusDto(
    GoogleBusinessConnectionStatus Status,
    string? GbpLocationName,
    string? GbpAccountName,
    DateTime? LastReviewSyncAtUtc,
    DateTime? LastHoursSyncAtUtc,
    string? LastSyncError,
    bool IsConfigured);

public sealed record GbpReviewDto(
    Guid Id,
    string ReviewName,
    string? ReviewerDisplayName,
    int StarRating,
    string? Comment,
    DateTime CreateTime,
    DateTime UpdateTime,
    string? ReplyComment,
    DateTime? ReplyUpdateTime,
    bool IsRead);

public sealed record GbpReplyRequest(string Comment);
