using System.Net.Http.Headers;
using System.Text.Json;
using Booking.Application.Abstractions;
using Booking.Domain.GoogleBusiness;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.AspNetCore.DataProtection;

namespace Booking.Api.GoogleBusiness;

public sealed record GbpLocationSummary(string AccountName, string LocationName, string Title, string? StoreCode);

public sealed class GoogleBusinessService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IDataProtectionProvider dataProtectionProvider,
    IGoogleBusinessRepository repository)
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("Zambiq.GoogleBusiness.v1");

    public bool IsEnabled => configuration.GetValue<bool>("GoogleBusiness:Enabled");

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(configuration["GoogleBusiness:ClientId"]) &&
        !string.IsNullOrWhiteSpace(configuration["GoogleBusiness:ClientSecret"]);

    public async Task<string> StartOAuthAsync(Guid restaurantId, CancellationToken ct = default)
    {
        EnsureConfigured();
        var state = Guid.NewGuid().ToString("N");

        // Remove any existing pending connection for this restaurant first
        var existing = await repository.GetConnectionAsync(restaurantId, ct);
        if (existing is not null && existing.Status == GoogleBusinessConnectionStatus.Pending)
        {
            existing.Disable();
            await repository.UpdateConnectionAsync(existing, ct);
        }

        var connection = new GoogleBusinessConnection(restaurantId, state, DateTime.UtcNow);
        await repository.AddConnectionAsync(connection, ct);

        var clientId = Uri.EscapeDataString(configuration["GoogleBusiness:ClientId"]!);
        var redirectUri = Uri.EscapeDataString(RedirectUrl);
        var scope = Uri.EscapeDataString("https://www.googleapis.com/auth/business.manage");
        return $"https://accounts.google.com/o/oauth2/v2/auth" +
               $"?client_id={clientId}" +
               $"&redirect_uri={redirectUri}" +
               $"&response_type=code" +
               $"&scope={scope}" +
               $"&access_type=offline" +
               $"&prompt=consent" +
               $"&state={state}";
    }

    public async Task<IReadOnlyCollection<GbpLocationSummary>> CompleteOAuthAsync(
        string state,
        string code,
        CancellationToken ct = default)
    {
        EnsureConfigured();

        var connection = await repository.GetConnectionByStateAsync(state, ct)
            ?? throw new KeyNotFoundException("Google Business Profile verbinding niet gevonden.");

        // Exchange code for tokens
        var client = httpClientFactory.CreateClient("GoogleOAuth");
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = RedirectUrl,
                ["client_id"] = configuration["GoogleBusiness:ClientId"]!,
                ["client_secret"] = configuration["GoogleBusiness:ClientSecret"]!
            })
        };
        using var tokenResponse = await client.SendAsync(tokenRequest, ct);
        tokenResponse.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await tokenResponse.Content.ReadAsStreamAsync(ct));
        var root = doc.RootElement;
        var accessToken = root.GetProperty("access_token").GetString()!;
        var refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var expiresIn = root.GetProperty("expires_in").GetInt32();

        connection.Connect(
            _protector.Protect(accessToken),
            refreshToken is not null ? _protector.Protect(refreshToken) : null,
            DateTime.UtcNow.AddSeconds(expiresIn));
        await repository.UpdateConnectionAsync(connection, ct);

        // List accounts/locations so user can pick their location
        return await ListLocationsAsync(accessToken, ct);
    }

    public async Task FinalizeLocationAsync(
        Guid restaurantId,
        string gbpAccountName,
        string gbpLocationName,
        CancellationToken ct = default)
    {
        var connection = await repository.GetConnectionAsync(restaurantId, ct)
            ?? throw new KeyNotFoundException("Google Business Profile verbinding niet gevonden.");
        connection.SetLocation(gbpAccountName, gbpLocationName);
        await repository.UpdateConnectionAsync(connection, ct);
    }

    public async Task<string> GetValidAccessTokenAsync(GoogleBusinessConnection connection, CancellationToken ct = default)
    {
        if (connection.EncryptedAccessToken is null)
            throw new InvalidOperationException("Geen toegangstoken beschikbaar.");

        // Token still valid with 2-minute buffer
        if (connection.AccessExpiresAtUtc > DateTime.UtcNow.AddMinutes(2))
            return _protector.Unprotect(connection.EncryptedAccessToken);

        // Refresh
        if (connection.EncryptedRefreshToken is null)
            throw new InvalidOperationException("Geen verversingstoken beschikbaar. Verbind opnieuw.");

        var client = httpClientFactory.CreateClient("GoogleOAuth");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = _protector.Unprotect(connection.EncryptedRefreshToken),
                ["client_id"] = configuration["GoogleBusiness:ClientId"]!,
                ["client_secret"] = configuration["GoogleBusiness:ClientSecret"]!
            })
        };
        using var response = await client.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            connection.RequireReconnect("Token verversen mislukt — verbind Google opnieuw.");
            await repository.UpdateConnectionAsync(connection, ct);
            throw new InvalidOperationException("Google token verversen mislukt.");
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        var root = doc.RootElement;
        var newAccessToken = root.GetProperty("access_token").GetString()!;
        var newRefreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var expiresIn = root.GetProperty("expires_in").GetInt32();

        connection.UpdateTokens(
            _protector.Protect(newAccessToken),
            newRefreshToken is not null ? _protector.Protect(newRefreshToken) : null,
            DateTime.UtcNow.AddSeconds(expiresIn));
        await repository.UpdateConnectionAsync(connection, ct);
        return newAccessToken;
    }

    public async Task DisconnectAsync(Guid restaurantId, CancellationToken ct = default)
    {
        var connection = await repository.GetConnectionAsync(restaurantId, ct)
            ?? throw new KeyNotFoundException("Google Business Profile verbinding niet gevonden.");

        // Best-effort token revocation — don't block on failure
        if (connection.EncryptedAccessToken is not null)
        {
            try
            {
                var token = _protector.Unprotect(connection.EncryptedAccessToken);
                var client = httpClientFactory.CreateClient("GoogleOAuth");
                await client.PostAsync(
                    $"https://oauth2.googleapis.com/revoke?token={Uri.EscapeDataString(token)}",
                    null, ct);
            }
            catch { /* revocation failure is non-fatal */ }
        }

        connection.Disable();
        await repository.UpdateConnectionAsync(connection, ct);
    }

    public async Task SyncOpeningHoursAsync(
        GoogleBusinessConnection connection,
        IReadOnlyCollection<(DayOfWeek DayOfWeek, TimeOnly OpensAt, TimeOnly ClosesAt)> hours,
        CancellationToken ct = default)
    {
        if (connection.GbpLocationName is null)
            throw new InvalidOperationException("Geen locatie gekoppeld aan verbinding.");

        var accessToken = await GetValidAccessTokenAsync(connection, ct);
        var credential = GoogleCredential.FromAccessToken(accessToken);
        var infoService = new Google.Apis.MyBusinessBusinessInformation.v1.MyBusinessBusinessInformationService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Zambiq"
            });

        var periods = hours.Select(h => new Google.Apis.MyBusinessBusinessInformation.v1.Data.TimePeriod
        {
            OpenDay = MapDayOfWeek(h.DayOfWeek),
            OpenTime = new Google.Apis.MyBusinessBusinessInformation.v1.Data.TimeOfDay
            {
                Hours = h.OpensAt.Hour,
                Minutes = h.OpensAt.Minute
            },
            CloseDay = MapDayOfWeek(h.DayOfWeek),
            CloseTime = new Google.Apis.MyBusinessBusinessInformation.v1.Data.TimeOfDay
            {
                Hours = h.ClosesAt.Hour,
                Minutes = h.ClosesAt.Minute
            }
        }).ToList();

        var location = new Google.Apis.MyBusinessBusinessInformation.v1.Data.Location
        {
            RegularHours = new Google.Apis.MyBusinessBusinessInformation.v1.Data.BusinessHours
            {
                Periods = periods
            }
        };

        var patchRequest = infoService.Locations.Patch(location, connection.GbpLocationName);
        patchRequest.UpdateMask = "regularHours";
        await patchRequest.ExecuteAsync(ct);

        connection.MarkHoursSynced(DateTime.UtcNow);
        await repository.UpdateConnectionAsync(connection, ct);
    }

    public async Task<IReadOnlyCollection<GbpReviewItem>> GetReviewsFromGbpAsync(
        GoogleBusinessConnection connection,
        CancellationToken ct = default)
    {
        if (connection.GbpLocationName is null)
            throw new InvalidOperationException("Geen locatie gekoppeld aan verbinding.");

        var accessToken = await GetValidAccessTokenAsync(connection, ct);
        var client = httpClientFactory.CreateClient("GoogleMyBusinessV4");
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{connection.GbpLocationName}/reviews?pageSize=50");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        var reviews = new List<GbpReviewItem>();
        if (!doc.RootElement.TryGetProperty("reviews", out var reviewArray))
            return reviews;

        foreach (var item in reviewArray.EnumerateArray())
        {
            var name = item.GetProperty("name").GetString()!;
            var reviewer = item.TryGetProperty("reviewer", out var rev)
                ? rev.TryGetProperty("displayName", out var dn) ? dn.GetString() : null
                : null;
            var stars = MapStarRating(
                item.TryGetProperty("starRating", out var sr) ? sr.GetString() : null);
            var comment = item.TryGetProperty("comment", out var c) ? c.GetString() : null;
            var createTime = item.GetProperty("createTime").GetDateTime();
            var updateTime = item.GetProperty("updateTime").GetDateTime();
            string? replyComment = null;
            DateTime? replyUpdateTime = null;
            if (item.TryGetProperty("reviewReply", out var reply))
            {
                replyComment = reply.TryGetProperty("comment", out var rc) ? rc.GetString() : null;
                replyUpdateTime = reply.TryGetProperty("updateTime", out var rut)
                    ? rut.GetDateTime()
                    : null;
            }
            reviews.Add(new GbpReviewItem(name, reviewer, stars, comment, createTime, updateTime, replyComment, replyUpdateTime));
        }
        return reviews;
    }

    public async Task UpsertReviewReplyAsync(
        GoogleBusinessConnection connection,
        string reviewName,
        string comment,
        CancellationToken ct = default)
    {
        var accessToken = await GetValidAccessTokenAsync(connection, ct);
        var client = httpClientFactory.CreateClient("GoogleMyBusinessV4");
        using var request = new HttpRequestMessage(HttpMethod.Put, $"{reviewName}/reply")
        {
            Content = JsonContent.Create(new { comment })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteReviewReplyAsync(
        GoogleBusinessConnection connection,
        string reviewName,
        CancellationToken ct = default)
    {
        var accessToken = await GetValidAccessTokenAsync(connection, ct);
        var client = httpClientFactory.CreateClient("GoogleMyBusinessV4");
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{reviewName}/reply");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task<IReadOnlyCollection<GbpLocationSummary>> ListLocationsAsync(string accessToken, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("GoogleAccountManagementV1");

        using var accountsReq = new HttpRequestMessage(HttpMethod.Get, "accounts");
        accountsReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var accountsResp = await client.SendAsync(accountsReq, ct);
        accountsResp.EnsureSuccessStatusCode();

        using var accountsDoc = JsonDocument.Parse(await accountsResp.Content.ReadAsStreamAsync(ct));
        var locations = new List<GbpLocationSummary>();

        if (!accountsDoc.RootElement.TryGetProperty("accounts", out var accountsArray))
            return locations;

        foreach (var account in accountsArray.EnumerateArray())
        {
            var accountName = account.GetProperty("name").GetString()!;
            using var locsReq = new HttpRequestMessage(HttpMethod.Get, $"{accountName}/locations");
            locsReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var locsResp = await client.SendAsync(locsReq, ct);
            if (!locsResp.IsSuccessStatusCode) continue;

            using var locsDoc = JsonDocument.Parse(await locsResp.Content.ReadAsStreamAsync(ct));
            if (!locsDoc.RootElement.TryGetProperty("locations", out var locsArray)) continue;

            foreach (var loc in locsArray.EnumerateArray())
            {
                var locationName = loc.GetProperty("name").GetString()!;
                var title = loc.TryGetProperty("title", out var t) ? t.GetString() ?? locationName : locationName;
                var storeCode = loc.TryGetProperty("storeCode", out var sc) ? sc.GetString() : null;
                locations.Add(new GbpLocationSummary(accountName, locationName, title, storeCode));
            }
        }
        return locations;
    }

    private static string MapDayOfWeek(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "MONDAY",
        DayOfWeek.Tuesday => "TUESDAY",
        DayOfWeek.Wednesday => "WEDNESDAY",
        DayOfWeek.Thursday => "THURSDAY",
        DayOfWeek.Friday => "FRIDAY",
        DayOfWeek.Saturday => "SATURDAY",
        DayOfWeek.Sunday => "SUNDAY",
        _ => "MONDAY"
    };

    private static int MapStarRating(string? starRating) => starRating switch
    {
        "ONE" => 1,
        "TWO" => 2,
        "THREE" => 3,
        "FOUR" => 4,
        "FIVE" => 5,
        _ => 0
    };

    private string RedirectUrl =>
        configuration["GoogleBusiness:RedirectUrl"]
        ?? throw new InvalidOperationException("GoogleBusiness:RedirectUrl ontbreekt.");

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Google Business Profile is niet geconfigureerd. Voeg ClientId en ClientSecret toe.");
    }
}

public sealed record GbpReviewItem(
    string ReviewName,
    string? ReviewerDisplayName,
    int StarRating,
    string? Comment,
    DateTime CreateTime,
    DateTime UpdateTime,
    string? ReplyComment,
    DateTime? ReplyUpdateTime);
