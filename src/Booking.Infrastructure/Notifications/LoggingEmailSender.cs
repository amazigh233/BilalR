using Booking.Application.Abstractions;
using Booking.Application.Notifications;
using Microsoft.Extensions.Logging;

namespace Booking.Infrastructure.Notifications;

/// <summary>
/// Fallback sender used when no SMTP host is configured (local dev / tests).
/// Logs the message instead of sending it so the booking flow keeps working without secrets.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "E-mail not sent (no SMTP configured). To={Recipient}; Subject={Subject}",
            message.To,
            message.Subject);

        return Task.CompletedTask;
    }
}
