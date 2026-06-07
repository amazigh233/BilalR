using Booking.Application.Abstractions;
using Booking.Domain.Scheduling;

namespace Booking.Application.Tests.Fakes;

public sealed class FakeShiftRepository : IShiftRepository
{
    public List<Shift> Shifts { get; } = [];

    public Task AddAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        Shifts.Add(shift);
        return Task.CompletedTask;
    }

    public Task<Shift?> GetByIdAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Shifts.FirstOrDefault(shift => shift.Id == shiftId));
    }

    public Task<IReadOnlyCollection<Shift>> GetByRestaurantAndDateRangeAsync(
        Guid restaurantId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var shifts = Shifts
            .Where(shift => shift.RestaurantId == restaurantId
                && shift.ShiftDate >= fromDate
                && shift.ShiftDate <= toDate)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<Shift>>(shifts);
    }

    public Task<IReadOnlyCollection<Shift>> GetByStaffAndDateRangeAsync(
        Guid restaurantId,
        Guid staffUserId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var shifts = Shifts
            .Where(shift => shift.RestaurantId == restaurantId
                && shift.StaffUserId == staffUserId
                && shift.ShiftDate >= fromDate
                && shift.ShiftDate <= toDate)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<Shift>>(shifts);
    }

    public Task UpdateAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        Shifts.Remove(shift);
        return Task.CompletedTask;
    }
}
