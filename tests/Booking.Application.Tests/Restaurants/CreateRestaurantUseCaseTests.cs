using Booking.Application.Restaurants;
using Booking.Application.Tests.Fakes;

namespace Booking.Application.Tests.Restaurants;

public sealed class CreateRestaurantUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesRestaurant()
    {
        var repository = new FakeRestaurantRepository();
        var useCase = new CreateRestaurantUseCase(repository);

        var response = await useCase.ExecuteAsync(new CreateRestaurantRequest(
            "Sultana BBQ",
            "0612345678",
            "info@sultana.test"));

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("Sultana BBQ", response.Name);
        Assert.Single(repository.Restaurants);
    }
}
