using System.Globalization;
using Microsoft.AspNetCore.Components.Authorization;

namespace Booking.BlazorApp.ApiClients;

public sealed class ShiftApiClient(
    HttpClient httpClient,
    AuthenticationStateProvider authenticationStateProvider)
    : BookingApiClientBase(httpClient, authenticationStateProvider)
{
    public async Task<IReadOnlyCollection<ShiftDto>> GetForRestaurantAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync($"api/admin/restaurant/shifts?{Range(from, to)}", token),
            cancellationToken);

        return await ReadResponseAsync<IReadOnlyCollection<ShiftDto>>(response, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ShiftDto>> GetMineAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync($"api/staff/me/shifts?{Range(from, to)}", token),
            cancellationToken);

        return await ReadResponseAsync<IReadOnlyCollection<ShiftDto>>(response, cancellationToken);
    }

    public async Task<ShiftDto> CreateAsync(
        CreateShiftRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.PostAsync("api/admin/restaurant/shifts", CreateJsonContent(request), token),
            cancellationToken);

        return await ReadResponseAsync<ShiftDto>(response, cancellationToken);
    }

    public async Task<ShiftDto> UpdateAsync(
        Guid shiftId,
        UpdateShiftRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Put, $"api/admin/restaurant/shifts/{shiftId}")
                {
                    Content = CreateJsonContent(request)
                },
                token),
            cancellationToken);

        return await ReadResponseAsync<ShiftDto>(response, cancellationToken);
    }

    public async Task DeleteAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.DeleteAsync($"api/admin/restaurant/shifts/{shiftId}", token),
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static string Range(DateOnly from, DateOnly to)
    {
        var fromValue = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var toValue = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return $"from={fromValue}&to={toValue}";
    }
}
