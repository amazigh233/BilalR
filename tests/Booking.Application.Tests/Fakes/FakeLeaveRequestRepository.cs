using Booking.Application.Abstractions;
using Booking.Domain.Scheduling;

namespace Booking.Application.Tests.Fakes;

public sealed class FakeLeaveRequestRepository : ILeaveRequestRepository
{
    public List<LeaveRequest> LeaveRequests { get; } = [];

    public Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default)
    {
        LeaveRequests.Add(leaveRequest);
        return Task.CompletedTask;
    }

    public Task<LeaveRequest?> GetByIdAsync(Guid leaveRequestId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(LeaveRequests.FirstOrDefault(leave => leave.Id == leaveRequestId));
    }

    public Task<IReadOnlyCollection<LeaveRequest>> GetByStaffAsync(
        Guid restaurantId,
        Guid staffUserId,
        CancellationToken cancellationToken = default)
    {
        var result = LeaveRequests
            .Where(leave => leave.RestaurantId == restaurantId && leave.StaffUserId == staffUserId)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<LeaveRequest>>(result);
    }

    public Task<IReadOnlyCollection<LeaveRequest>> GetByRestaurantAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        var result = LeaveRequests
            .Where(leave => leave.RestaurantId == restaurantId)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<LeaveRequest>>(result);
    }

    public Task UpdateAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
