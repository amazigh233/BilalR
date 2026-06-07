namespace Booking.Domain.Scheduling;

public sealed class Shift
{
    private Shift()
    {
    }

    public Shift(
        Guid restaurantId,
        Guid staffUserId,
        DateOnly shiftDate,
        TimeOnly startTime,
        TimeOnly endTime,
        string? note = null)
    {
        if (restaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(restaurantId));
        }

        if (staffUserId == Guid.Empty)
        {
            throw new ArgumentException("Staff user id is required.", nameof(staffUserId));
        }

        if (endTime <= startTime)
        {
            throw new ArgumentException("End time must be after start time.", nameof(endTime));
        }

        if (note?.Length > 500)
        {
            throw new ArgumentException("Shift note cannot exceed 500 characters.", nameof(note));
        }

        Id = Guid.NewGuid();
        RestaurantId = restaurantId;
        StaffUserId = staffUserId;
        ShiftDate = shiftDate;
        StartTime = startTime;
        EndTime = endTime;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    public Guid Id { get; private set; }

    public Guid RestaurantId { get; private set; }

    public Guid StaffUserId { get; private set; }

    public DateOnly ShiftDate { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    public string? Note { get; private set; }

    public void UpdateDetails(
        Guid staffUserId,
        DateOnly shiftDate,
        TimeOnly startTime,
        TimeOnly endTime,
        string? note)
    {
        if (staffUserId == Guid.Empty)
        {
            throw new ArgumentException("Staff user id is required.", nameof(staffUserId));
        }

        if (endTime <= startTime)
        {
            throw new ArgumentException("End time must be after start time.", nameof(endTime));
        }

        if (note?.Length > 500)
        {
            throw new ArgumentException("Shift note cannot exceed 500 characters.", nameof(note));
        }

        StaffUserId = staffUserId;
        ShiftDate = shiftDate;
        StartTime = startTime;
        EndTime = endTime;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }
}
