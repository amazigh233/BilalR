namespace Booking.Infrastructure.Notifications;

/// <summary>
/// SMTP configuration, bound from the "Email" configuration section.
/// Secrets (username/password) must come from environment variables, never hardcoded.
/// When <see cref="SmtpHost"/> is empty, the platform falls back to a logging-only sender.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string? SmtpHost { get; init; }

    public int SmtpPort { get; init; } = 587;

    public string? SmtpUsername { get; init; }

    public string? SmtpPassword { get; init; }

    public bool UseSsl { get; init; } = true;

    public string FromAddress { get; init; } = "no-reply@zambiq.local";

    public string FromName { get; init; } = "Zambiq";
}
