using Booking.Domain.Restaurants;

namespace Booking.Application.WidgetSettings;

public sealed record WidgetBrandingResponse(
    Guid RestaurantId,
    string PrimaryColor,
    string AccentColor,
    string? WelcomeText,
    string? LogoUrl)
{
    public static WidgetBrandingResponse FromRestaurant(Restaurant restaurant)
    {
        return new WidgetBrandingResponse(
            restaurant.Id,
            restaurant.WidgetPrimaryColor,
            restaurant.WidgetAccentColor,
            restaurant.WidgetWelcomeText,
            restaurant.WidgetLogoUrl);
    }
}
