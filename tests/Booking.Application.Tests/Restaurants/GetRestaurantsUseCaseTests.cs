using Booking.Application.Restaurants;
using Booking.Application.Tests.Fakes;
using Booking.Domain.Restaurants;

namespace Booking.Application.Tests.Restaurants;

public sealed class GetRestaurantsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsRestaurantsOrderedByName()
    {
        var repository = new FakeRestaurantRepository();
        repository.Restaurants.Add(new Restaurant("Zaalzicht"));
        repository.Restaurants.Add(new Restaurant("Sultana BBQ"));

        var useCase = new GetRestaurantsUseCase(repository);

        var response = await useCase.ExecuteAsync();

        Assert.Collection(
            response,
            restaurant => Assert.Equal("Sultana BBQ", restaurant.Name),
            restaurant => Assert.Equal("Zaalzicht", restaurant.Name));
    }
}
