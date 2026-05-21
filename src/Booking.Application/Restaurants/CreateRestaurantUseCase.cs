using Booking.Application.Abstractions;
using Booking.Domain.Restaurants;

namespace Booking.Application.Restaurants;

public sealed class CreateRestaurantUseCase(IRestaurantRepository restaurantRepository)
{
    public async Task<CreateRestaurantResponse> ExecuteAsync(
        CreateRestaurantRequest request,
        CancellationToken cancellationToken = default)
    {
        var restaurant = new Restaurant(
            request.Name,
            request.PhoneNumber,
            request.Email);

        await restaurantRepository.AddAsync(restaurant, cancellationToken);

        return new CreateRestaurantResponse(
            restaurant.Id,
            restaurant.Name,
            restaurant.PhoneNumber,
            restaurant.Email);
    }
}
