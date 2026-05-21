namespace Booking.Domain.Restaurants;

public sealed class OpeningHour
{
    public OpeningHour(DayOfWeek dayOfWeek, TimeOnly opensAt, TimeOnly closesAt)
    {
        if (closesAt <= opensAt)
        {
            throw new ArgumentException("Closing time must be later than opening time.", nameof(closesAt));
        }

        Id = Guid.NewGuid();
        DayOfWeek = dayOfWeek;
        OpensAt = opensAt;
        ClosesAt = closesAt;
    }

    public Guid Id { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }

    public TimeOnly OpensAt { get; private set; }

    public TimeOnly ClosesAt { get; private set; }
}
