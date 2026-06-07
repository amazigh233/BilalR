using Booking.Application.Abstractions;

namespace Booking.Application.Scheduling;

public sealed record GetStaffAvailabilityRequest(Guid RestaurantId, Guid StaffUserId);

public sealed class GetStaffAvailabilityUseCase(IStaffAvailabilityRepository availabilityRepository)
{
    public async Task<IReadOnlyCollection<AvailabilitySlotResponse>> ExecuteAsync(
        GetStaffAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var availabilities = await availabilityRepository.GetByStaffAsync(
            request.RestaurantId,
            request.StaffUserId,
            cancellationToken);

        return availabilities.Select(AvailabilitySlotResponse.FromAvailability).ToList();
    }
}
