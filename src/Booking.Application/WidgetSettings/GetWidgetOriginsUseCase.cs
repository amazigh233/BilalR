using Booking.Application.Abstractions;

namespace Booking.Application.WidgetSettings;

public sealed class GetWidgetOriginsUseCase(IRestaurantRepository restaurantRepository)
{
    public async Task<WidgetOriginsResponse> ExecuteAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        if (restaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(restaurantId));
        }

        if (await restaurantRepository.GetByIdAsync(restaurantId, cancellationToken) is null)
        {
            throw new KeyNotFoundException("Restaurant was not found.");
        }

        var origins = await restaurantRepository.GetWidgetAllowedOriginsAsync(
            restaurantId,
            cancellationToken);

        return new WidgetOriginsResponse(
            restaurantId,
            origins.Select(origin => origin.Origin).ToList());
    }
}
