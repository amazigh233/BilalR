using Booking.Application.Abstractions;

namespace Booking.Application.Scheduling;

public sealed record GetStaffLeaveRequest(Guid RestaurantId, Guid StaffUserId);

public sealed class GetStaffLeaveUseCase(ILeaveRequestRepository leaveRequestRepository)
{
    public async Task<IReadOnlyCollection<LeaveRequestResponse>> ExecuteAsync(
        GetStaffLeaveRequest request,
        CancellationToken cancellationToken = default)
    {
        var leave = await leaveRequestRepository.GetByStaffAsync(
            request.RestaurantId,
            request.StaffUserId,
            cancellationToken);

        return leave.Select(LeaveRequestResponse.FromLeaveRequest).ToList();
    }
}

public sealed record GetRestaurantLeaveRequest(Guid RestaurantId);

public sealed class GetRestaurantLeaveUseCase(ILeaveRequestRepository leaveRequestRepository)
{
    public async Task<IReadOnlyCollection<LeaveRequestResponse>> ExecuteAsync(
        GetRestaurantLeaveRequest request,
        CancellationToken cancellationToken = default)
    {
        var leave = await leaveRequestRepository.GetByRestaurantAsync(
            request.RestaurantId,
            cancellationToken);

        return leave.Select(LeaveRequestResponse.FromLeaveRequest).ToList();
    }
}
