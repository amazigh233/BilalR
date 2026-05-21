using Booking.Application.Abstractions;
using Booking.Domain.Restaurants;

namespace Booking.Application.OpeningHours;

public sealed class SetOpeningHoursUseCase(IRestaurantRepository restaurantRepository)
{
    public async Task<SetOpeningHoursResponse> ExecuteAsync(
        SetOpeningHoursRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.RestaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(request));
        }

        var restaurant = await restaurantRepository.GetByIdAsync(request.RestaurantId, cancellationToken);
        if (restaurant is null)
        {
            throw new KeyNotFoundException("Restaurant was not found.");
        }

        var openingHours = request.OpeningHours
            .Select(openingHour => new OpeningHour(
                openingHour.DayOfWeek,
                openingHour.OpensAt,
                openingHour.ClosesAt))
            .ToList();

        await restaurantRepository.SetOpeningHoursAsync(
            restaurant.Id,
            openingHours,
            cancellationToken);

        var responseOpeningHours = openingHours
            .Select(openingHour => new OpeningHourResponse(
                openingHour.Id,
                openingHour.DayOfWeek,
                openingHour.OpensAt,
                openingHour.ClosesAt))
            .ToList();

        return new SetOpeningHoursResponse(restaurant.Id, responseOpeningHours);
    }
}
