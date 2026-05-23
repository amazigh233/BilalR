using Booking.Application.Abstractions;

namespace Booking.Application.Restaurants;

public sealed class GetRestaurantUseCase(IRestaurantRepository restaurantRepository)
{
    public async Task<RestaurantResponse> ExecuteAsync(
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

        return RestaurantResponse.FromRestaurant(restaurant);
    }
}
