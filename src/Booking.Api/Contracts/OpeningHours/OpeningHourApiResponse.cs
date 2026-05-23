namespace Booking.Api.Contracts.OpeningHours;

public sealed record OpeningHourApiResponse(
    Guid Id,
    DayOfWeek DayOfWeek,
    TimeOnly OpensAt,
    TimeOnly ClosesAt);
