namespace Booking.Domain.GoogleBusiness;

public enum GoogleBusinessConnectionStatus { Pending, Connected, ReconnectRequired, Disabled }

public sealed class GoogleBusinessConnection
{
    private GoogleBusinessConnection()
    {
        OAuthState = string.Empty;
    }

    public GoogleBusinessConnection(Guid restaurantId, string oAuthState, DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        RestaurantId = restaurantId;
        OAuthState = oAuthState;
        Status = GoogleBusinessConnectionStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid RestaurantId { get; private set; }
    public GoogleBusinessConnectionStatus Status { get; private set; }
    public string OAuthState { get; private set; }
    public string? GbpAccountName { get; private set; }
    public string? GbpLocationName { get; private set; }
    public string? EncryptedAccessToken { get; private set; }
    public string? EncryptedRefreshToken { get; private set; }
    public DateTime? AccessExpiresAtUtc { get; private set; }
    public DateTime? LastReviewSyncAtUtc { get; private set; }
    public DateTime? LastHoursSyncAtUtc { get; private set; }
    public string? LastSyncError { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public void Connect(string encAccessToken, string? encRefreshToken, DateTime expiresAtUtc)
    {
        EncryptedAccessToken = encAccessToken;
        EncryptedRefreshToken = encRefreshToken;
        AccessExpiresAtUtc = expiresAtUtc;
        Status = GoogleBusinessConnectionStatus.Connected;
        LastSyncError = null;
    }

    public void SetLocation(string gbpAccountName, string gbpLocationName)
    {
        GbpAccountName = gbpAccountName;
        GbpLocationName = gbpLocationName;
    }

    public void UpdateTokens(string encAccessToken, string? encRefreshToken, DateTime expiresAtUtc)
    {
        EncryptedAccessToken = encAccessToken;
        EncryptedRefreshToken = encRefreshToken ?? EncryptedRefreshToken;
        AccessExpiresAtUtc = expiresAtUtc;
    }

    public void MarkReviewSynced(DateTime syncedAtUtc)
    {
        LastReviewSyncAtUtc = syncedAtUtc;
        LastSyncError = null;
    }

    public void MarkHoursSynced(DateTime syncedAtUtc)
    {
        LastHoursSyncAtUtc = syncedAtUtc;
        LastSyncError = null;
    }

    public void MarkSyncError(string error)
    {
        LastSyncError = error;
    }

    public void RequireReconnect(string error)
    {
        Status = GoogleBusinessConnectionStatus.ReconnectRequired;
        LastSyncError = error;
    }

    public void Disable()
    {
        Status = GoogleBusinessConnectionStatus.Disabled;
        EncryptedAccessToken = null;
        EncryptedRefreshToken = null;
    }
}
