namespace Booking.Api.Contracts.WidgetSettings;

public sealed record WidgetOriginsApiResponse(
    Guid RestaurantId,
    IReadOnlyCollection<string> Origins);
