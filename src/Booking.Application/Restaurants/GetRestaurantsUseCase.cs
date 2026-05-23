using Booking.Application.Abstractions;

namespace Booking.Application.Restaurants;

public sealed class GetRestaurantsUseCase(IRestaurantRepository restaurantRepository)
{
    public async Task<IReadOnlyCollection<RestaurantResponse>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var restaurants = await restaurantRepository.GetAllAsync(cancellationToken);

        return restaurants
            .OrderBy(restaurant => restaurant.Name)
            .Select(RestaurantResponse.FromRestaurant)
            .ToList();
    }
}
