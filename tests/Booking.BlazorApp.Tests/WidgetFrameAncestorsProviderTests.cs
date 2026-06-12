using System.Net;
using System.Text;
using Booking.BlazorApp.ApiClients;
using Booking.BlazorApp.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Booking.BlazorApp.Tests;

public sealed class WidgetFrameAncestorsProviderTests
{
    [Fact]
    public async Task GetDirectiveAsync_AddsRestaurantOrigins()
    {
        var restaurantId = Guid.NewGuid();
        var provider = CreateProvider(
            HttpStatusCode.OK,
            $$"""{"restaurantId":"{{restaurantId}}","origins":["https://restaurant.example"]}""");

        var directive = await provider.GetDirectiveAsync(restaurantId);

        Assert.Equal(
            "frame-ancestors 'self' https://restaurant.example",
            directive);
    }

    [Fact]
    public async Task GetDirectiveAsync_FailsClosedWhenApiIsUnavailable()
    {
        var provider = CreateProvider(HttpStatusCode.ServiceUnavailable, "{}");

        var directive = await provider.GetDirectiveAsync(Guid.NewGuid());

        Assert.Equal("frame-ancestors 'self'", directive);
    }

    private static WidgetFrameAncestorsProvider CreateProvider(
        HttpStatusCode statusCode,
        string content)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Widget:AllowedFrameAncestors:0"] = "'self'"
            })
            .Build();
        var httpClient = new HttpClient(new StubHttpMessageHandler(statusCode, content))
        {
            BaseAddress = new Uri("http://booking-api")
        };
        var apiClient = new WidgetSecurityApiClient(httpClient);

        return new WidgetFrameAncestorsProvider(
            configuration,
            apiClient,
            NullLogger<WidgetFrameAncestorsProvider>.Instance);
    }

    private sealed class StubHttpMessageHandler(
        HttpStatusCode statusCode,
        string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
        }
    }
}
