namespace Booking.BlazorApp.ApiClients;

public sealed class WidgetSecurityApiClient(HttpClient httpClient)
    : BookingApiClientBase(httpClient)
{
    public async Task<WidgetOriginsDto> GetAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync(
                $"api/restaurants/{restaurantId}/widget-origins",
                token),
            cancellationToken);

        return await ReadResponseAsync<WidgetOriginsDto>(response, cancellationToken);
    }
}
