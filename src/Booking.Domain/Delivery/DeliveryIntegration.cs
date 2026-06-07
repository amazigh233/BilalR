namespace Booking.Domain.Delivery;

public sealed class DeliveryIntegration
{
    private DeliveryIntegration()
    {
    }

    public DeliveryIntegration(
        Guid restaurantId,
        DeliveryProvider provider,
        string webhookSecretHash,
        DateTime createdAtUtc)
    {
        if (restaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(restaurantId));
        }

        if (string.IsNullOrWhiteSpace(webhookSecretHash))
        {
            throw new ArgumentException("Webhook secret hash is required.", nameof(webhookSecretHash));
        }

        Id = Guid.NewGuid();
        RestaurantId = restaurantId;
        Provider = provider;
        WebhookSecretHash = webhookSecretHash;
        Enabled = true;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid RestaurantId { get; private set; }

    public DeliveryProvider Provider { get; private set; }

    public string WebhookSecretHash { get; private set; } = null!;

    public bool Enabled { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? LastRotatedAtUtc { get; private set; }

    public void Rotate(string newSecretHash, DateTime rotatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(newSecretHash))
        {
            throw new ArgumentException("Webhook secret hash is required.", nameof(newSecretHash));
        }

        WebhookSecretHash = newSecretHash;
        Enabled = true;
        LastRotatedAtUtc = rotatedAtUtc;
    }

    public void Disable()
    {
        Enabled = false;
    }

    public void Enable()
    {
        Enabled = true;
    }
}
