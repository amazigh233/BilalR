using Booking.Application.Abstractions;

namespace Booking.Application.Scheduling;

public sealed record DecideLeaveRequest(Guid RestaurantId, Guid LeaveRequestId, bool Approve);

public sealed class DecideLeaveUseCase(
    ILeaveRequestRepository leaveRequestRepository,
    TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<LeaveRequestResponse> ExecuteAsync(
        DecideLeaveRequest request,
        CancellationToken cancellationToken = default)
    {
        var leaveRequest = await leaveRequestRepository.GetByIdAsync(request.LeaveRequestId, cancellationToken);
        if (leaveRequest is null || leaveRequest.RestaurantId != request.RestaurantId)
        {
            throw new KeyNotFoundException("Leave request was not found.");
        }

        var decidedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        if (request.Approve)
        {
            leaveRequest.Approve(decidedAtUtc);
        }
        else
        {
            leaveRequest.Deny(decidedAtUtc);
        }

        await leaveRequestRepository.UpdateAsync(leaveRequest, cancellationToken);

        return LeaveRequestResponse.FromLeaveRequest(leaveRequest);
    }
}
