namespace Booking.Domain.Scheduling;

public sealed class StaffAvailability
{
    private StaffAvailability()
    {
    }

    public StaffAvailability(
        Guid restaurantId,
        Guid staffUserId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime)
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

        Id = Guid.NewGuid();
        RestaurantId = restaurantId;
        StaffUserId = staffUserId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
    }

    public Guid Id { get; private set; }

    public Guid RestaurantId { get; private set; }

    public Guid StaffUserId { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }
}
