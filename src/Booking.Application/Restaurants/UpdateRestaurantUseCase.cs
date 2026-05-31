using Booking.Application.Abstractions;

namespace Booking.Application.Restaurants;

public sealed class UpdateRestaurantUseCase(IRestaurantRepository restaurantRepository)
{
    public async Task<RestaurantResponse> ExecuteAsync(
        UpdateRestaurantRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.RestaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(request));
        }

        var restaurant = await restaurantRepository.GetByIdAsync(request.RestaurantId, cancellationToken);
        if (restaurant is null)
        {
            throw new KeyNotFoundException("Restaurant was not found.");
        }

        restaurant.UpdateDetails(
            request.Name,
            request.PhoneNumber,
            request.Email);

        await restaurantRepository.UpdateAsync(restaurant, cancellationToken);

        return RestaurantResponse.FromRestaurant(restaurant);
    }
}
