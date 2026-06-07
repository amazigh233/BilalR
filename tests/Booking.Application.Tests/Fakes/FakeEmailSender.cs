using Booking.Application.Abstractions;
using Booking.Application.Notifications;

namespace Booking.Application.Tests.Fakes;

public sealed class FakeEmailSender : IEmailSender
{
    public List<EmailMessage> SentMessages { get; } = [];

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        SentMessages.Add(message);
        return Task.CompletedTask;
    }
}
