using Microsoft.AspNetCore.Components.Authorization;

namespace Booking.BlazorApp.ApiClients;

public sealed class DeliveryApiClient(
    HttpClient httpClient,
    AuthenticationStateProvider authenticationStateProvider)
    : BookingApiClientBase(httpClient, authenticationStateProvider)
{
    public async Task<IReadOnlyCollection<DeliveryOrderDto>> GetOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync("api/admin/restaurant/delivery-orders", token),
            cancellationToken);

        return await ReadResponseAsync<IReadOnlyCollection<DeliveryOrderDto>>(response, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DeliveryIntegrationDto>> GetIntegrationsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync("api/admin/restaurant/delivery-integrations", token),
            cancellationToken);

        return await ReadResponseAsync<IReadOnlyCollection<DeliveryIntegrationDto>>(response, cancellationToken);
    }

    public async Task<ConnectDeliveryDto> ConnectAsync(
        DeliveryProvider provider,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, $"api/admin/restaurant/delivery-integrations/{provider}/connect"),
                token),
            cancellationToken);

        return await ReadResponseAsync<ConnectDeliveryDto>(response, cancellationToken);
    }

    public async Task<DeliveryIntegrationDto> DisableAsync(
        DeliveryProvider provider,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, $"api/admin/restaurant/delivery-integrations/{provider}/disable"),
                token),
            cancellationToken);

        return await ReadResponseAsync<DeliveryIntegrationDto>(response, cancellationToken);
    }
}
