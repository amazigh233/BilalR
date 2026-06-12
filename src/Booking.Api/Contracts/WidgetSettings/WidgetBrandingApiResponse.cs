namespace Booking.Api.Contracts.WidgetSettings;

public sealed record WidgetBrandingApiResponse(
    Guid RestaurantId,
    string PrimaryColor,
    string AccentColor,
    string? WelcomeText,
    string? LogoUrl);
