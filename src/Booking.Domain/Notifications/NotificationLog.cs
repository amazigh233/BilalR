namespace Booking.Domain.Notifications;

public sealed class NotificationLog
{
    public NotificationLog(
        Guid reservationId,
        Guid restaurantId,
        string recipientEmail,
        string subject,
        DateTime createdAtUtc)
    {
        if (reservationId == Guid.Empty)
        {
            throw new ArgumentException("Reservation id is required.", nameof(reservationId));
        }

        if (restaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(restaurantId));
        }

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            throw new ArgumentException("Recipient email is required.", nameof(recipientEmail));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Notification subject is required.", nameof(subject));
        }

        Id = Guid.NewGuid();
        ReservationId = reservationId;
        RestaurantId = restaurantId;
        RecipientEmail = recipientEmail.Trim();
        Subject = subject.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ReservationId { get; private set; }

    public Guid RestaurantId { get; private set; }

    public string RecipientEmail { get; private set; }

    public string Subject { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? SentAtUtc { get; private set; }

    public string? ErrorMessage { get; private set; }

    public void MarkSent(DateTime sentAtUtc)
    {
        SentAtUtc = sentAtUtc;
        ErrorMessage = null;
    }

    public void MarkFailed(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException("Error message is required.", nameof(errorMessage));
        }

        ErrorMessage = errorMessage.Trim();
    }
}
