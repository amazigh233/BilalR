using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Booking.BlazorApp.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

namespace Booking.BlazorApp.ApiClients;

public abstract class BookingApiClientBase(
    HttpClient httpClient,
    AuthenticationStateProvider? authenticationStateProvider = null)
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected HttpClient HttpClient { get; } = httpClient;

    protected async Task<HttpResponseMessage> SendAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> sendAsync,
        CancellationToken cancellationToken)
    {
        try
        {
            await ApplyBearerTokenAsync();

            return await sendAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new ApiClientException(
                "De API is tijdelijk niet bereikbaar. Controleer of de API draait en probeer opnieuw.",
                HttpStatusCode.ServiceUnavailable);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiClientException(
                "De API reageerde niet op tijd. Probeer opnieuw.",
                HttpStatusCode.RequestTimeout);
        }
    }

    protected async Task<TResponse> ReadResponseAsync<TResponse>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var value = await response.Content.ReadFromJsonAsync<TResponse>(
                JsonOptions,
                cancellationToken);

            return value ?? throw new ApiClientException(
                "De API gaf een lege response terug.",
                response.StatusCode);
        }

        var message = await ReadErrorMessageAsync(response, cancellationToken);
        throw new ApiClientException(message, response.StatusCode);
    }

    protected static JsonContent CreateJsonContent<TRequest>(TRequest request)
    {
        return JsonContent.Create(request, options: JsonOptions);
    }

    private async Task ApplyBearerTokenAsync()
    {
        if (authenticationStateProvider is null)
        {
            return;
        }

        var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var accessToken = authenticationState.User
            .FindFirst(BookingAuthClaimTypes.AccessToken)
            ?.Value;

        HttpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(accessToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static async Task<string> ReadErrorMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(
                JsonOptions,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return error.Message;
            }
        }
        catch (JsonException)
        {
        }

        return $"De API-call is mislukt ({(int)response.StatusCode}).";
    }
}
