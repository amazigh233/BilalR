using Booking.Application.Availability;
using Booking.Application.Reservations;
using Booking.Application.Tests.Fakes;
using Booking.Domain.Restaurants;

namespace Booking.Application.Tests.Reservations;

public sealed class CreateReservationUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_RejectsReservationOutsideOpeningHours()
    {
        var restaurantRepository = new FakeRestaurantRepository();
        var reservationRepository = new FakeReservationRepository();
        var restaurant = new Restaurant("Sultana BBQ");

        await restaurantRepository.AddAsync(restaurant);
        await restaurantRepository.SetOpeningHoursAsync(
            restaurant.Id,
            [
                new OpeningHour(
                    DayOfWeek.Monday,
                    new TimeOnly(17, 00),
                    new TimeOnly(22, 00))
            ]);

        var useCase = new CreateReservationUseCase(
            restaurantRepository,
            reservationRepository,
            new AvailabilityService(restaurantRepository));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(new CreateReservationRequest(
                restaurant.Id,
                new DateTime(2026, 06, 01, 16, 30, 00),
                2,
                new CustomerRequest("Test Customer", "test@example.com", null),
                null)));

        Assert.Equal("Het restaurant is gesloten op het gekozen tijdstip.", exception.Message);
        Assert.Empty(reservationRepository.Reservations);
    }
}
