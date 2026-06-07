using Booking.Application.Abstractions;

namespace Booking.Application.Scheduling;

public sealed record DeleteShiftRequest(Guid RestaurantId, Guid ShiftId);

public sealed class DeleteShiftUseCase(IShiftRepository shiftRepository)
{
    public async Task ExecuteAsync(
        DeleteShiftRequest request,
        CancellationToken cancellationToken = default)
    {
        var shift = await shiftRepository.GetByIdAsync(request.ShiftId, cancellationToken);
        if (shift is null || shift.RestaurantId != request.RestaurantId)
        {
            throw new KeyNotFoundException("Shift was not found.");
        }

        await shiftRepository.DeleteAsync(shift, cancellationToken);
    }
}
