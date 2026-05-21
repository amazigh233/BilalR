using Booking.Domain.Customers;

namespace Booking.Domain.Reservations;

public sealed class Reservation
{
    private Reservation()
    {
        Customer = null!;
    }

    public Reservation(
        Guid restaurantId,
        Customer customer,
        DateTime reservationDateTime,
        int partySize)
    {
        if (restaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(restaurantId));
        }

        ArgumentNullException.ThrowIfNull(customer);

        if (reservationDateTime == default)
        {
            throw new ArgumentException("Reservation date/time is required.", nameof(reservationDateTime));
        }

        if (partySize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(partySize), "Party size must be greater than 0.");
        }

        Id = Guid.NewGuid();
        RestaurantId = restaurantId;
        CustomerId = customer.Id;
        Customer = customer;
        ReservationDateTime = reservationDateTime;
        PartySize = partySize;
        Status = ReservationStatus.New;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid RestaurantId { get; private set; }

    public Guid CustomerId { get; private set; }

    public Customer Customer { get; private set; }

    public DateTime ReservationDateTime { get; private set; }

    public int PartySize { get; private set; }

    public ReservationStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public void Confirm()
    {
        Status = ReservationStatus.Confirmed;
    }

    public void Cancel()
    {
        Status = ReservationStatus.Cancelled;
    }

    public void MarkAsNoShow()
    {
        Status = ReservationStatus.NoShow;
    }
}
