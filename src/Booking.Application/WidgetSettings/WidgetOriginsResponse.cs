namespace Booking.Application.WidgetSettings;

public sealed record WidgetOriginsResponse(
    Guid RestaurantId,
    IReadOnlyCollection<string> Origins);
