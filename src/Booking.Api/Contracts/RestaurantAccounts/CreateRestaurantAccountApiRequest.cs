namespace Booking.Api.Contracts.RestaurantAccounts;

public sealed record CreateRestaurantAccountApiRequest(
    string RestaurantName,
    string OwnerName,
    string OwnerEmail,
    string OwnerPassword,
    string? PhoneNumber);
