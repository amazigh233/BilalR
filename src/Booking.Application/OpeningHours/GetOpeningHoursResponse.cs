namespace Booking.Application.OpeningHours;

public sealed record GetOpeningHoursResponse(
    Guid RestaurantId,
    IReadOnlyCollection<OpeningHourResponse> OpeningHours);
