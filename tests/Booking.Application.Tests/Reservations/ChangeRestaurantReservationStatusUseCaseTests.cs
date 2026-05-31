using Booking.Application.Reservations;
using Booking.Application.Tests.Fakes;
using Booking.Domain.Customers;
using Booking.Domain.Reservations;

namespace Booking.Application.Tests.Reservations;

public sealed class ChangeRestaurantReservationStatusUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ChangesStatusWhenReservationBelongsToRestaurant()
    {
        var repository = new FakeReservationRepository();
        var restaurantId = Guid.NewGuid();
        var reservation = new Reservation(
            restaurantId,
            new Customer("Test Customer", "test@example.com"),
            new DateTime(2026, 06, 01, 19, 00, 00),
            2);

        await repository.AddAsync(reservation);

        var useCase = new ChangeRestaurantReservationStatusUseCase(repository);

        var response = await useCase.ExecuteAsync(new ChangeRestaurantReservationStatusRequest(
            restaurantId,
            reservation.Id,
            ReservationStatus.Confirmed));

        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        Assert.Equal(ReservationStatus.Confirmed, response.Status);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsWhenReservationBelongsToAnotherRestaurant()
    {
        var repository = new FakeReservationRepository();
        var reservation = new Reservation(
            Guid.NewGuid(),
            new Customer("Test Customer", "test@example.com"),
            new DateTime(2026, 06, 01, 19, 00, 00),
            2);

        await repository.AddAsync(reservation);

        var useCase = new ChangeRestaurantReservationStatusUseCase(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => useCase.ExecuteAsync(
            new ChangeRestaurantReservationStatusRequest(
                Guid.NewGuid(),
                reservation.Id,
                ReservationStatus.Confirmed)));
    }
}
