namespace Booking.Application.Analytics;

public sealed record ReservationAnalyticsRequest(
    Guid RestaurantId,
    DateOnly FromDate,
    DateOnly ToDate);
