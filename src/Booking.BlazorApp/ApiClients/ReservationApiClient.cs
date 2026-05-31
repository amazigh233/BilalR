using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace Booking.BlazorApp.ApiClients;

public sealed class ReservationApiClient(
    HttpClient httpClient,
    AuthenticationStateProvider authenticationStateProvider)
    : BookingApiClientBase(httpClient, authenticationStateProvider)
{
    public async Task<IReadOnlyCollection<ReservationDto>> GetCurrentRestaurantAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync("api/admin/restaurant/reservations", token),
            cancellationToken);

        return await ReadResponseAsync<IReadOnlyCollection<ReservationDto>>(response, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ReservationDto>> GetByRestaurantAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync($"api/restaurants/{restaurantId}/reservations", token),
            cancellationToken);

        return await ReadResponseAsync<IReadOnlyCollection<ReservationDto>>(response, cancellationToken);
    }

    public async Task<ReservationDto> CreateAsync(
        CreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.PostAsJsonAsync("api/reservations", request, JsonOptions, token),
            cancellationToken);

        return await ReadResponseAsync<ReservationDto>(response, cancellationToken);
    }

    public async Task<ReservationDto> ChangeCurrentRestaurantStatusAsync(
        Guid reservationId,
        ReservationStatus status,
        CancellationToken cancellationToken = default)
    {
        var request = new ChangeReservationStatusRequest(status);

        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(
                    HttpMethod.Patch,
                    $"api/admin/restaurant/reservations/{reservationId}/status")
                {
                    Content = CreateJsonContent(request)
                },
                token),
            cancellationToken);

        return await ReadResponseAsync<ReservationDto>(response, cancellationToken);
    }

    public async Task<ReservationDto> ChangeStatusAsync(
        Guid reservationId,
        ReservationStatus status,
        CancellationToken cancellationToken = default)
    {
        var request = new ChangeReservationStatusRequest(status);

        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Patch, $"api/reservations/{reservationId}/status")
                {
                    Content = CreateJsonContent(request)
                },
                token),
            cancellationToken);

        return await ReadResponseAsync<ReservationDto>(response, cancellationToken);
    }
}
