namespace Booking.Api.Contracts.Restaurants;

public sealed record UpdateRestaurantApiRequest(
    string Name,
    string? PhoneNumber,
    string? Email);
