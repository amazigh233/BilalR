using Booking.Application.Abstractions;

namespace Booking.Application.OpeningHours;

public sealed class GetOpeningHoursUseCase(IRestaurantRepository restaurantRepository)
{
    public async Task<GetOpeningHoursResponse> ExecuteAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        if (restaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(restaurantId));
        }

        var restaurant = await restaurantRepository.GetByIdAsync(restaurantId, cancellationToken);
        if (restaurant is null)
        {
            throw new KeyNotFoundException("Restaurant was not found.");
        }

        var openingHours = await restaurantRepository.GetOpeningHoursAsync(
            restaurantId,
            cancellationToken);

        var responseOpeningHours = openingHours
            .Select(openingHour => new OpeningHourResponse(
                openingHour.Id,
                openingHour.DayOfWeek,
                openingHour.OpensAt,
                openingHour.ClosesAt))
            .ToList();

        return new GetOpeningHoursResponse(restaurantId, responseOpeningHours);
    }
}
