namespace Booking.Api.Contracts.Analytics;

public sealed record ReservationAnalyticsApiResponse(
    DateOnly FromDate,
    DateOnly ToDate,
    int TotalReservations,
    int NewCount,
    int ConfirmedCount,
    int CancelledCount,
    int NoShowCount,
    double NoShowRate,
    int TotalGuests,
    double AveragePartySize,
    IReadOnlyList<DailyReservationCountDto> PerDay,
    IReadOnlyList<WeekdayReservationCountDto> PerWeekday,
    IReadOnlyList<HourlyReservationCountDto> PerHour);

public sealed record DailyReservationCountDto(DateOnly Date, int Count);

public sealed record WeekdayReservationCountDto(DayOfWeek DayOfWeek, int Count);

public sealed record HourlyReservationCountDto(int Hour, int Count);
