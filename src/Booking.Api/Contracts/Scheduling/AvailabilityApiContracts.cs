namespace Booking.Api.Contracts.Scheduling;

public sealed record AvailabilitySlotApiResponse(
    Guid StaffUserId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record SetAvailabilityApiRequest(
    IReadOnlyCollection<AvailabilitySlotApiItem> Slots);

public sealed record AvailabilitySlotApiItem(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);
