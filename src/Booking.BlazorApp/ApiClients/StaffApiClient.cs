using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace Booking.BlazorApp.ApiClients;

public sealed class StaffApiClient(
    HttpClient httpClient,
    AuthenticationStateProvider authenticationStateProvider)
    : BookingApiClientBase(httpClient, authenticationStateProvider)
{
    public async Task<IReadOnlyCollection<StaffUserDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync("api/admin/staff", token),
            cancellationToken);

        return await ReadResponseAsync<IReadOnlyCollection<StaffUserDto>>(response, cancellationToken);
    }

    public async Task<StaffUserDto> CreateAsync(
        CreateStaffRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.PostAsJsonAsync(
                "api/admin/staff",
                request,
                JsonOptions,
                token),
            cancellationToken);

        return await ReadResponseAsync<StaffUserDto>(response, cancellationToken);
    }

    public async Task<StaffUserDto> DisableAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(
                    HttpMethod.Patch,
                    $"api/admin/staff/{userId}/disable"),
                token),
            cancellationToken);

        return await ReadResponseAsync<StaffUserDto>(response, cancellationToken);
    }

    public async Task<StaffUserDto> EnableAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(
                    HttpMethod.Patch,
                    $"api/admin/staff/{userId}/enable"),
                token),
            cancellationToken);

        return await ReadResponseAsync<StaffUserDto>(response, cancellationToken);
    }
}
