using Booking.Application.Abstractions;
using Booking.Domain.GoogleBusiness;

namespace Booking.Api.GoogleBusiness;

public sealed class GoogleBusinessHoursSyncService(
    IGoogleBusinessRepository gbpRepository,
    IRestaurantRepository restaurantRepository,
    GoogleBusinessService googleBusinessService,
    ILogger<GoogleBusinessHoursSyncService> logger)
{
    public async Task SyncAsync(Guid restaurantId, CancellationToken ct = default)
    {
        if (!googleBusinessService.IsEnabled) return;

        try
        {
            var connection = await gbpRepository.GetConnectionAsync(restaurantId, ct);
            if (connection is null || connection.Status != GoogleBusinessConnectionStatus.Connected) return;

            var hours = await restaurantRepository.GetOpeningHoursAsync(restaurantId, ct);
            var mapped = hours.Select(h => (h.DayOfWeek, h.OpensAt, h.ClosesAt)).ToList();
            await googleBusinessService.SyncOpeningHoursAsync(connection, mapped, ct);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "Google Business opening hours sync failed for restaurant {RestaurantId}.", restaurantId);
        }
    }
}
