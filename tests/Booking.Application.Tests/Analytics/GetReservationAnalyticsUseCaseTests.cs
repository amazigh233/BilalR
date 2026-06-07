using Booking.Application.Analytics;
using Booking.Application.Tests.Fakes;
using Booking.Domain.Customers;
using Booking.Domain.Reservations;
using Booking.Domain.Restaurants;

namespace Booking.Application.Tests.Analytics;

public sealed class GetReservationAnalyticsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_AggregatesReservationsInRange()
    {
        var restaurantRepository = new FakeRestaurantRepository();
        var reservationRepository = new FakeReservationRepository();

        var restaurant = new Restaurant("Sultana BBQ");
        await restaurantRepository.AddAsync(restaurant);

        // In range: 2026-06-01 .. 2026-06-07
        await Seed(reservationRepository, restaurant.Id, new DateTime(2026, 06, 01, 18, 00, 00), 2, ReservationStatus.New);
        await Seed(reservationRepository, restaurant.Id, new DateTime(2026, 06, 01, 19, 00, 00), 4, ReservationStatus.Confirmed);
        await Seed(reservationRepository, restaurant.Id, new DateTime(2026, 06, 02, 18, 00, 00), 2, ReservationStatus.NoShow);
        await Seed(reservationRepository, restaurant.Id, new DateTime(2026, 06, 03, 20, 00, 00), 6, ReservationStatus.Cancelled);
        await Seed(reservationRepository, restaurant.Id, new DateTime(2026, 06, 07, 18, 00, 00), 2, ReservationStatus.Confirmed);

        // Out of range: must be excluded.
        await Seed(reservationRepository, restaurant.Id, new DateTime(2026, 05, 31, 18, 00, 00), 10, ReservationStatus.New);
        await Seed(reservationRepository, restaurant.Id, new DateTime(2026, 06, 08, 18, 00, 00), 10, ReservationStatus.New);

        var useCase = new GetReservationAnalyticsUseCase(restaurantRepository, reservationRepository);

        var result = await useCase.ExecuteAsync(new ReservationAnalyticsRequest(
            restaurant.Id,
            new DateOnly(2026, 06, 01),
            new DateOnly(2026, 06, 07)));

        Assert.Equal(5, result.TotalReservations);
        Assert.Equal(1, result.NewCount);
        Assert.Equal(2, result.ConfirmedCount);
        Assert.Equal(1, result.CancelledCount);
        Assert.Equal(1, result.NoShowCount);
        Assert.Equal(0.2d, result.NoShowRate, 3);
        Assert.Equal(16, result.TotalGuests);
        Assert.Equal(3.2d, result.AveragePartySize, 3);

        // Per day spans exactly the requested inclusive range (7 days), out-of-range excluded.
        Assert.Equal(7, result.PerDay.Count);
        Assert.Equal(2, result.PerDay.Single(day => day.Date == new DateOnly(2026, 06, 01)).Count);
        Assert.DoesNotContain(result.PerDay, day => day.Date == new DateOnly(2026, 05, 31));

        // Per hour: 18:00 has three reservations (06-01, 06-02, 06-07).
        Assert.Equal(3, result.PerHour.Single(hour => hour.Hour == 18).Count);
        Assert.Equal(24, result.PerHour.Count);

        // Per weekday: always 7 entries summing to the total; Monday 2026-06-01 has 2.
        Assert.Equal(7, result.PerWeekday.Count);
        Assert.Equal(5, result.PerWeekday.Sum(weekday => weekday.Count));
        var firstDayWeekday = new DateTime(2026, 06, 01).DayOfWeek;
        Assert.Equal(2, result.PerWeekday.Single(weekday => weekday.DayOfWeek == firstDayWeekday).Count);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsZeros_WhenNoReservations()
    {
        var restaurantRepository = new FakeRestaurantRepository();
        var reservationRepository = new FakeReservationRepository();

        var restaurant = new Restaurant("Sultana BBQ");
        await restaurantRepository.AddAsync(restaurant);

        var useCase = new GetReservationAnalyticsUseCase(restaurantRepository, reservationRepository);

        var result = await useCase.ExecuteAsync(new ReservationAnalyticsRequest(
            restaurant.Id,
            new DateOnly(2026, 06, 01),
            new DateOnly(2026, 06, 07)));

        Assert.Equal(0, result.TotalReservations);
        Assert.Equal(0d, result.NoShowRate);
        Assert.Equal(0d, result.AveragePartySize);
        Assert.Equal(7, result.PerDay.Count);
        Assert.All(result.PerDay, day => Assert.Equal(0, day.Count));
    }

    [Fact]
    public async Task ExecuteAsync_Throws_WhenRestaurantDoesNotExist()
    {
        var restaurantRepository = new FakeRestaurantRepository();
        var reservationRepository = new FakeReservationRepository();

        var useCase = new GetReservationAnalyticsUseCase(restaurantRepository, reservationRepository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            useCase.ExecuteAsync(new ReservationAnalyticsRequest(
                Guid.NewGuid(),
                new DateOnly(2026, 06, 01),
                new DateOnly(2026, 06, 07))));
    }

    private static async Task Seed(
        FakeReservationRepository repository,
        Guid restaurantId,
        DateTime when,
        int partySize,
        ReservationStatus status)
    {
        var reservation = new Reservation(
            restaurantId,
            new Customer("Test Customer", "test@example.com"),
            when,
            partySize);

        switch (status)
        {
            case ReservationStatus.Confirmed:
                reservation.Confirm();
                break;
            case ReservationStatus.Cancelled:
                reservation.Cancel();
                break;
            case ReservationStatus.NoShow:
                reservation.MarkAsNoShow();
                break;
        }

        await repository.AddAsync(reservation);
    }
}
