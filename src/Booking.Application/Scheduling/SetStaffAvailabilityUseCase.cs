using Booking.Application.Abstractions;
using Booking.Domain.Scheduling;

namespace Booking.Application.Scheduling;

public sealed record SetStaffAvailabilityRequest(
    Guid RestaurantId,
    Guid StaffUserId,
    IReadOnlyCollection<AvailabilitySlotInput> Slots);

public sealed class SetStaffAvailabilityUseCase(IStaffAvailabilityRepository availabilityRepository)
{
    public async Task<IReadOnlyCollection<AvailabilitySlotResponse>> ExecuteAsync(
        SetStaffAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var availabilities = request.Slots
            .Select(slot => new StaffAvailability(
                request.RestaurantId,
                request.StaffUserId,
                slot.DayOfWeek,
                slot.StartTime,
                slot.EndTime))
            .ToList();

        await availabilityRepository.ReplaceForStaffAsync(
            request.RestaurantId,
            request.StaffUserId,
            availabilities,
            cancellationToken);

        return availabilities.Select(AvailabilitySlotResponse.FromAvailability).ToList();
    }
}
