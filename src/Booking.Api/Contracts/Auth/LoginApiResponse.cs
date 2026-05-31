namespace Booking.Api.Contracts.Auth;

public sealed record LoginApiResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    AuthenticatedUserApiResponse User);
