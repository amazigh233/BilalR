using Booking.Application.Abstractions;

namespace Booking.Application.WidgetSettings;

public sealed class SetWidgetBrandingUseCase(IRestaurantRepository restaurantRepository)
{
    public async Task<WidgetBrandingResponse> ExecuteAsync(
        SetWidgetBrandingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.RestaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(request));
        }

        var restaurant = await restaurantRepository.GetByIdAsync(
            request.RestaurantId,
            cancellationToken);
        if (restaurant is null)
        {
            throw new KeyNotFoundException("Restaurant was not found.");
        }

        restaurant.UpdateWidgetBranding(
            request.PrimaryColor,
            request.AccentColor,
            request.WelcomeText,
            request.LogoUrl);

        await restaurantRepository.UpdateAsync(restaurant, cancellationToken);

        return WidgetBrandingResponse.FromRestaurant(restaurant);
    }
}
