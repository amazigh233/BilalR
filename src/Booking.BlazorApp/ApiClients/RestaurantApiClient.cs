using System.Net.Http.Json;

namespace Booking.BlazorApp.ApiClients;

public sealed class RestaurantApiClient(HttpClient httpClient) : BookingApiClientBase(httpClient)
{
    public async Task<IReadOnlyCollection<RestaurantDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync("api/restaurants", cancellationToken);

        return await ReadResponseAsync<IReadOnlyCollection<RestaurantDto>>(response, cancellationToken);
    }

    public async Task<RestaurantDto> GetByIdAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync(
            $"api/restaurants/{restaurantId}",
            cancellationToken);

        return await ReadResponseAsync<RestaurantDto>(response, cancellationToken);
    }

    public async Task<RestaurantDto> CreateAsync(
        CreateRestaurantRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.PostAsJsonAsync(
            "api/restaurants",
            request,
            JsonOptions,
            cancellationToken);

        return await ReadResponseAsync<RestaurantDto>(response, cancellationToken);
    }
}
