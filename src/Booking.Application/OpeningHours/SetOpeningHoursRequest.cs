namespace Booking.Application.OpeningHours;

public sealed record SetOpeningHoursRequest(
    Guid RestaurantId,
    IReadOnlyCollection<OpeningHourRequest> OpeningHours);
