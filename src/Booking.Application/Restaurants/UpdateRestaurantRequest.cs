namespace Booking.Application.Restaurants;

public sealed record UpdateRestaurantRequest(
    Guid RestaurantId,
    string Name,
    string? PhoneNumber,
    string? Email);
