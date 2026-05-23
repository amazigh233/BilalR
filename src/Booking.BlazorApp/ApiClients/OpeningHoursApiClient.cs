using System.Net.Http.Json;

namespace Booking.BlazorApp.ApiClients;

public sealed class OpeningHoursApiClient(HttpClient httpClient) : BookingApiClientBase(httpClient)
{
    public async Task<OpeningHoursDto> GetAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync(
            $"api/restaurants/{restaurantId}/opening-hours",
            cancellationToken);

        return await ReadResponseAsync<OpeningHoursDto>(response, cancellationToken);
    }

    public async Task<OpeningHoursDto> SetAsync(
        Guid restaurantId,
        SetOpeningHoursRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.PostAsJsonAsync(
            $"api/restaurants/{restaurantId}/opening-hours",
            request,
            JsonOptions,
            cancellationToken);

        return await ReadResponseAsync<OpeningHoursDto>(response, cancellationToken);
    }
}
