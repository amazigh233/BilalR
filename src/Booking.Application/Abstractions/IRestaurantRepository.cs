using Booking.Domain.Restaurants;

namespace Booking.Application.Abstractions;

public interface IRestaurantRepository
{
    Task AddAsync(Restaurant restaurant, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Restaurant>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Restaurant?> GetByIdAsync(Guid restaurantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<OpeningHour>> GetOpeningHoursAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default);

    Task SetOpeningHoursAsync(
        Guid restaurantId,
        IReadOnlyCollection<OpeningHour> openingHours,
        CancellationToken cancellationToken = default);
}
