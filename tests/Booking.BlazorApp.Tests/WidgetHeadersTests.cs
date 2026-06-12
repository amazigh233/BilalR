using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Booking.BlazorApp.Tests;

public sealed class WidgetHeadersTests : IClassFixture<WidgetAppFactory>
{
    private readonly WidgetAppFactory _factory;

    public WidgetHeadersTests(WidgetAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EmbedRoute_UsesConfiguredFrameAncestors()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/embed/not-a-component-route");

        Assert.True(response.Headers.TryGetValues("Content-Security-Policy", out var values));
        Assert.Equal(
            "frame-ancestors 'self' https://booking.example.com",
            Assert.Single(values));
        Assert.False(response.Headers.Contains("X-Frame-Options"));
    }

    [Fact]
    public async Task NonEmbedRoute_DoesNotUseWidgetFrameAllowlist()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/not-a-component-route");

        var hasWidgetPolicy =
            response.Headers.TryGetValues("Content-Security-Policy", out var values) &&
            values.Contains("frame-ancestors 'self' https://booking.example.com");
        Assert.False(hasWidgetPolicy);
    }
}

public sealed class WidgetAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Widget:AllowedFrameAncestors"] =
                    "'self' https://booking.example.com",
                ["BookingApi:BaseUrl"] = "http://localhost"
            });
        });
    }
}
