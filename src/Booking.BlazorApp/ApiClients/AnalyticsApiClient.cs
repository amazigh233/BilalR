using System.Globalization;
using Microsoft.AspNetCore.Components.Authorization;

namespace Booking.BlazorApp.ApiClients;

public sealed class AnalyticsApiClient(
    HttpClient httpClient,
    AuthenticationStateProvider authenticationStateProvider)
    : BookingApiClientBase(httpClient, authenticationStateProvider)
{
    public async Task<AnalyticsDto> GetAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var fromValue = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var toValue = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        using var response = await SendAsync(
            token => HttpClient.GetAsync(
                $"api/admin/restaurant/analytics?from={fromValue}&to={toValue}",
                token),
            cancellationToken);

        return await ReadResponseAsync<AnalyticsDto>(response, cancellationToken);
    }
}
