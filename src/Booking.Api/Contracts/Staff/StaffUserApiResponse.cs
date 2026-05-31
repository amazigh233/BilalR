namespace Booking.Api.Contracts.Staff;

public sealed record StaffUserApiResponse(
    Guid UserId,
    string Name,
    string Email,
    string? PhoneNumber,
    string Role,
    Guid RestaurantId,
    bool IsActive);
