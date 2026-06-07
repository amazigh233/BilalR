using Booking.Application.Abstractions;
using Booking.Domain.Notifications;

namespace Booking.Application.Tests.Fakes;

public sealed class FakeNotificationLogRepository : INotificationLogRepository
{
    public List<NotificationLog> Logs { get; } = [];

    public Task AddAsync(NotificationLog notificationLog, CancellationToken cancellationToken = default)
    {
        Logs.Add(notificationLog);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(NotificationLog notificationLog, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
