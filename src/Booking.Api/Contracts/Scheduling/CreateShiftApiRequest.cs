namespace Booking.Api.Contracts.Scheduling;

public sealed record CreateShiftApiRequest(
    Guid StaffUserId,
    DateOnly ShiftDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Note);
