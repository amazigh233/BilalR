namespace Booking.Application.OpeningHours;

public sealed record OpeningHourRequest(
    DayOfWeek DayOfWeek,
    TimeOnly OpensAt,
    TimeOnly ClosesAt);
