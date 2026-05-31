namespace Booking.Api.Contracts.Staff;

public sealed record CreateStaffApiRequest(
    string Name,
    string Email,
    string TemporaryPassword,
    string? PhoneNumber);
