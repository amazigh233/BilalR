using Booking.Domain.Reservations;

namespace Booking.Application.Reservations;

public sealed record ReservationResponse(
    Guid Id,
    Guid RestaurantId,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhoneNumber,
    DateTime ReservationDateTime,
    int PartySize,
    string? Note,
    ReservationStatus Status,
    DateTime CreatedAtUtc)
{
    public static ReservationResponse FromReservation(Reservation reservation)
    {
        return new ReservationResponse(
            reservation.Id,
            reservation.RestaurantId,
            reservation.CustomerId,
            reservation.Customer.Name,
            reservation.Customer.Email,
            reservation.Customer.PhoneNumber,
            reservation.ReservationDateTime,
            reservation.PartySize,
            reservation.Note,
            reservation.Status,
            reservation.CreatedAtUtc);
    }
}
