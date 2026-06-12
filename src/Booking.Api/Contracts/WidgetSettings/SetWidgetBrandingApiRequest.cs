namespace Booking.Api.Contracts.WidgetSettings;

public sealed record SetWidgetBrandingApiRequest(
    string PrimaryColor,
    string AccentColor,
    string? WelcomeText,
    string? LogoUrl);
