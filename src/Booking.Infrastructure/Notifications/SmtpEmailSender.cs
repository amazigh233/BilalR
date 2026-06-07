using System.Net;
using System.Net.Mail;
using Booking.Application.Abstractions;
using Booking.Application.Notifications;

namespace Booking.Infrastructure.Notifications;

/// <summary>
/// Sends e-mail over SMTP using credentials from <see cref="EmailOptions"/>.
/// </summary>
public sealed class SmtpEmailSender(EmailOptions options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(options.SmtpHost, options.SmtpPort)
        {
            EnableSsl = options.UseSsl,
            Credentials = string.IsNullOrWhiteSpace(options.SmtpUsername)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(options.SmtpUsername, options.SmtpPassword)
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(options.FromAddress, options.FromName),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = false
        };
        mail.To.Add(message.To);

        await client.SendMailAsync(mail, cancellationToken);
    }
}
