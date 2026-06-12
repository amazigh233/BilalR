namespace Booking.Application.WidgetSettings;

public sealed record SetWidgetBrandingRequest(
    Guid RestaurantId,
    string PrimaryColor,
    string AccentColor,
    string? WelcomeText,
    string? LogoUrl);
