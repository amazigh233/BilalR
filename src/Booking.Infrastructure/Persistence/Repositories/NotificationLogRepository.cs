using Booking.Application.Abstractions;
using Booking.Domain.Notifications;

namespace Booking.Infrastructure.Persistence.Repositories;

public sealed class NotificationLogRepository(BookingDbContext dbContext) : INotificationLogRepository
{
    public async Task AddAsync(NotificationLog notificationLog, CancellationToken cancellationToken = default)
    {
        dbContext.NotificationLogs.Add(notificationLog);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(NotificationLog notificationLog, CancellationToken cancellationToken = default)
    {
        dbContext.NotificationLogs.Update(notificationLog);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
