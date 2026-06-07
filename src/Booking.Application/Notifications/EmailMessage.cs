namespace Booking.Application.Notifications;

/// <summary>
/// A single outbound e-mail message. Plain-text body for MVP.
/// </summary>
public sealed record EmailMessage(string To, string Subject, string Body);
