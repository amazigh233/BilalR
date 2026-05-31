using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace Booking.BlazorApp.ApiClients;

public sealed class RestaurantAccountApiClient(
    HttpClient httpClient,
    AuthenticationStateProvider authenticationStateProvider)
    : BookingApiClientBase(httpClient, authenticationStateProvider)
{
    public async Task<RestaurantAccountDto> CreateAsync(
        CreateRestaurantAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.PostAsJsonAsync(
                "api/restaurant-accounts",
                request,
                JsonOptions,
                token),
            cancellationToken);

        return await ReadResponseAsync<RestaurantAccountDto>(response, cancellationToken);
    }
}
