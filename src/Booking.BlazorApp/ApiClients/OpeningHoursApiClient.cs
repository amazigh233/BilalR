using System.Net.Http.Json;

namespace Booking.BlazorApp.ApiClients;

public sealed class OpeningHoursApiClient(HttpClient httpClient) : BookingApiClientBase(httpClient)
{
    public async Task<OpeningHoursDto> GetAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync($"api/restaurants/{restaurantId}/opening-hours", token),
            cancellationToken);

        return await ReadResponseAsync<OpeningHoursDto>(response, cancellationToken);
    }

    public async Task<OpeningHoursDto> SetAsync(
        Guid restaurantId,
        SetOpeningHoursRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.PostAsJsonAsync(
                $"api/restaurants/{restaurantId}/opening-hours",
                request,
                JsonOptions,
                token),
            cancellationToken);

        return await ReadResponseAsync<OpeningHoursDto>(response, cancellationToken);
    }
}
