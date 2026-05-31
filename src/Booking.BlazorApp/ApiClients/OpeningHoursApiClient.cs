using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace Booking.BlazorApp.ApiClients;

public sealed class OpeningHoursApiClient(
    HttpClient httpClient,
    AuthenticationStateProvider authenticationStateProvider)
    : BookingApiClientBase(httpClient, authenticationStateProvider)
{
    public async Task<OpeningHoursDto> GetCurrentRestaurantAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync("api/admin/restaurant/opening-hours", token),
            cancellationToken);

        return await ReadResponseAsync<OpeningHoursDto>(response, cancellationToken);
    }

    public async Task<OpeningHoursDto> GetAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync($"api/restaurants/{restaurantId}/opening-hours", token),
            cancellationToken);

        return await ReadResponseAsync<OpeningHoursDto>(response, cancellationToken);
    }

    public async Task<OpeningHoursDto> SetCurrentRestaurantAsync(
        SetOpeningHoursRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.PostAsJsonAsync(
                "api/admin/restaurant/opening-hours",
                request,
                JsonOptions,
                token),
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
