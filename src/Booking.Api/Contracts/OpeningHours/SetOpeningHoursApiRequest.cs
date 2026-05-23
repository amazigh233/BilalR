namespace Booking.Api.Contracts.OpeningHours;

public sealed record SetOpeningHoursApiRequest(
    IReadOnlyCollection<OpeningHourApiRequest> OpeningHours);
