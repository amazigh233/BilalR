namespace Booking.Application.OpeningHours;

public sealed record SetOpeningHoursResponse(
    Guid RestaurantId,
    IReadOnlyCollection<OpeningHourResponse> OpeningHours);
