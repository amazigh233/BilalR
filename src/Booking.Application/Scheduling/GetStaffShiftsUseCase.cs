using Booking.Application.Abstractions;

namespace Booking.Application.Scheduling;

public sealed record GetStaffShiftsRequest(
    Guid RestaurantId,
    Guid StaffUserId,
    DateOnly FromDate,
    DateOnly ToDate);

public sealed class GetStaffShiftsUseCase(IShiftRepository shiftRepository)
{
    public async Task<IReadOnlyCollection<ShiftResponse>> ExecuteAsync(
        GetStaffShiftsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ToDate < request.FromDate)
        {
            throw new ArgumentException("To date cannot be before from date.", nameof(request));
        }

        var shifts = await shiftRepository.GetByStaffAndDateRangeAsync(
            request.RestaurantId,
            request.StaffUserId,
            request.FromDate,
            request.ToDate,
            cancellationToken);

        return shifts.Select(ShiftResponse.FromShift).ToList();
    }
}
