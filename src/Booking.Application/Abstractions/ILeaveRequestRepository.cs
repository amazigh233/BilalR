using Booking.Domain.Scheduling;

namespace Booking.Application.Abstractions;

public interface ILeaveRequestRepository
{
    Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default);

    Task<LeaveRequest?> GetByIdAsync(Guid leaveRequestId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<LeaveRequest>> GetByStaffAsync(
        Guid restaurantId,
        Guid staffUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<LeaveRequest>> GetByRestaurantAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default);
}
