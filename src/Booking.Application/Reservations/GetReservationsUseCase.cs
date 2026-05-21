using Booking.Application.Abstractions;

namespace Booking.Application.Reservations;

public sealed class GetReservationsUseCase(
    IRestaurantRepository restaurantRepository,
    IReservationRepository reservationRepository)
{
    public async Task<IReadOnlyCollection<ReservationResponse>> ExecuteAsync(
        GetReservationsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.RestaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(request));
        }

        var restaurant = await restaurantRepository.GetByIdAsync(request.RestaurantId, cancellationToken);
        if (restaurant is null)
        {
            throw new KeyNotFoundException("Restaurant was not found.");
        }

        var reservations = await reservationRepository.GetByRestaurantIdAsync(
            request.RestaurantId,
            cancellationToken);

        return reservations
            .OrderBy(reservation => reservation.ReservationDateTime)
            .Select(ReservationResponse.FromReservation)
            .ToList();
    }
}
