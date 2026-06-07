using Booking.Application.Scheduling;
using Booking.Application.Tests.Fakes;

namespace Booking.Application.Tests.Scheduling;

public sealed class AvailabilityUseCasesTests
{
    private static readonly Guid RestaurantId = Guid.NewGuid();
    private static readonly Guid StaffUserId = Guid.NewGuid();

    [Fact]
    public async Task SetAvailability_ReplacesExistingSlotsForStaff()
    {
        var repository = new FakeStaffAvailabilityRepository();
        var useCase = new SetStaffAvailabilityUseCase(repository);

        await useCase.ExecuteAsync(new SetStaffAvailabilityRequest(
            RestaurantId,
            StaffUserId,
            [new AvailabilitySlotInput(DayOfWeek.Monday, new TimeOnly(09, 00), new TimeOnly(17, 00))]));

        await useCase.ExecuteAsync(new SetStaffAvailabilityRequest(
            RestaurantId,
            StaffUserId,
            [new AvailabilitySlotInput(DayOfWeek.Tuesday, new TimeOnly(10, 00), new TimeOnly(14, 00))]));

        Assert.Single(repository.Availabilities);
        Assert.Equal(DayOfWeek.Tuesday, repository.Availabilities[0].DayOfWeek);
    }

    [Fact]
    public async Task SetAvailability_DoesNotTouchOtherStaff()
    {
        var repository = new FakeStaffAvailabilityRepository();
        var otherStaff = Guid.NewGuid();
        var useCase = new SetStaffAvailabilityUseCase(repository);

        await useCase.ExecuteAsync(new SetStaffAvailabilityRequest(
            RestaurantId,
            otherStaff,
            [new AvailabilitySlotInput(DayOfWeek.Friday, new TimeOnly(18, 00), new TimeOnly(23, 00))]));

        await useCase.ExecuteAsync(new SetStaffAvailabilityRequest(
            RestaurantId,
            StaffUserId,
            [new AvailabilitySlotInput(DayOfWeek.Monday, new TimeOnly(09, 00), new TimeOnly(17, 00))]));

        Assert.Equal(2, repository.Availabilities.Count);
    }

    [Fact]
    public async Task GetStaffAvailability_ReturnsOnlyOwnSlots()
    {
        var repository = new FakeStaffAvailabilityRepository();
        var otherStaff = Guid.NewGuid();
        await new SetStaffAvailabilityUseCase(repository).ExecuteAsync(new SetStaffAvailabilityRequest(
            RestaurantId, StaffUserId,
            [new AvailabilitySlotInput(DayOfWeek.Monday, new TimeOnly(09, 00), new TimeOnly(17, 00))]));
        await new SetStaffAvailabilityUseCase(repository).ExecuteAsync(new SetStaffAvailabilityRequest(
            RestaurantId, otherStaff,
            [new AvailabilitySlotInput(DayOfWeek.Tuesday, new TimeOnly(09, 00), new TimeOnly(17, 00))]));

        var result = await new GetStaffAvailabilityUseCase(repository).ExecuteAsync(
            new GetStaffAvailabilityRequest(RestaurantId, StaffUserId));

        Assert.Single(result);
        Assert.All(result, slot => Assert.Equal(StaffUserId, slot.StaffUserId));
    }

    [Fact]
    public async Task GetRestaurantAvailability_ReturnsAllStaffSlots()
    {
        var repository = new FakeStaffAvailabilityRepository();
        await new SetStaffAvailabilityUseCase(repository).ExecuteAsync(new SetStaffAvailabilityRequest(
            RestaurantId, StaffUserId,
            [new AvailabilitySlotInput(DayOfWeek.Monday, new TimeOnly(09, 00), new TimeOnly(17, 00))]));
        await new SetStaffAvailabilityUseCase(repository).ExecuteAsync(new SetStaffAvailabilityRequest(
            RestaurantId, Guid.NewGuid(),
            [new AvailabilitySlotInput(DayOfWeek.Tuesday, new TimeOnly(09, 00), new TimeOnly(17, 00))]));

        var result = await new GetRestaurantAvailabilityUseCase(repository).ExecuteAsync(
            new GetRestaurantAvailabilityRequest(RestaurantId));

        Assert.Equal(2, result.Count);
    }
}
