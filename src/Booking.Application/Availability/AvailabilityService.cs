using Booking.Application.Abstractions;

namespace Booking.Application.Availability;

public sealed class AvailabilityService(IRestaurantRepository restaurantRepository) : IAvailabilityService
{
    public async Task<AvailabilityResult> CheckAsync(
        Guid restaurantId,
        DateTime reservationDateTime,
        int partySize,
        CancellationToken cancellationToken = default)
    {
        if (restaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(restaurantId));
        }

        if (reservationDateTime == default)
        {
            throw new ArgumentException("Reservation date/time is required.", nameof(reservationDateTime));
        }

        if (partySize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(partySize), "Party size must be greater than 0.");
        }

        var restaurant = await restaurantRepository.GetByIdAsync(restaurantId, cancellationToken);
        if (restaurant is null)
        {
            return new AvailabilityResult(false, "Restaurant does not exist.");
        }

        var openingHours = await restaurantRepository.GetOpeningHoursAsync(restaurantId, cancellationToken);
        var reservationTime = TimeOnly.FromDateTime(reservationDateTime);

        var isOpen = openingHours.Any(openingHour =>
            openingHour.DayOfWeek == reservationDateTime.DayOfWeek &&
            openingHour.OpensAt <= reservationTime &&
            openingHour.ClosesAt > reservationTime);

        return isOpen
            ? new AvailabilityResult(true)
            : new AvailabilityResult(false, "Restaurant is closed at the requested time.");
    }
}
