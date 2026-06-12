using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;

namespace Booking.BlazorApp.ApiClients;

public sealed class WidgetSettingsApiClient(
    HttpClient httpClient,
    AuthenticationStateProvider authenticationStateProvider)
    : BookingApiClientBase(httpClient, authenticationStateProvider)
{
    public async Task<WidgetOriginsDto> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync("api/admin/restaurant/widget-origins", token),
            cancellationToken);

        return await ReadResponseAsync<WidgetOriginsDto>(response, cancellationToken);
    }

    public async Task<WidgetOriginsDto> SetCurrentAsync(
        SetWidgetOriginsRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.PutAsJsonAsync(
                "api/admin/restaurant/widget-origins",
                request,
                JsonOptions,
                token),
            cancellationToken);

        return await ReadResponseAsync<WidgetOriginsDto>(response, cancellationToken);
    }

    public async Task<WidgetBrandingDto> GetCurrentBrandingAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.GetAsync("api/admin/restaurant/widget-branding", token),
            cancellationToken);

        return await ReadResponseAsync<WidgetBrandingDto>(response, cancellationToken);
    }

    public async Task<WidgetBrandingDto> SetCurrentBrandingAsync(
        SetWidgetBrandingRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.PutAsJsonAsync(
                "api/admin/restaurant/widget-branding",
                request,
                JsonOptions,
                token),
            cancellationToken);

        return await ReadResponseAsync<WidgetBrandingDto>(response, cancellationToken);
    }

    public async Task<WidgetBrandingDto> UploadLogoAsync(
        Stream content,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(content);
        if (MediaTypeHeaderValue.TryParse(contentType, out var parsedContentType))
        {
            fileContent.Headers.ContentType = parsedContentType;
        }

        form.Add(fileContent, "file", fileName);

        using var response = await SendAsync(
            token => HttpClient.PostAsync(
                "api/admin/restaurant/widget-logo",
                form,
                token),
            cancellationToken);

        return await ReadResponseAsync<WidgetBrandingDto>(response, cancellationToken);
    }

    public async Task<WidgetBrandingDto> DeleteLogoAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            token => HttpClient.DeleteAsync("api/admin/restaurant/widget-logo", token),
            cancellationToken);

        return await ReadResponseAsync<WidgetBrandingDto>(response, cancellationToken);
    }
}
