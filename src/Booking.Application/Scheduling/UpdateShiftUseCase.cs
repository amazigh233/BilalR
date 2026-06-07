using Booking.Application.Abstractions;

namespace Booking.Application.Scheduling;

public sealed record UpdateShiftRequest(
    Guid RestaurantId,
    Guid ShiftId,
    Guid StaffUserId,
    DateOnly ShiftDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Note);

public sealed class UpdateShiftUseCase(IShiftRepository shiftRepository)
{
    public async Task<ShiftResponse> ExecuteAsync(
        UpdateShiftRequest request,
        CancellationToken cancellationToken = default)
    {
        var shift = await shiftRepository.GetByIdAsync(request.ShiftId, cancellationToken);
        if (shift is null || shift.RestaurantId != request.RestaurantId)
        {
            throw new KeyNotFoundException("Shift was not found.");
        }

        shift.UpdateDetails(
            request.StaffUserId,
            request.ShiftDate,
            request.StartTime,
            request.EndTime,
            request.Note);

        await shiftRepository.UpdateAsync(shift, cancellationToken);

        return ShiftResponse.FromShift(shift);
    }
}
