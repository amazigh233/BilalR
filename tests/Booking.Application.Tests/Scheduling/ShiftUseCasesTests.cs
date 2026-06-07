using Booking.Application.Scheduling;
using Booking.Application.Tests.Fakes;
using Booking.Domain.Scheduling;

namespace Booking.Application.Tests.Scheduling;

public sealed class ShiftUseCasesTests
{
    private static readonly Guid RestaurantId = Guid.NewGuid();
    private static readonly Guid StaffUserId = Guid.NewGuid();

    [Fact]
    public void Shift_Throws_WhenEndNotAfterStart()
    {
        Assert.Throws<ArgumentException>(() => new Shift(
            RestaurantId,
            StaffUserId,
            new DateOnly(2026, 06, 01),
            new TimeOnly(18, 00),
            new TimeOnly(18, 00)));
    }

    [Fact]
    public async Task CreateShift_AddsShift()
    {
        var repository = new FakeShiftRepository();
        var useCase = new CreateShiftUseCase(repository);

        var response = await useCase.ExecuteAsync(new CreateShiftRequest(
            RestaurantId,
            StaffUserId,
            new DateOnly(2026, 06, 01),
            new TimeOnly(17, 00),
            new TimeOnly(22, 00),
            "Avonddienst"));

        Assert.Equal(RestaurantId, response.RestaurantId);
        Assert.Equal(StaffUserId, response.StaffUserId);
        Assert.Single(repository.Shifts);
    }

    [Fact]
    public async Task UpdateShift_Throws_WhenShiftBelongsToAnotherRestaurant()
    {
        var repository = new FakeShiftRepository();
        var shift = new Shift(
            Guid.NewGuid(), // other restaurant
            StaffUserId,
            new DateOnly(2026, 06, 01),
            new TimeOnly(17, 00),
            new TimeOnly(22, 00));
        await repository.AddAsync(shift);

        var useCase = new UpdateShiftUseCase(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            useCase.ExecuteAsync(new UpdateShiftRequest(
                RestaurantId, // requesting restaurant differs from the shift's
                shift.Id,
                StaffUserId,
                new DateOnly(2026, 06, 02),
                new TimeOnly(09, 00),
                new TimeOnly(17, 00),
                null)));
    }

    [Fact]
    public async Task GetShifts_ReturnsOnlyShiftsInRange()
    {
        var repository = new FakeShiftRepository();
        await repository.AddAsync(NewShift(new DateOnly(2026, 06, 01)));
        await repository.AddAsync(NewShift(new DateOnly(2026, 06, 05)));
        await repository.AddAsync(NewShift(new DateOnly(2026, 06, 20))); // out of range

        var useCase = new GetShiftsUseCase(repository);

        var result = await useCase.ExecuteAsync(new GetShiftsRequest(
            RestaurantId,
            new DateOnly(2026, 06, 01),
            new DateOnly(2026, 06, 07)));

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetShifts_Throws_WhenToBeforeFrom()
    {
        var useCase = new GetShiftsUseCase(new FakeShiftRepository());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(new GetShiftsRequest(
                RestaurantId,
                new DateOnly(2026, 06, 07),
                new DateOnly(2026, 06, 01))));
    }

    private static Shift NewShift(DateOnly date)
    {
        return new Shift(RestaurantId, StaffUserId, date, new TimeOnly(17, 00), new TimeOnly(22, 00));
    }
}
