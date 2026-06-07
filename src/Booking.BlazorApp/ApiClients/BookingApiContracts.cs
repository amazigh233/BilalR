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
    string? Email);

public sealed record CreateRestaurantRequest(
    string Name,
    string? PhoneNumber,
    string? Email);

public sealed record UpdateRestaurantRequest(
    string Name,
    string? PhoneNumber,
    string? Email);

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
    string? PhoneNumber);

public sealed record StaffUserDto(
    Guid UserId,
    string Name,
    string Email,
    string? PhoneNumber,
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
    DateTime CreatedAtUtc);

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
    string Secret);
