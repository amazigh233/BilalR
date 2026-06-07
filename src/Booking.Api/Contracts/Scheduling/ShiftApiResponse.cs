namespace Booking.Api.Contracts.Scheduling;

public sealed record ShiftApiResponse(
    Guid Id,
    Guid RestaurantId,
    Guid StaffUserId,
    DateOnly ShiftDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Note);
