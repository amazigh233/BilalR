using Booking.Application.Scheduling;
using Booking.Application.Tests.Fakes;
using Booking.Domain.Scheduling;

namespace Booking.Application.Tests.Scheduling;

public sealed class LeaveUseCasesTests
{
    private static readonly Guid RestaurantId = Guid.NewGuid();
    private static readonly Guid StaffUserId = Guid.NewGuid();

    private static readonly TimeProvider FixedTime =
        new FixedTimeProvider(new DateTimeOffset(2026, 06, 08, 10, 00, 00, TimeSpan.Zero));

    [Fact]
    public async Task RequestLeave_AddsPendingRequest()
    {
        var repository = new FakeLeaveRequestRepository();
        var useCase = new RequestLeaveUseCase(repository, FixedTime);

        var response = await useCase.ExecuteAsync(new RequestLeaveRequest(
            RestaurantId,
            StaffUserId,
            new DateOnly(2026, 07, 01),
            new DateOnly(2026, 07, 05),
            "Vakantie"));

        Assert.Equal(LeaveStatus.Pending, response.Status);
        Assert.Single(repository.LeaveRequests);
    }

    [Fact]
    public async Task DecideLeave_Approve_SetsApprovedAndDecidedAt()
    {
        var repository = new FakeLeaveRequestRepository();
        var leave = new LeaveRequest(RestaurantId, StaffUserId,
            new DateOnly(2026, 07, 01), new DateOnly(2026, 07, 05), null,
            FixedTime.GetUtcNow().UtcDateTime);
        await repository.AddAsync(leave);

        var response = await new DecideLeaveUseCase(repository, FixedTime).ExecuteAsync(
            new DecideLeaveRequest(RestaurantId, leave.Id, Approve: true));

        Assert.Equal(LeaveStatus.Approved, response.Status);
        Assert.NotNull(response.DecidedAtUtc);
    }

    [Fact]
    public async Task DecideLeave_Deny_SetsDenied()
    {
        var repository = new FakeLeaveRequestRepository();
        var leave = new LeaveRequest(RestaurantId, StaffUserId,
            new DateOnly(2026, 07, 01), new DateOnly(2026, 07, 05), null,
            FixedTime.GetUtcNow().UtcDateTime);
        await repository.AddAsync(leave);

        var response = await new DecideLeaveUseCase(repository, FixedTime).ExecuteAsync(
            new DecideLeaveRequest(RestaurantId, leave.Id, Approve: false));

        Assert.Equal(LeaveStatus.Denied, response.Status);
    }

    [Fact]
    public async Task DecideLeave_Throws_WhenLeaveBelongsToAnotherRestaurant()
    {
        var repository = new FakeLeaveRequestRepository();
        var leave = new LeaveRequest(Guid.NewGuid(), StaffUserId,
            new DateOnly(2026, 07, 01), new DateOnly(2026, 07, 05), null,
            FixedTime.GetUtcNow().UtcDateTime);
        await repository.AddAsync(leave);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            new DecideLeaveUseCase(repository, FixedTime).ExecuteAsync(
                new DecideLeaveRequest(RestaurantId, leave.Id, Approve: true)));
    }

    [Fact]
    public async Task DecideLeave_Throws_WhenAlreadyDecided()
    {
        var repository = new FakeLeaveRequestRepository();
        var leave = new LeaveRequest(RestaurantId, StaffUserId,
            new DateOnly(2026, 07, 01), new DateOnly(2026, 07, 05), null,
            FixedTime.GetUtcNow().UtcDateTime);
        await repository.AddAsync(leave);
        var useCase = new DecideLeaveUseCase(repository, FixedTime);
        await useCase.ExecuteAsync(new DecideLeaveRequest(RestaurantId, leave.Id, Approve: true));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(new DecideLeaveRequest(RestaurantId, leave.Id, Approve: false)));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
