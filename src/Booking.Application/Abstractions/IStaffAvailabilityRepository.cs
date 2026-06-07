using Booking.Domain.Scheduling;

namespace Booking.Application.Abstractions;

public interface IStaffAvailabilityRepository
{
    Task<IReadOnlyCollection<StaffAvailability>> GetByStaffAsync(
        Guid restaurantId,
        Guid staffUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StaffAvailability>> GetByRestaurantAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default);

    Task ReplaceForStaffAsync(
        Guid restaurantId,
        Guid staffUserId,
        IReadOnlyCollection<StaffAvailability> availabilities,
        CancellationToken cancellationToken = default);
}
