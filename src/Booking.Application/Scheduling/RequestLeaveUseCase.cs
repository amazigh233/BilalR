using Booking.Application.Abstractions;
using Booking.Domain.Scheduling;

namespace Booking.Application.Scheduling;

public sealed record RequestLeaveRequest(
    Guid RestaurantId,
    Guid StaffUserId,
    DateOnly FromDate,
    DateOnly ToDate,
    string? Reason);

public sealed class RequestLeaveUseCase(
    ILeaveRequestRepository leaveRequestRepository,
    TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<LeaveRequestResponse> ExecuteAsync(
        RequestLeaveRequest request,
        CancellationToken cancellationToken = default)
    {
        var leaveRequest = new LeaveRequest(
            request.RestaurantId,
            request.StaffUserId,
            request.FromDate,
            request.ToDate,
            request.Reason,
            _timeProvider.GetUtcNow().UtcDateTime);

        await leaveRequestRepository.AddAsync(leaveRequest, cancellationToken);

        return LeaveRequestResponse.FromLeaveRequest(leaveRequest);
    }
}
