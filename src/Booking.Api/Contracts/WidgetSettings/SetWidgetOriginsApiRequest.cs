namespace Booking.Api.Contracts.WidgetSettings;

public sealed record SetWidgetOriginsApiRequest(
    IReadOnlyCollection<string> Origins);
