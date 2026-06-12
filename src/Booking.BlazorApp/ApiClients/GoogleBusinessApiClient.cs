using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace Booking.BlazorApp.ApiClients;

public sealed class GoogleBusinessApiClient(
    HttpClient httpClient,
    AuthenticationStateProvider authenticationStateProvider)
    : BookingApiClientBase(httpClient, authenticationStateProvider)
{
    public async Task<GbpStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync("api/admin/restaurant/google-business/status", token), ct);
        return await ReadResponseAsync<GbpStatusDto>(response, ct);
    }

    public async Task<GbpConnectDto> StartConnectAsync(CancellationToken ct = default)
    {
        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, "api/admin/restaurant/google-business/connect"), token), ct);
        return await ReadResponseAsync<GbpConnectDto>(response, ct);
    }

    public async Task<IReadOnlyCollection<GbpLocationSummaryDto>> CompleteOAuthAsync(
        string state, string code, CancellationToken ct = default)
    {
        using var response = await SendAsync(
            token => HttpClient.PostAsJsonAsync(
                "api/admin/restaurant/google-business/complete",
                new GbpCompleteRequest(state, code), JsonOptions, token), ct);
        return await ReadResponseAsync<IReadOnlyCollection<GbpLocationSummaryDto>>(response, ct);
    }

    public async Task SelectLocationAsync(string accountName, string locationName, CancellationToken ct = default)
    {
        using var response = await SendAsync(
            token => HttpClient.PostAsJsonAsync(
                "api/admin/restaurant/google-business/select-location",
                new GbpSelectLocationRequest(accountName, locationName), JsonOptions, token), ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, "api/admin/restaurant/google-business/disconnect"), token), ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task<IReadOnlyCollection<GbpReviewDto>> GetReviewsAsync(CancellationToken ct = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync("api/admin/restaurant/google-business/reviews", token), ct);
        return await ReadResponseAsync<IReadOnlyCollection<GbpReviewDto>>(response, ct);
    }

    public async Task UpsertReplyAsync(string reviewName, string comment, CancellationToken ct = default)
    {
        var encoded = Uri.EscapeDataString(reviewName);
        using var response = await SendAsync(
            token => HttpClient.PostAsJsonAsync(
                $"api/admin/restaurant/google-business/reviews/{encoded}/reply",
                new GbpReplyRequest(comment), JsonOptions, token), ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task DeleteReplyAsync(string reviewName, CancellationToken ct = default)
    {
        var encoded = Uri.EscapeDataString(reviewName);
        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Delete, $"api/admin/restaurant/google-business/reviews/{encoded}/reply"),
                token), ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task MarkReadAsync(string reviewName, CancellationToken ct = default)
    {
        var encoded = Uri.EscapeDataString(reviewName);
        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, $"api/admin/restaurant/google-business/reviews/{encoded}/read"),
                token), ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task SyncHoursAsync(CancellationToken ct = default)
    {
        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, "api/admin/restaurant/google-business/sync/hours"),
                token), ct);
        await EnsureSuccessAsync(response, ct);
    }
}
