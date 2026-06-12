using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace Booking.BlazorApp.ApiClients;

public sealed class TableApiClient(
    HttpClient httpClient,
    AuthenticationStateProvider authenticationStateProvider)
    : BookingApiClientBase(httpClient, authenticationStateProvider)
{
    public async Task<IReadOnlyList<TableDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync("api/admin/restaurant/tables", token),
            cancellationToken);

        return await ReadResponseAsync<IReadOnlyList<TableDto>>(response, cancellationToken);
    }

    public async Task<TableDto> CreateAsync(
        CreateTableRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.PostAsJsonAsync("api/admin/restaurant/tables", request, JsonOptions, token),
            cancellationToken);

        return await ReadResponseAsync<TableDto>(response, cancellationToken);
    }

    public async Task<TableDto> UpdateAsync(
        Guid tableId,
        UpdateTableRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Put, $"api/admin/restaurant/tables/{tableId}")
                {
                    Content = JsonContent.Create(request, options: JsonOptions)
                },
                token),
            cancellationToken);

        return await ReadResponseAsync<TableDto>(response, cancellationToken);
    }

    public async Task DeactivateAsync(Guid tableId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Delete, $"api/admin/restaurant/tables/{tableId}"),
                token),
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task ReactivateAsync(Guid tableId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, $"api/admin/restaurant/tables/{tableId}/reactivate"),
                token),
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task UpdateLayoutAsync(
        Guid tableId,
        UpdateTableLayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Patch, $"api/admin/restaurant/tables/{tableId}/layout")
                {
                    Content = JsonContent.Create(request, options: JsonOptions)
                },
                token),
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task AssignToReservationAsync(
        Guid reservationId,
        Guid? tableId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(
                    HttpMethod.Patch,
                    $"api/admin/restaurant/reservations/{reservationId}/table")
                {
                    Content = JsonContent.Create(new { tableId }, options: JsonOptions)
                },
                token),
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }
}
