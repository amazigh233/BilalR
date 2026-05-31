using Booking.Application.Restaurants;
using Booking.Application.Tests.Fakes;
using Booking.Domain.Restaurants;

namespace Booking.Application.Tests.Restaurants;

public sealed class UpdateRestaurantUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_UpdatesRestaurantDetails()
    {
        var repository = new FakeRestaurantRepository();
        var restaurant = new Restaurant("Old Name", "123", "old@example.com");
        repository.Restaurants.Add(restaurant);

        var useCase = new UpdateRestaurantUseCase(repository);

        var response = await useCase.ExecuteAsync(new UpdateRestaurantRequest(
            restaurant.Id,
            "New Name",
            "456",
            "new@example.com"));

        Assert.Equal("New Name", restaurant.Name);
        Assert.Equal("456", restaurant.PhoneNumber);
        Assert.Equal("new@example.com", restaurant.Email);
        Assert.Equal("New Name", response.Name);
    }
}
