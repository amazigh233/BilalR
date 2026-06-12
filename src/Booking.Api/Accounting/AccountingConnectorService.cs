using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Booking.Application.Abstractions;
using Booking.Application.Accounting;
using Booking.Domain.Accounting;
using Microsoft.AspNetCore.DataProtection;

namespace Booking.Api.Accounting;

public sealed record AccountingConnectionStartResponse(
    AccountingConnectionProvider Provider,
    string ExternalId,
    string AuthorizationUrl);

public sealed class AccountingConnectorService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IDataProtectionProvider dataProtectionProvider,
    IAccountingRepository accountingRepository)
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("Zambiq.Accounting.Connectors.v1");

    public bool IsConfigured(AccountingConnectionProvider provider) => provider switch
    {
        AccountingConnectionProvider.GoCardless =>
            !string.IsNullOrWhiteSpace(configuration["Accounting:GoCardless:SecretId"]) &&
            !string.IsNullOrWhiteSpace(configuration["Accounting:GoCardless:SecretKey"]),
        AccountingConnectionProvider.Mollie =>
            !string.IsNullOrWhiteSpace(configuration["Accounting:Mollie:ClientId"]) &&
            !string.IsNullOrWhiteSpace(configuration["Accounting:Mollie:ClientSecret"]),
        _ => false
    };

    public async Task<IReadOnlyCollection<(string Id, string Name)>> GetGoCardlessInstitutionsAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured(AccountingConnectionProvider.GoCardless);
        var token = await GetGoCardlessTokenAsync(cancellationToken);
        var client = httpClientFactory.CreateClient("AccountingGoCardless");
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v2/institutions/?country=nl");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        return document.RootElement.EnumerateArray()
            .Select(item => (item.GetProperty("id").GetString()!, item.GetProperty("name").GetString()!))
            .ToList();
    }

    public async Task<AccountingConnectionStartResponse> StartGoCardlessAsync(
        Guid restaurantId,
        string institutionId,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured(AccountingConnectionProvider.GoCardless);
        var token = await GetGoCardlessTokenAsync(cancellationToken);
        var callback = configuration["Accounting:GoCardless:RedirectUrl"]
            ?? throw new InvalidOperationException("Accounting:GoCardless:RedirectUrl is missing.");
        var client = httpClientFactory.CreateClient("AccountingGoCardless");
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/requisitions/")
        {
            Content = JsonContent.Create(new
            {
                redirect = callback,
                institution_id = institutionId,
                reference = restaurantId.ToString("N"),
                user_language = "NL"
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        var id = document.RootElement.GetProperty("id").GetString()!;
        var link = document.RootElement.GetProperty("link").GetString()!;
        var connection = new AccountingConnection(restaurantId, AccountingConnectionProvider.GoCardless, id, DateTime.UtcNow);
        await accountingRepository.AddConnectionAsync(connection, cancellationToken);
        return new AccountingConnectionStartResponse(AccountingConnectionProvider.GoCardless, id, link);
    }

    public async Task CompleteGoCardlessAsync(Guid restaurantId, string requisitionId, CancellationToken cancellationToken = default)
    {
        var connection = await accountingRepository.GetConnectionAsync(restaurantId, AccountingConnectionProvider.GoCardless, requisitionId, cancellationToken)
            ?? throw new KeyNotFoundException("GoCardless connection was not found.");
        var requisition = await GetGoCardlessRequisitionAsync(requisitionId, cancellationToken);
        if (requisition.GetProperty("status").GetString() != "LN")
        {
            throw new InvalidOperationException("Bank connection is not linked yet.");
        }
        connection.Connect(null, null, null);
        await accountingRepository.UpdateConnectionAsync(connection, cancellationToken);
    }

    public async Task<AccountingConnectionStartResponse> StartMollieAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured(AccountingConnectionProvider.Mollie);
        var state = Guid.NewGuid().ToString("N");
        var connection = new AccountingConnection(restaurantId, AccountingConnectionProvider.Mollie, state, DateTime.UtcNow);
        await accountingRepository.AddConnectionAsync(connection, cancellationToken);
        var clientId = Uri.EscapeDataString(configuration["Accounting:Mollie:ClientId"]!);
        var redirect = Uri.EscapeDataString(configuration["Accounting:Mollie:RedirectUrl"]
            ?? throw new InvalidOperationException("Accounting:Mollie:RedirectUrl is missing."));
        var scopes = Uri.EscapeDataString("payments.read balances.read settlements.read organizations.read");
        var url = $"https://my.mollie.com/oauth2/authorize?client_id={clientId}&state={state}&scope={scopes}&response_type=code&approval_prompt=auto&redirect_uri={redirect}";
        return new AccountingConnectionStartResponse(AccountingConnectionProvider.Mollie, state, url);
    }

    public async Task CompleteMollieAsync(Guid restaurantId, string state, string code, CancellationToken cancellationToken = default)
    {
        var connection = await accountingRepository.GetConnectionAsync(restaurantId, AccountingConnectionProvider.Mollie, state, cancellationToken)
            ?? throw new KeyNotFoundException("Mollie connection was not found.");
        var client = httpClientFactory.CreateClient("AccountingMollie");
        using var request = new HttpRequestMessage(HttpMethod.Post, "oauth2/tokens")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = configuration["Accounting:Mollie:RedirectUrl"]!,
                ["client_id"] = configuration["Accounting:Mollie:ClientId"]!,
                ["client_secret"] = configuration["Accounting:Mollie:ClientSecret"]!
            })
        };
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        var root = document.RootElement;
        connection.Connect(
            _protector.Protect(root.GetProperty("access_token").GetString()!),
            root.TryGetProperty("refresh_token", out var refresh) ? _protector.Protect(refresh.GetString()!) : null,
            DateTime.UtcNow.AddSeconds(root.GetProperty("expires_in").GetInt32()));
        await accountingRepository.UpdateConnectionAsync(connection, cancellationToken);
    }

    public async Task<int> SyncAsync(
        Guid restaurantId,
        AccountingConnectionProvider provider,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        var connection = await accountingRepository.GetConnectionAsync(restaurantId, provider, externalId, cancellationToken)
            ?? throw new KeyNotFoundException("Accounting connection was not found.");
        if (connection.Status != AccountingConnectionStatus.Connected)
        {
            throw new InvalidOperationException("Accounting connection is not connected.");
        }

        try
        {
            var imported = provider switch
            {
                AccountingConnectionProvider.GoCardless => await SyncGoCardlessAsync(connection, cancellationToken),
                AccountingConnectionProvider.Mollie => await SyncMollieAsync(connection, cancellationToken),
                _ => 0
            };
            connection.RecordSync(DateTime.UtcNow);
            await accountingRepository.UpdateConnectionAsync(connection, cancellationToken);
            return imported;
        }
        catch (Exception exception)
        {
            connection.RequireReconnect(exception.Message[..Math.Min(exception.Message.Length, 1000)]);
            await accountingRepository.UpdateConnectionAsync(connection, cancellationToken);
            throw;
        }
    }

    public async Task DisableAsync(Guid restaurantId, AccountingConnectionProvider provider, string externalId, CancellationToken cancellationToken = default)
    {
        var connection = await accountingRepository.GetConnectionAsync(restaurantId, provider, externalId, cancellationToken)
            ?? throw new KeyNotFoundException("Accounting connection was not found.");
        connection.Disable();
        await accountingRepository.UpdateConnectionAsync(connection, cancellationToken);
    }

    private async Task<int> SyncGoCardlessAsync(AccountingConnection connection, CancellationToken cancellationToken)
    {
        var requisition = await GetGoCardlessRequisitionAsync(connection.ExternalId, cancellationToken);
        if (requisition.GetProperty("status").GetString() != "LN")
        {
            throw new InvalidOperationException("GoCardless consent is expired or no longer linked.");
        }
        var token = await GetGoCardlessTokenAsync(cancellationToken);
        var client = httpClientFactory.CreateClient("AccountingGoCardless");
        var candidates = new List<AccountingSourceTransaction>();
        foreach (var account in requisition.GetProperty("accounts").EnumerateArray())
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v2/accounts/{account.GetString()}/transactions/");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
            foreach (var item in document.RootElement.GetProperty("transactions").GetProperty("booked").EnumerateArray())
            {
                var amountData = item.GetProperty("transactionAmount");
                var currency = amountData.GetProperty("currency").GetString() ?? string.Empty;
                if (currency != "EUR") continue;
                var amount = decimal.Parse(amountData.GetProperty("amount").GetString()!, CultureInfo.InvariantCulture);
                var date = DateOnly.Parse(item.GetProperty("bookingDate").GetString()!, CultureInfo.InvariantCulture);
                var externalId = item.TryGetProperty("transactionId", out var id) ? id.GetString()! : AccountingUseCases.Fingerprint(item.ToString(), date, amount, currency);
                var description = item.TryGetProperty("remittanceInformationUnstructured", out var remittance) ? remittance.GetString()! : "Banktransactie";
                candidates.Add(new AccountingSourceTransaction(connection.RestaurantId, AccountingSourceKind.GoCardless, externalId, AccountingUseCases.Fingerprint(externalId, date, amount, currency), date, description, amount, currency, item.ToString(), DateTime.UtcNow));
            }
        }
        return await SaveCandidatesAsync(connection.RestaurantId, candidates, cancellationToken);
    }

    private async Task<int> SyncMollieAsync(AccountingConnection connection, CancellationToken cancellationToken)
    {
        var accessToken = await GetValidMollieAccessTokenAsync(connection, cancellationToken);
        var client = httpClientFactory.CreateClient("AccountingMollieApi");
        var rawTransactions = new List<(DateOnly Date, string Group, decimal Amount, JsonElement Payload)>();
        string? nextUrl = "v2/balances/primary/transactions?limit=250";
        while (!string.IsNullOrWhiteSpace(nextUrl))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, nextUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
            var root = document.RootElement;
            foreach (var item in root.GetProperty("_embedded").GetProperty("balance_transactions").EnumerateArray())
            {
                var amountData = item.GetProperty("resultAmount");
                var currency = amountData.GetProperty("currency").GetString() ?? string.Empty;
                if (currency != "EUR") continue;
                var amount = decimal.Parse(amountData.GetProperty("value").GetString()!, CultureInfo.InvariantCulture);
                var date = DateOnly.FromDateTime(item.GetProperty("createdAt").GetDateTime());
                var type = item.GetProperty("type").GetString() ?? "transaction";
                rawTransactions.Add((date, MollieGroup(type), amount, item.Clone()));
            }
            nextUrl = root.GetProperty("_links").TryGetProperty("next", out var next) && next.ValueKind != JsonValueKind.Null
                ? next.GetProperty("href").GetString()?.Replace("https://api.mollie.com/", string.Empty, StringComparison.OrdinalIgnoreCase)
                : null;
        }
        var candidates = rawTransactions
            .GroupBy(item => new { item.Date, item.Group })
            .Select(group =>
            {
                var externalId = $"mollie:{group.Key.Group}:{group.Key.Date:yyyy-MM-dd}";
                var amount = group.Sum(item => item.Amount);
                return new AccountingSourceTransaction(
                    connection.RestaurantId,
                    AccountingSourceKind.Mollie,
                    externalId,
                    AccountingUseCases.Fingerprint(externalId, group.Key.Date, amount, "EUR"),
                    group.Key.Date,
                    $"Mollie {group.Key.Group} dagtotaal",
                    amount,
                    "EUR",
                    JsonSerializer.Serialize(group.Select(item => item.Payload)),
                    DateTime.UtcNow);
            })
            .ToList();
        return await SaveCandidatesAsync(connection.RestaurantId, candidates, cancellationToken);
    }

    private static string MollieGroup(string type)
    {
        if (type.Contains("fee", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("commission", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("deduction", StringComparison.OrdinalIgnoreCase))
        {
            return "kosten";
        }
        if (type.Contains("settlement", StringComparison.OrdinalIgnoreCase)) return "settlement";
        if (type.Contains("refund", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("chargeback", StringComparison.OrdinalIgnoreCase))
        {
            return "terugbetaling";
        }
        return "betalingen";
    }

    private async Task<int> SaveCandidatesAsync(Guid restaurantId, List<AccountingSourceTransaction> candidates, CancellationToken cancellationToken)
    {
        var existing = await accountingRepository.GetExistingFingerprintsAsync(restaurantId, candidates.Select(item => item.Fingerprint), cancellationToken);
        var newItems = candidates.Where(item => !existing.Contains(item.Fingerprint)).ToList();
        if (newItems.Count > 0) await accountingRepository.AddSourceTransactionsAsync(newItems, cancellationToken);
        return newItems.Count;
    }

    private async Task<JsonElement> GetGoCardlessRequisitionAsync(string requisitionId, CancellationToken cancellationToken)
    {
        var token = await GetGoCardlessTokenAsync(cancellationToken);
        var client = httpClientFactory.CreateClient("AccountingGoCardless");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v2/requisitions/{requisitionId}/");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        return document.RootElement.Clone();
    }

    private async Task<string> GetGoCardlessTokenAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("AccountingGoCardless");
        using var response = await client.PostAsJsonAsync("api/v2/token/new/", new
        {
            secret_id = configuration["Accounting:GoCardless:SecretId"],
            secret_key = configuration["Accounting:GoCardless:SecretKey"]
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        return document.RootElement.GetProperty("access").GetString()!;
    }

    private async Task<string> GetValidMollieAccessTokenAsync(AccountingConnection connection, CancellationToken cancellationToken)
    {
        if (connection.EncryptedAccessToken is null) throw new InvalidOperationException("Mollie access token is missing.");
        if (connection.AccessExpiresAtUtc > DateTime.UtcNow.AddMinutes(2)) return _protector.Unprotect(connection.EncryptedAccessToken);
        if (connection.EncryptedRefreshToken is null) throw new InvalidOperationException("Mollie refresh token is missing.");

        var client = httpClientFactory.CreateClient("AccountingMollie");
        using var response = await client.PostAsync("oauth2/tokens", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = _protector.Unprotect(connection.EncryptedRefreshToken),
            ["client_id"] = configuration["Accounting:Mollie:ClientId"]!,
            ["client_secret"] = configuration["Accounting:Mollie:ClientSecret"]!
        }), cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        var root = document.RootElement;
        var token = root.GetProperty("access_token").GetString()!;
        connection.Connect(
            _protector.Protect(token),
            root.TryGetProperty("refresh_token", out var refresh) ? _protector.Protect(refresh.GetString()!) : connection.EncryptedRefreshToken,
            DateTime.UtcNow.AddSeconds(root.GetProperty("expires_in").GetInt32()));
        await accountingRepository.UpdateConnectionAsync(connection, cancellationToken);
        return token;
    }

    private void EnsureConfigured(AccountingConnectionProvider provider)
    {
        if (!IsConfigured(provider)) throw new InvalidOperationException($"{provider} is niet geconfigureerd voor deze omgeving.");
    }
}
