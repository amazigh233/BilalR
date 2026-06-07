using Booking.Domain.Scheduling;

namespace Booking.Api.Contracts.Scheduling;

public sealed record LeaveRequestApiResponse(
    Guid Id,
    Guid StaffUserId,
    DateOnly FromDate,
    DateOnly ToDate,
    string? Reason,
    LeaveStatus Status,
    DateTime CreatedAtUtc,
    DateTime? DecidedAtUtc);

public sealed record CreateLeaveApiRequest(
    DateOnly FromDate,
    DateOnly ToDate,
    string? Reason);
