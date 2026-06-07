using Microsoft.AspNetCore.Components.Authorization;

namespace Booking.BlazorApp.ApiClients;

public sealed class StaffAvailabilityApiClient(
    HttpClient httpClient,
    AuthenticationStateProvider authenticationStateProvider)
    : BookingApiClientBase(httpClient, authenticationStateProvider)
{
    public async Task<IReadOnlyCollection<AvailabilitySlotDto>> GetMineAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync("api/staff/me/availability", token),
            cancellationToken);

        return await ReadResponseAsync<IReadOnlyCollection<AvailabilitySlotDto>>(response, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AvailabilitySlotDto>> SetMineAsync(
        SetAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Put, "api/staff/me/availability")
                {
                    Content = CreateJsonContent(request)
                },
                token),
            cancellationToken);

        return await ReadResponseAsync<IReadOnlyCollection<AvailabilitySlotDto>>(response, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AvailabilitySlotDto>> GetForRestaurantAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync("api/admin/restaurant/availability", token),
            cancellationToken);

        return await ReadResponseAsync<IReadOnlyCollection<AvailabilitySlotDto>>(response, cancellationToken);
    }
}
