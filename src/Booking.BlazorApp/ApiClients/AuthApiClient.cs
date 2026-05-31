using System.Net.Http.Json;

namespace Booking.BlazorApp.ApiClients;

public sealed class AuthApiClient(HttpClient httpClient) : BookingApiClientBase(httpClient)
{
    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.PostAsJsonAsync("api/auth/login", request, JsonOptions, token),
            cancellationToken);

        return await ReadResponseAsync<LoginResponse>(response, cancellationToken);
    }
}
