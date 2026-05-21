using Booking.Application.Abstractions;

namespace Booking.Application.Availability;

public sealed class CheckAvailabilityUseCase(IAvailabilityService availabilityService)
{
    public async Task<CheckAvailabilityResponse> ExecuteAsync(
        CheckAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await availabilityService.CheckAsync(
            request.RestaurantId,
            request.ReservationDateTime,
            request.PartySize,
            cancellationToken);

        return new CheckAvailabilityResponse(result.IsAvailable, result.Reason);
    }
}
