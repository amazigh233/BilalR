using Booking.Application.Tests.Fakes;
using Booking.Application.WidgetSettings;
using Booking.Domain.Restaurants;

namespace Booking.Application.Tests.WidgetSettings;

public sealed class SetWidgetBrandingUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_UpdatesAndNormalizesBranding()
    {
        var repository = new FakeRestaurantRepository();
        var restaurant = new Restaurant("Branding Test");
        repository.Restaurants.Add(restaurant);
        var useCase = new SetWidgetBrandingUseCase(repository);

        var response = await useCase.ExecuteAsync(new SetWidgetBrandingRequest(
            restaurant.Id,
            " #ABCDEF ",
            "#123456",
            " Welkom bij ons ",
            " https://example.com/logo.png "));

        Assert.Equal("#abcdef", response.PrimaryColor);
        Assert.Equal("#123456", response.AccentColor);
        Assert.Equal("Welkom bij ons", response.WelcomeText);
        Assert.Equal("https://example.com/logo.png", response.LogoUrl);
        Assert.Equal("#abcdef", restaurant.WidgetPrimaryColor);
    }

    [Theory]
    [InlineData("green", "#123456", null)]
    [InlineData("#12345", "#123456", null)]
    [InlineData("#abcdef", "#123456", "javascript:alert(1)")]
    [InlineData("#abcdef", "#123456", "https://user:password@example.com/logo.png")]
    public async Task ExecuteAsync_RejectsInvalidBranding(
        string primaryColor,
        string accentColor,
        string? logoUrl)
    {
        var repository = new FakeRestaurantRepository();
        var restaurant = new Restaurant("Branding Test");
        repository.Restaurants.Add(restaurant);
        var useCase = new SetWidgetBrandingUseCase(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(new SetWidgetBrandingRequest(
                restaurant.Id,
                primaryColor,
                accentColor,
                null,
                logoUrl)));
    }
}
