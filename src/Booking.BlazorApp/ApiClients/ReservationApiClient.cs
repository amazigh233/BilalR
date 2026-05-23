using System.Net.Http.Json;

namespace Booking.BlazorApp.ApiClients;

public sealed class ReservationApiClient(HttpClient httpClient) : BookingApiClientBase(httpClient)
{
    public async Task<IReadOnlyCollection<ReservationDto>> GetByRestaurantAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync(
            $"api/restaurants/{restaurantId}/reservations",
            cancellationToken);

        return await ReadResponseAsync<IReadOnlyCollection<ReservationDto>>(response, cancellationToken);
    }

    public async Task<ReservationDto> CreateAsync(
        CreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.PostAsJsonAsync(
            "api/reservations",
            request,
            JsonOptions,
            cancellationToken);

        return await ReadResponseAsync<ReservationDto>(response, cancellationToken);
    }

    public async Task<ReservationDto> ChangeStatusAsync(
        Guid reservationId,
        ReservationStatus status,
        CancellationToken cancellationToken = default)
    {
        var request = new ChangeReservationStatusRequest(status);

        using var response = await HttpClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Patch, $"api/reservations/{reservationId}/status")
            {
                Content = CreateJsonContent(request)
            },
            cancellationToken);

        return await ReadResponseAsync<ReservationDto>(response, cancellationToken);
    }
}
