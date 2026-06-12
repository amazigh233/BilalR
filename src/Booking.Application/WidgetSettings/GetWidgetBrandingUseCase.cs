using Booking.Application.Abstractions;

namespace Booking.Application.WidgetSettings;

public sealed class GetWidgetBrandingUseCase(IRestaurantRepository restaurantRepository)
{
    public async Task<WidgetBrandingResponse> ExecuteAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        if (restaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(restaurantId));
        }

        var restaurant = await restaurantRepository.GetByIdAsync(restaurantId, cancellationToken);
        if (restaurant is null)
        {
            throw new KeyNotFoundException("Restaurant was not found.");
        }

        return WidgetBrandingResponse.FromRestaurant(restaurant);
    }
}
