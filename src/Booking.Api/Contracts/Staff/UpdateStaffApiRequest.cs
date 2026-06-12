namespace Booking.Api.Contracts.Staff;

public sealed record UpdateStaffApiRequest(
    string Name,
    string? PhoneNumber,
    decimal? HourlyWage);
