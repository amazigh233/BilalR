using Microsoft.AspNetCore.Components.Authorization;

namespace Booking.BlazorApp.ApiClients;

public sealed class LeaveApiClient(
    HttpClient httpClient,
    AuthenticationStateProvider authenticationStateProvider)
    : BookingApiClientBase(httpClient, authenticationStateProvider)
{
    public async Task<LeaveRequestDto> RequestAsync(
        CreateLeaveRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, "api/staff/me/leave")
                {
                    Content = CreateJsonContent(request)
                },
                token),
            cancellationToken);

        return await ReadResponseAsync<LeaveRequestDto>(response, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LeaveRequestDto>> GetMineAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync("api/staff/me/leave", token),
            cancellationToken);

        return await ReadResponseAsync<IReadOnlyCollection<LeaveRequestDto>>(response, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LeaveRequestDto>> GetForRestaurantAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync("api/admin/restaurant/leave", token),
            cancellationToken);

        return await ReadResponseAsync<IReadOnlyCollection<LeaveRequestDto>>(response, cancellationToken);
    }

    public async Task<LeaveRequestDto> ApproveAsync(
        Guid leaveId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Patch, $"api/admin/restaurant/leave/{leaveId}/approve"),
                token),
            cancellationToken);

        return await ReadResponseAsync<LeaveRequestDto>(response, cancellationToken);
    }

    public async Task<LeaveRequestDto> DenyAsync(
        Guid leaveId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Patch, $"api/admin/restaurant/leave/{leaveId}/deny"),
                token),
            cancellationToken);

        return await ReadResponseAsync<LeaveRequestDto>(response, cancellationToken);
    }
}
