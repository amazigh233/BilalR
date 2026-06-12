using Booking.Application.Abstractions;
using Booking.Domain.Restaurants;

namespace Booking.Application.WidgetSettings;

public sealed class SetWidgetOriginsUseCase(IRestaurantRepository restaurantRepository)
{
    private const int MaximumOrigins = 20;

    public async Task<WidgetOriginsResponse> ExecuteAsync(
        SetWidgetOriginsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.RestaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(request));
        }

        if (request.Origins is null)
        {
            throw new ArgumentException("Origins are required.", nameof(request));
        }

        if (await restaurantRepository.GetByIdAsync(request.RestaurantId, cancellationToken) is null)
        {
            throw new KeyNotFoundException("Restaurant was not found.");
        }

        var normalizedOrigins = request.Origins
            .Select(WidgetAllowedOrigin.Normalize)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(origin => origin, StringComparer.Ordinal)
            .ToList();

        if (normalizedOrigins.Count > MaximumOrigins)
        {
            throw new ArgumentException(
                $"Er mogen maximaal {MaximumOrigins} websites worden toegevoegd.",
                nameof(request));
        }

        var origins = normalizedOrigins
            .Select(origin => new WidgetAllowedOrigin(request.RestaurantId, origin))
            .ToList();

        await restaurantRepository.SetWidgetAllowedOriginsAsync(
            request.RestaurantId,
            origins,
            cancellationToken);

        return new WidgetOriginsResponse(request.RestaurantId, normalizedOrigins);
    }
}
