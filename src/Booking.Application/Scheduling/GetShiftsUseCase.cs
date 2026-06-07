using Booking.Application.Abstractions;

namespace Booking.Application.Scheduling;

public sealed record GetShiftsRequest(Guid RestaurantId, DateOnly FromDate, DateOnly ToDate);

public sealed class GetShiftsUseCase(IShiftRepository shiftRepository)
{
    public async Task<IReadOnlyCollection<ShiftResponse>> ExecuteAsync(
        GetShiftsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ToDate < request.FromDate)
        {
            throw new ArgumentException("To date cannot be before from date.", nameof(request));
        }

        var shifts = await shiftRepository.GetByRestaurantAndDateRangeAsync(
            request.RestaurantId,
            request.FromDate,
            request.ToDate,
            cancellationToken);

        return shifts.Select(ShiftResponse.FromShift).ToList();
    }
}
