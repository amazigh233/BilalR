namespace Booking.Application.Restaurants;

public sealed record CreateRestaurantRequest(
    string Name,
    string? PhoneNumber,
    string? Email);
