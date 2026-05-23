using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Booking.BlazorApp.ApiClients;

public abstract class BookingApiClientBase(HttpClient httpClient)
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected HttpClient HttpClient { get; } = httpClient;

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
