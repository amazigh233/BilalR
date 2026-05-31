namespace Booking.Api.Contracts.Auth;

public sealed record AuthenticatedUserApiResponse(
    Guid Id,
    string Email,
    string DisplayName,
    Guid RestaurantId,
    string RestaurantName,
    IReadOnlyCollection<string> Roles);
