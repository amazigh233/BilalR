using System.Net;

namespace Booking.BlazorApp.ApiClients;

public sealed class ApiClientException(string message, HttpStatusCode statusCode) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
