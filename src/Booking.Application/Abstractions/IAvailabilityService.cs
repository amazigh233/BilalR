using Booking.Application.Availability;

namespace Booking.Application.Abstractions;

public interface IAvailabilityService
{
    Task<AvailabilityResult> CheckAsync(
        Guid restaurantId,
        DateTime reservationDateTime,
        int partySize,
        CancellationToken cancellationToken = default);
}
