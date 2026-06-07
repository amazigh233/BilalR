namespace Booking.Api.Contracts.Scheduling;

public sealed record UpdateShiftApiRequest(
    Guid StaffUserId,
    DateOnly ShiftDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Note);
