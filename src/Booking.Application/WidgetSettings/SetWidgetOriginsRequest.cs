namespace Booking.Application.WidgetSettings;

public sealed record SetWidgetOriginsRequest(
    Guid RestaurantId,
    IReadOnlyCollection<string> Origins);
