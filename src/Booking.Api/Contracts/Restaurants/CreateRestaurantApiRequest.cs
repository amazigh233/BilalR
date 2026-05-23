namespace Booking.Api.Contracts.Restaurants;

public sealed record CreateRestaurantApiRequest(
    string Name,
    string? PhoneNumber,
    string? Email);
