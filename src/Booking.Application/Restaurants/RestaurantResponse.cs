using Booking.Domain.Restaurants;

namespace Booking.Application.Restaurants;

public sealed record RestaurantResponse(
    Guid Id,
    string Name,
    string? PhoneNumber,
    string? Email)
{
    public static RestaurantResponse FromRestaurant(Restaurant restaurant)
    {
        return new RestaurantResponse(
            restaurant.Id,
            restaurant.Name,
            restaurant.PhoneNumber,
            restaurant.Email);
    }
}
