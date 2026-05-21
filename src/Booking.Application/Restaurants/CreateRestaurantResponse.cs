namespace Booking.Application.Restaurants;

public sealed record CreateRestaurantResponse(
    Guid Id,
    string Name,
    string? PhoneNumber,
    string? Email);
