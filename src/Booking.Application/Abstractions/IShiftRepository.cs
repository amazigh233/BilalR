using Booking.Domain.Scheduling;

namespace Booking.Application.Abstractions;

public interface IShiftRepository
{
    Task AddAsync(Shift shift, CancellationToken cancellationToken = default);

    Task<Shift?> GetByIdAsync(Guid shiftId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Shift>> GetByRestaurantAndDateRangeAsync(
        Guid restaurantId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Shift>> GetByStaffAndDateRangeAsync(
        Guid restaurantId,
        Guid staffUserId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Shift shift, CancellationToken cancellationToken = default);

    Task DeleteAsync(Shift shift, CancellationToken cancellationToken = default);
}
