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
