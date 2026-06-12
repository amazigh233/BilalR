using Booking.BlazorApp.Security;
using Microsoft.Extensions.Configuration;

namespace Booking.BlazorApp.Tests;

public sealed class WidgetSecurityPolicyTests
{
    [Fact]
    public void CreateFrameAncestorsDirective_DefaultsToSelf()
    {
        var configuration = BuildConfiguration([]);

        var directive = WidgetSecurityPolicy.CreateFrameAncestorsDirective(configuration);

        Assert.Equal("frame-ancestors 'self'", directive);
    }

    [Fact]
    public void CreateFrameAncestorsDirective_AcceptsConfiguredOrigins()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Widget:AllowedFrameAncestors"] =
                "'self' https://booking.example.com https://*.restaurant-group.example"
        });

        var directive = WidgetSecurityPolicy.CreateFrameAncestorsDirective(configuration);

        Assert.Equal(
            "frame-ancestors 'self' https://booking.example.com https://*.restaurant-group.example",
            directive);
    }

    [Fact]
    public void CreateFrameAncestorsDirective_AddsRestaurantOrigins()
    {
        var configuration = BuildConfiguration([]);

        var directive = WidgetSecurityPolicy.CreateFrameAncestorsDirective(
            configuration,
            ["https://restaurant.example"]);

        Assert.Equal(
            "frame-ancestors 'self' https://restaurant.example",
            directive);
    }

    [Fact]
    public void TryGetRestaurantId_ReadsEmbedBookingRoute()
    {
        var restaurantId = Guid.NewGuid();

        var parsed = WidgetSecurityPolicy.TryGetRestaurantId(
            $"/embed/booking/{restaurantId}",
            out var parsedRestaurantId);

        Assert.True(parsed);
        Assert.Equal(restaurantId, parsedRestaurantId);
    }

    [Theory]
    [InlineData("https://example.com/path")]
    [InlineData("'none' https://example.com")]
    [InlineData("https://example.com\r\nX-Frame-Options: ALLOWALL")]
    public void CreateFrameAncestorsDirective_RejectsUnsafeConfiguration(string value)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Widget:AllowedFrameAncestors"] = value
        });

        Assert.Throws<InvalidOperationException>(
            () => WidgetSecurityPolicy.CreateFrameAncestorsDirective(configuration));
    }

    private static IConfiguration BuildConfiguration(
        Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
