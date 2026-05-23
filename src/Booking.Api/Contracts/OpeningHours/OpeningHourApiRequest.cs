namespace Booking.Api.Contracts.OpeningHours;

public sealed record OpeningHourApiRequest(
    DayOfWeek DayOfWeek,
    TimeOnly OpensAt,
    TimeOnly ClosesAt);
