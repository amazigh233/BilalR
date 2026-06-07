namespace Booking.Domain.Scheduling;

public sealed class LeaveRequest
{
    private LeaveRequest()
    {
    }

    public LeaveRequest(
        Guid restaurantId,
        Guid staffUserId,
        DateOnly fromDate,
        DateOnly toDate,
        string? reason,
        DateTime createdAtUtc)
    {
        if (restaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(restaurantId));
        }

        if (staffUserId == Guid.Empty)
        {
            throw new ArgumentException("Staff user id is required.", nameof(staffUserId));
        }

        if (toDate < fromDate)
        {
            throw new ArgumentException("To date cannot be before from date.", nameof(toDate));
        }

        if (reason?.Length > 500)
        {
            throw new ArgumentException("Reason cannot exceed 500 characters.", nameof(reason));
        }

        Id = Guid.NewGuid();
        RestaurantId = restaurantId;
        StaffUserId = staffUserId;
        FromDate = fromDate;
        ToDate = toDate;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Status = LeaveStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid RestaurantId { get; private set; }

    public Guid StaffUserId { get; private set; }

    public DateOnly FromDate { get; private set; }

    public DateOnly ToDate { get; private set; }

    public string? Reason { get; private set; }

    public LeaveStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? DecidedAtUtc { get; private set; }

    public void Approve(DateTime decidedAtUtc)
    {
        EnsurePending();
        Status = LeaveStatus.Approved;
        DecidedAtUtc = decidedAtUtc;
    }

    public void Deny(DateTime decidedAtUtc)
    {
        EnsurePending();
        Status = LeaveStatus.Denied;
        DecidedAtUtc = decidedAtUtc;
    }

    private void EnsurePending()
    {
        if (Status != LeaveStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending leave request can be decided.");
        }
    }
}
