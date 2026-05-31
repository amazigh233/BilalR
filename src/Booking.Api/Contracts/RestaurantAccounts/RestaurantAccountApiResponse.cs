namespace Booking.Api.Contracts.RestaurantAccounts;

public sealed record RestaurantAccountApiResponse(
    Guid RestaurantId,
    Guid OwnerUserId,
    string OwnerEmail);
