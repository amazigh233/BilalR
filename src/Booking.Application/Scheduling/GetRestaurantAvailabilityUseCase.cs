using Booking.Application.Abstractions;

namespace Booking.Application.Scheduling;

public sealed record GetRestaurantAvailabilityRequest(Guid RestaurantId);

public sealed class GetRestaurantAvailabilityUseCase(IStaffAvailabilityRepository availabilityRepository)
{
    public async Task<IReadOnlyCollection<AvailabilitySlotResponse>> ExecuteAsync(
        GetRestaurantAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var availabilities = await availabilityRepository.GetByRestaurantAsync(
            request.RestaurantId,
            cancellationToken);

        return availabilities.Select(AvailabilitySlotResponse.FromAvailability).ToList();
    }
}
