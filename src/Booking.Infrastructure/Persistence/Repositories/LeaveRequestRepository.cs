using Booking.Application.Abstractions;
using Booking.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Persistence.Repositories;

public sealed class LeaveRequestRepository(BookingDbContext dbContext) : ILeaveRequestRepository
{
    public async Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default)
    {
        dbContext.LeaveRequests.Add(leaveRequest);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<LeaveRequest?> GetByIdAsync(Guid leaveRequestId, CancellationToken cancellationToken = default)
    {
        return await dbContext.LeaveRequests
            .FirstOrDefaultAsync(leave => leave.Id == leaveRequestId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LeaveRequest>> GetByStaffAsync(
        Guid restaurantId,
        Guid staffUserId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.LeaveRequests
            .Where(leave => leave.RestaurantId == restaurantId && leave.StaffUserId == staffUserId)
            .OrderByDescending(leave => leave.FromDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<LeaveRequest>> GetByRestaurantAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.LeaveRequests
            .Where(leave => leave.RestaurantId == restaurantId)
            .OrderByDescending(leave => leave.FromDate)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default)
    {
        dbContext.LeaveRequests.Update(leaveRequest);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
