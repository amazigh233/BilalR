using Booking.Application.Tests.Fakes;
using Booking.Application.WidgetSettings;
using Booking.Domain.Restaurants;

namespace Booking.Application.Tests.WidgetSettings;

public sealed class SetWidgetOriginsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_NormalizesAndDeduplicatesOrigins()
    {
        var repository = new FakeRestaurantRepository();
        var restaurant = new Restaurant("Widget Test");
        repository.Restaurants.Add(restaurant);
        var useCase = new SetWidgetOriginsUseCase(repository);

        var response = await useCase.ExecuteAsync(new SetWidgetOriginsRequest(
            restaurant.Id,
            ["HTTPS://Example.com", "https://example.com/", "http://localhost:8080"]));

        Assert.Equal(
            ["http://localhost:8080", "https://example.com"],
            response.Origins);
    }

    [Theory]
    [InlineData("https://example.com/path")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://user:password@example.com")]
    public async Task ExecuteAsync_RejectsInvalidOrigins(string origin)
    {
        var repository = new FakeRestaurantRepository();
        var restaurant = new Restaurant("Widget Test");
        repository.Restaurants.Add(restaurant);
        var useCase = new SetWidgetOriginsUseCase(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(new SetWidgetOriginsRequest(restaurant.Id, [origin])));
    }
}
