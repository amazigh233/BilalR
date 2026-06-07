using Booking.Domain.Scheduling;

namespace Booking.Application.Scheduling;

public sealed record AvailabilitySlotResponse(
    Guid StaffUserId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime)
{
    public static AvailabilitySlotResponse FromAvailability(StaffAvailability availability)
    {
        return new AvailabilitySlotResponse(
            availability.StaffUserId,
            availability.DayOfWeek,
            availability.StartTime,
            availability.EndTime);
    }
}

public sealed record AvailabilitySlotInput(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);
