namespace Booking.Application.Analytics;

public sealed record ReservationAnalyticsResponse(
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
    IReadOnlyList<DailyReservationCount> PerDay,
    IReadOnlyList<WeekdayReservationCount> PerWeekday,
    IReadOnlyList<HourlyReservationCount> PerHour);

public sealed record DailyReservationCount(DateOnly Date, int Count);

public sealed record WeekdayReservationCount(DayOfWeek DayOfWeek, int Count);

public sealed record HourlyReservationCount(int Hour, int Count);
