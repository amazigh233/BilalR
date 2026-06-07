using Booking.Application.Abstractions;
using Booking.Domain.Scheduling;

namespace Booking.Application.Tests.Fakes;

public sealed class FakeStaffAvailabilityRepository : IStaffAvailabilityRepository
{
    public List<StaffAvailability> Availabilities { get; } = [];

    public Task<IReadOnlyCollection<StaffAvailability>> GetByStaffAsync(
        Guid restaurantId,
        Guid staffUserId,
        CancellationToken cancellationToken = default)
    {
        var result = Availabilities
            .Where(availability => availability.RestaurantId == restaurantId
                && availability.StaffUserId == staffUserId)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<StaffAvailability>>(result);
    }

    public Task<IReadOnlyCollection<StaffAvailability>> GetByRestaurantAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        var result = Availabilities
            .Where(availability => availability.RestaurantId == restaurantId)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<StaffAvailability>>(result);
    }

    public Task ReplaceForStaffAsync(
        Guid restaurantId,
        Guid staffUserId,
        IReadOnlyCollection<StaffAvailability> availabilities,
        CancellationToken cancellationToken = default)
    {
        Availabilities.RemoveAll(availability => availability.RestaurantId == restaurantId
            && availability.StaffUserId == staffUserId);
        Availabilities.AddRange(availabilities);
        return Task.CompletedTask;
    }
}
