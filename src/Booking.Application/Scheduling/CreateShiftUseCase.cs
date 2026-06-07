using Booking.Application.Abstractions;
using Booking.Domain.Scheduling;

namespace Booking.Application.Scheduling;

public sealed record CreateShiftRequest(
    Guid RestaurantId,
    Guid StaffUserId,
    DateOnly ShiftDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Note);

public sealed class CreateShiftUseCase(IShiftRepository shiftRepository)
{
    public async Task<ShiftResponse> ExecuteAsync(
        CreateShiftRequest request,
        CancellationToken cancellationToken = default)
    {
        var shift = new Shift(
            request.RestaurantId,
            request.StaffUserId,
            request.ShiftDate,
            request.StartTime,
            request.EndTime,
            request.Note);

        await shiftRepository.AddAsync(shift, cancellationToken);

        return ShiftResponse.FromShift(shift);
    }
}
