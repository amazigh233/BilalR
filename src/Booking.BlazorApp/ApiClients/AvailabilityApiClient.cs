using System.Globalization;
using Microsoft.AspNetCore.Components.Authorization;

namespace Booking.BlazorApp.ApiClients;

public sealed class AvailabilityApiClient(
    HttpClient httpClient,
    AuthenticationStateProvider authenticationStateProvider)
    : BookingApiClientBase(httpClient, authenticationStateProvider)
{
    public async Task<AvailabilityDto> CheckAsync(
        Guid restaurantId,
        DateTime reservationDateTime,
        int partySize,
        CancellationToken cancellationToken = default)
    {
        var dateTime = Uri.EscapeDataString(
            reservationDateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));

        using var response = await SendAsync(
            token => HttpClient.GetAsync(
                $"api/restaurants/{restaurantId}/availability?dateTime={dateTime}&partySize={partySize}",
                token),
            cancellationToken);

        return await ReadResponseAsync<AvailabilityDto>(response, cancellationToken);
    }
}
