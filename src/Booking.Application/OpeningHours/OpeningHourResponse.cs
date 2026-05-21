namespace Booking.Application.OpeningHours;

public sealed record OpeningHourResponse(
    Guid Id,
    DayOfWeek DayOfWeek,
    TimeOnly OpensAt,
    TimeOnly ClosesAt);
