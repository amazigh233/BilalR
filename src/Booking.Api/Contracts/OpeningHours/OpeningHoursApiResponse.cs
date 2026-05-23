namespace Booking.Api.Contracts.OpeningHours;

public sealed record OpeningHoursApiResponse(
    Guid RestaurantId,
    IReadOnlyCollection<OpeningHourApiResponse> OpeningHours);
