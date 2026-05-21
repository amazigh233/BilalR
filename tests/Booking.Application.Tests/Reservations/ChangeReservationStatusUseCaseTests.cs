using Booking.Application.Reservations;
using Booking.Application.Tests.Fakes;
using Booking.Domain.Customers;
using Booking.Domain.Reservations;

namespace Booking.Application.Tests.Reservations;

public sealed class ChangeReservationStatusUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ConfirmsReservation()
    {
        var repository = new FakeReservationRepository();
        var reservation = new Reservation(
            Guid.NewGuid(),
            new Customer("Test Customer", "test@example.com"),
            new DateTime(2026, 06, 01, 19, 00, 00),
            2);

        await repository.AddAsync(reservation);

        var useCase = new ChangeReservationStatusUseCase(repository);

        var response = await useCase.ExecuteAsync(new ChangeReservationStatusRequest(
            reservation.Id,
            ReservationStatus.Confirmed));

        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        Assert.Equal(ReservationStatus.Confirmed, response.Status);
    }
}
