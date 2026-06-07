using Booking.Domain.Notifications;

namespace Booking.Application.Abstractions;

public interface INotificationLogRepository
{
    Task AddAsync(NotificationLog notificationLog, CancellationToken cancellationToken = default);

    Task UpdateAsync(NotificationLog notificationLog, CancellationToken cancellationToken = default);
}
