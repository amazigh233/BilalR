using Booking.Application.Availability;
using Booking.Application.Tests.Fakes;
using Booking.Domain.Restaurants;

namespace Booking.Application.Tests.Availability;

public sealed class AvailabilityServiceTests
{
    [Fact]
    public async Task CheckAsync_AllowsReservationBeforeMidnightForOvernightOpeningHours()
    {
        var restaurantRepository = new FakeRestaurantRepository();
        var restaurant = await CreateRestaurantWithMondayOvernightHoursAsync(restaurantRepository);
        var service = new AvailabilityService(restaurantRepository);

        var result = await service.CheckAsync(
            restaurant.Id,
            new DateTime(2026, 06, 01, 23, 30, 00),
            2);

        Assert.True(result.IsAvailable);
    }

    [Fact]
    public async Task CheckAsync_AllowsReservationAfterMidnightForPreviousDaysOpeningHours()
    {
        var restaurantRepository = new FakeRestaurantRepository();
        var restaurant = await CreateRestaurantWithMondayOvernightHoursAsync(restaurantRepository);
        var service = new AvailabilityService(restaurantRepository);

        var result = await service.CheckAsync(
            restaurant.Id,
            new DateTime(2026, 06, 02, 00, 30, 00),
            2);

        Assert.True(result.IsAvailable);
    }

    [Fact]
    public async Task CheckAsync_RejectsReservationAtOvernightClosingTime()
    {
        var restaurantRepository = new FakeRestaurantRepository();
        var restaurant = await CreateRestaurantWithMondayOvernightHoursAsync(restaurantRepository);
        var service = new AvailabilityService(restaurantRepository);

        var result = await service.CheckAsync(
            restaurant.Id,
            new DateTime(2026, 06, 02, 01, 00, 00),
            2);

        Assert.False(result.IsAvailable);
        Assert.Equal("Het restaurant is gesloten op het gekozen tijdstip.", result.Reason);
    }

    private static async Task<Restaurant> CreateRestaurantWithMondayOvernightHoursAsync(
        FakeRestaurantRepository restaurantRepository)
    {
        var restaurant = new Restaurant("Sultana BBQ");

        await restaurantRepository.AddAsync(restaurant);
        await restaurantRepository.SetOpeningHoursAsync(
            restaurant.Id,
            [
                new OpeningHour(
                    DayOfWeek.Monday,
                    new TimeOnly(17, 00),
                    new TimeOnly(01, 00))
            ]);

        return restaurant;
    }
}
