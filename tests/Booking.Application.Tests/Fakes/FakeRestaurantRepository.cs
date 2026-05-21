using Booking.Application.Abstractions;
using Booking.Domain.Restaurants;

namespace Booking.Application.Tests.Fakes;

public sealed class FakeRestaurantRepository : IRestaurantRepository
{
    private readonly Dictionary<Guid, List<OpeningHour>> _openingHours = [];

    public List<Restaurant> Restaurants { get; } = [];

    public Task AddAsync(Restaurant restaurant, CancellationToken cancellationToken = default)
    {
        Restaurants.Add(restaurant);
        return Task.CompletedTask;
    }

    public Task<Restaurant?> GetByIdAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Restaurants.FirstOrDefault(restaurant => restaurant.Id == restaurantId));
    }

    public Task<IReadOnlyCollection<OpeningHour>> GetOpeningHoursAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        var openingHours = _openingHours.TryGetValue(restaurantId, out var values)
            ? values
            : [];

        return Task.FromResult<IReadOnlyCollection<OpeningHour>>(openingHours);
    }

    public Task SetOpeningHoursAsync(
        Guid restaurantId,
        IReadOnlyCollection<OpeningHour> openingHours,
        CancellationToken cancellationToken = default)
    {
        _openingHours[restaurantId] = openingHours.ToList();
        return Task.CompletedTask;
    }
}
