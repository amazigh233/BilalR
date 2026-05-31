using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace Booking.BlazorApp.ApiClients;

public sealed class RestaurantApiClient(
    HttpClient httpClient,
    AuthenticationStateProvider authenticationStateProvider)
    : BookingApiClientBase(httpClient, authenticationStateProvider)
{
    public async Task<RestaurantDto> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync("api/admin/restaurant", token),
            cancellationToken);

        return await ReadResponseAsync<RestaurantDto>(response, cancellationToken);
    }

    public async Task<IReadOnlyCollection<RestaurantDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync("api/restaurants", token),
            cancellationToken);

        return await ReadResponseAsync<IReadOnlyCollection<RestaurantDto>>(response, cancellationToken);
    }

    public async Task<RestaurantDto> GetByIdAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync($"api/restaurants/{restaurantId}", token),
            cancellationToken);

        return await ReadResponseAsync<RestaurantDto>(response, cancellationToken);
    }

    public async Task<RestaurantDto> UpdateCurrentAsync(
        UpdateRestaurantRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.PutAsJsonAsync("api/admin/restaurant", request, JsonOptions, token),
            cancellationToken);

        return await ReadResponseAsync<RestaurantDto>(response, cancellationToken);
    }

    public async Task<RestaurantDto> CreateAsync(
        CreateRestaurantRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.PostAsJsonAsync("api/restaurants", request, JsonOptions, token),
            cancellationToken);

        return await ReadResponseAsync<RestaurantDto>(response, cancellationToken);
    }
}
