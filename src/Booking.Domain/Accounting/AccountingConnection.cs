namespace Booking.Domain.Accounting;

public sealed class AccountingConnection
{
    private AccountingConnection()
    {
        ExternalId = string.Empty;
    }

    public AccountingConnection(
        Guid restaurantId,
        AccountingConnectionProvider provider,
        string externalId,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        RestaurantId = restaurantId;
        Provider = provider;
        ExternalId = externalId;
        Status = AccountingConnectionStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid RestaurantId { get; private set; }
    public AccountingConnectionProvider Provider { get; private set; }
    public AccountingConnectionStatus Status { get; private set; }
    public string ExternalId { get; private set; }
    public string? EncryptedAccessToken { get; private set; }
    public string? EncryptedRefreshToken { get; private set; }
    public DateTime? AccessExpiresAtUtc { get; private set; }
    public DateTime? LastSyncedAtUtc { get; private set; }
    public string? LastError { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public void Connect(string? accessToken, string? refreshToken, DateTime? expiresAtUtc)
    {
        EncryptedAccessToken = accessToken;
        EncryptedRefreshToken = refreshToken;
        AccessExpiresAtUtc = expiresAtUtc;
        Status = AccountingConnectionStatus.Connected;
        LastError = null;
    }

    public void RecordSync(DateTime syncedAtUtc)
    {
        LastSyncedAtUtc = syncedAtUtc;
        LastError = null;
    }

    public void RequireReconnect(string error)
    {
        Status = AccountingConnectionStatus.ReconnectRequired;
        LastError = error;
    }

    public void Disable()
    {
        Status = AccountingConnectionStatus.Disabled;
        EncryptedAccessToken = null;
        EncryptedRefreshToken = null;
    }
}
