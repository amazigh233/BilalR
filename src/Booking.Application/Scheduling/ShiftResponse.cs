using Booking.Domain.Scheduling;

namespace Booking.Application.Scheduling;

public sealed record ShiftResponse(
    Guid Id,
    Guid RestaurantId,
    Guid StaffUserId,
    DateOnly ShiftDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Note)
{
    public static ShiftResponse FromShift(Shift shift)
    {
        return new ShiftResponse(
            shift.Id,
            shift.RestaurantId,
            shift.StaffUserId,
            shift.ShiftDate,
            shift.StartTime,
            shift.EndTime,
            shift.Note);
    }
}
