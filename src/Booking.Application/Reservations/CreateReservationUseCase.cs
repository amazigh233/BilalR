using Booking.Application.Abstractions;
using Booking.Domain.Customers;
using Booking.Domain.Reservations;

namespace Booking.Application.Reservations;

public sealed class CreateReservationUseCase(
    IRestaurantRepository restaurantRepository,
    IReservationRepository reservationRepository,
    IAvailabilityService availabilityService)
{
    public async Task<ReservationResponse> ExecuteAsync(
        CreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        var restaurant = await restaurantRepository.GetByIdAsync(request.RestaurantId, cancellationToken);
        if (restaurant is null)
        {
            throw new KeyNotFoundException("Restaurant was not found.");
        }

        var availability = await availabilityService.CheckAsync(
            request.RestaurantId,
            request.ReservationDateTime,
            request.PartySize,
            cancellationToken);

        if (!availability.IsAvailable)
        {
            throw new InvalidOperationException(availability.Reason ?? "Reservation is not available.");
        }

        var customer = new Customer(
            request.Customer.Name,
            request.Customer.Email,
            request.Customer.PhoneNumber);

        var reservation = new Reservation(
            restaurant.Id,
            customer,
            request.ReservationDateTime,
            request.PartySize);

        await reservationRepository.AddAsync(reservation, cancellationToken);

        return ReservationResponse.FromReservation(reservation);
    }
}
