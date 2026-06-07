using Booking.Application.Availability;
using Booking.Application.Reservations;
using Booking.Application.Tests.Fakes;
using Booking.Domain.Restaurants;

namespace Booking.Application.Tests.Reservations;

public sealed class CreateReservationUseCaseTests
{
    private static readonly DateTime FixedNow = new(2026, 05, 28, 12, 00, 00);

    [Fact]
    public async Task CreateReservation_Should_Throw_WhenReservationDateTimeIsInPast()
    {
        var reservationRepository = new FakeReservationRepository();
        var restaurantRepository = await CreateRestaurantRepositoryAsync(DayOfWeek.Thursday);
        var restaurant = restaurantRepository.Restaurants.Single();
        var useCase = CreateUseCase(restaurantRepository, reservationRepository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(new CreateReservationRequest(
                restaurant.Id,
                FixedNow.AddMinutes(-1),
                2,
                new CustomerRequest("Test Customer", "test@example.com", null),
                null)));

        Assert.Equal("Reservering moet in de toekomst liggen.", exception.Message);
        Assert.Empty(reservationRepository.Reservations);
    }

    [Fact]
    public async Task CreateReservation_Should_Throw_WhenReservationDateTimeIsNow()
    {
        var reservationRepository = new FakeReservationRepository();
        var restaurantRepository = await CreateRestaurantRepositoryAsync(DayOfWeek.Thursday);
        var restaurant = restaurantRepository.Restaurants.Single();
        var useCase = CreateUseCase(restaurantRepository, reservationRepository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(new CreateReservationRequest(
                restaurant.Id,
                FixedNow,
                2,
                new CustomerRequest("Test Customer", "test@example.com", null),
                null)));

        Assert.Equal("Reservering moet in de toekomst liggen.", exception.Message);
        Assert.Empty(reservationRepository.Reservations);
    }

    [Fact]
    public async Task CreateReservation_Should_Create_WhenReservationDateTimeIsInFuture()
    {
        var reservationRepository = new FakeReservationRepository();
        var restaurantRepository = await CreateRestaurantRepositoryAsync(DayOfWeek.Thursday);
        var restaurant = restaurantRepository.Restaurants.Single();
        var useCase = CreateUseCase(restaurantRepository, reservationRepository);

        var reservation = await useCase.ExecuteAsync(new CreateReservationRequest(
            restaurant.Id,
            FixedNow.AddHours(7),
            2,
            new CustomerRequest("Test Customer", "test@example.com", null),
            null));

        Assert.Equal(restaurant.Id, reservation.RestaurantId);
        Assert.Single(reservationRepository.Reservations);
    }

    [Fact]
    public async Task CreateReservation_Should_SendCustomerConfirmation_AndLogIt()
    {
        var reservationRepository = new FakeReservationRepository();
        var restaurantRepository = await CreateRestaurantRepositoryAsync(DayOfWeek.Thursday);
        var restaurant = restaurantRepository.Restaurants.Single();
        var emailSender = new FakeEmailSender();
        var notificationLogRepository = new FakeNotificationLogRepository();
        var useCase = CreateUseCase(
            restaurantRepository,
            reservationRepository,
            emailSender,
            notificationLogRepository);

        await useCase.ExecuteAsync(new CreateReservationRequest(
            restaurant.Id,
            FixedNow.AddHours(7),
            2,
            new CustomerRequest("Test Customer", "test@example.com", null),
            null));

        // Restaurant has no e-mail, so only the customer confirmation is sent.
        var sent = Assert.Single(emailSender.SentMessages);
        Assert.Equal("test@example.com", sent.To);

        var log = Assert.Single(notificationLogRepository.Logs);
        Assert.Equal("test@example.com", log.RecipientEmail);
        Assert.NotNull(log.SentAtUtc);
    }

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
            new AvailabilityService(restaurantRepository),
            new FakeEmailSender(),
            new FakeNotificationLogRepository(),
            new FixedTimeProvider(FixedNow));

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

    private static async Task<FakeRestaurantRepository> CreateRestaurantRepositoryAsync(DayOfWeek openDay)
    {
        var restaurantRepository = new FakeRestaurantRepository();
        var restaurant = new Restaurant("Sultana BBQ");

        await restaurantRepository.AddAsync(restaurant);
        await restaurantRepository.SetOpeningHoursAsync(
            restaurant.Id,
            [
                new OpeningHour(
                    openDay,
                    new TimeOnly(00, 00),
                    new TimeOnly(23, 59))
            ]);

        return restaurantRepository;
    }

    private static CreateReservationUseCase CreateUseCase(
        FakeRestaurantRepository restaurantRepository,
        FakeReservationRepository reservationRepository,
        FakeEmailSender? emailSender = null,
        FakeNotificationLogRepository? notificationLogRepository = null)
    {
        return new CreateReservationUseCase(
            restaurantRepository,
            reservationRepository,
            new AvailabilityService(restaurantRepository),
            emailSender ?? new FakeEmailSender(),
            notificationLogRepository ?? new FakeNotificationLogRepository(),
            new FixedTimeProvider(FixedNow));
    }

    private sealed class FixedTimeProvider(DateTime localNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(localNow, TimeZoneInfo.Local.GetUtcOffset(localNow)).ToUniversalTime();
        }
    }
}
