using Booking.BlazorApp.ApiClients;

namespace Booking.BlazorApp.Security;

public sealed class WidgetFrameAncestorsProvider(
    IConfiguration configuration,
    WidgetSecurityApiClient widgetSecurityApiClient,
    ILogger<WidgetFrameAncestorsProvider> logger)
{
    public async Task<string> GetDirectiveAsync(
        Guid? restaurantId,
        CancellationToken cancellationToken = default)
    {
        if (!restaurantId.HasValue)
        {
            return WidgetSecurityPolicy.CreateFrameAncestorsDirective(configuration);
        }

        try
        {
            var settings = await widgetSecurityApiClient.GetAsync(
                restaurantId.Value,
                cancellationToken);

            return WidgetSecurityPolicy.CreateFrameAncestorsDirective(
                configuration,
                settings.Origins);
        }
        catch (ApiClientException exception)
        {
            logger.LogWarning(
                exception,
                "Could not load widget origins for restaurant {RestaurantId}. Using the global allowlist.",
                restaurantId);

            return WidgetSecurityPolicy.CreateFrameAncestorsDirective(configuration);
        }
    }
}
