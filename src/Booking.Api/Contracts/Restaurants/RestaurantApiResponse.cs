namespace Booking.Api.Contracts.Restaurants;

public sealed record RestaurantApiResponse(
    Guid Id,
    string Name,
    string? PhoneNumber,
    string? Email);
