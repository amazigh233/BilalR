using Booking.Domain.Scheduling;

namespace Booking.Application.Scheduling;

public sealed record LeaveRequestResponse(
    Guid Id,
    Guid StaffUserId,
    DateOnly FromDate,
    DateOnly ToDate,
    string? Reason,
    LeaveStatus Status,
    DateTime CreatedAtUtc,
    DateTime? DecidedAtUtc)
{
    public static LeaveRequestResponse FromLeaveRequest(LeaveRequest leaveRequest)
    {
        return new LeaveRequestResponse(
            leaveRequest.Id,
            leaveRequest.StaffUserId,
            leaveRequest.FromDate,
            leaveRequest.ToDate,
            leaveRequest.Reason,
            leaveRequest.Status,
            leaveRequest.CreatedAtUtc,
            leaveRequest.DecidedAtUtc);
    }
}
