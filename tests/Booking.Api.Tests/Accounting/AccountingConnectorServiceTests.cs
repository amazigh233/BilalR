using System.Net;
using System.Text;
using Booking.Api.Accounting;
using Booking.Application.Abstractions;
using Booking.Domain.Accounting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;

namespace Booking.Api.Tests.Accounting;

public sealed class AccountingConnectorServiceTests
{
    [Fact]
    public async Task GoCardlessSync_IsIdempotent_WithBookedEurTransactions()
    {
        var repository = new StubAccountingRepository();
        var service = CreateService(repository, new GoCardlessHandler(), new MollieHandler());
        var restaurantId = Guid.NewGuid();

        var start = await service.StartGoCardlessAsync(restaurantId, "TEST_BANK");
        await service.CompleteGoCardlessAsync(restaurantId, start.ExternalId);
        var first = await service.SyncAsync(restaurantId, AccountingConnectionProvider.GoCardless, start.ExternalId);
        var second = await service.SyncAsync(restaurantId, AccountingConnectionProvider.GoCardless, start.ExternalId);

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Single(repository.Sources);
        Assert.Equal("EUR", repository.Sources.Single().Currency);
    }

    [Fact]
    public async Task MollieOAuth_StoresProtectedTokens()
    {
        var repository = new StubAccountingRepository();
        var service = CreateService(repository, new GoCardlessHandler(), new MollieHandler());
        var restaurantId = Guid.NewGuid();

        var start = await service.StartMollieAsync(restaurantId);
        await service.CompleteMollieAsync(restaurantId, start.ExternalId, "authorization-code");

        var connection = Assert.Single(repository.Connections);
        Assert.Equal(AccountingConnectionStatus.Connected, connection.Status);
        Assert.NotEqual("access-token", connection.EncryptedAccessToken);
        Assert.NotEqual("refresh-token", connection.EncryptedRefreshToken);
    }

    private static AccountingConnectorService CreateService(
        StubAccountingRepository repository,
        HttpMessageHandler goCardless,
        HttpMessageHandler mollie)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Accounting:GoCardless:SecretId"] = "secret-id",
            ["Accounting:GoCardless:SecretKey"] = "secret-key",
            ["Accounting:GoCardless:RedirectUrl"] = "https://example.test/callback",
            ["Accounting:Mollie:ClientId"] = "client-id",
            ["Accounting:Mollie:ClientSecret"] = "client-secret",
            ["Accounting:Mollie:RedirectUrl"] = "https://example.test/mollie"
        }).Build();
        return new AccountingConnectorService(
            new StubHttpClientFactory(goCardless, mollie),
            configuration,
            new EphemeralDataProtectionProvider(),
            repository);
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler goCardless, HttpMessageHandler mollie) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            var client = new HttpClient(name == "AccountingGoCardless" ? goCardless : mollie, disposeHandler: false);
            client.BaseAddress = name == "AccountingGoCardless"
                ? new Uri("https://bankaccountdata.gocardless.com/")
                : new Uri("https://api.mollie.com/");
            return client;
        }
    }

    private sealed class GoCardlessHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            var json = path switch
            {
                "/api/v2/token/new/" => """{"access":"gc-token"}""",
                "/api/v2/requisitions/" when request.Method == HttpMethod.Post => """{"id":"req-1","link":"https://bank.test/link"}""",
                "/api/v2/requisitions/req-1/" => """{"id":"req-1","status":"LN","accounts":["account-1"]}""",
                "/api/v2/accounts/account-1/transactions/" => """{"transactions":{"booked":[{"transactionId":"tx-1","transactionAmount":{"currency":"EUR","amount":"45.00"},"bookingDate":"2026-06-01","remittanceInformationUnstructured":"Test betaling"}]}}""",
                _ => throw new InvalidOperationException(path)
            };
            return Task.FromResult(Json(json));
        }
    }

    private sealed class MollieHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(Json("""{"access_token":"access-token","refresh_token":"refresh-token","expires_in":3600}"""));
    }

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubAccountingRepository : IAccountingRepository
    {
        public List<AccountingConnection> Connections { get; } = [];
        public List<AccountingSourceTransaction> Sources { get; } = [];
        public Task AddConnectionAsync(AccountingConnection connection, CancellationToken cancellationToken = default) { Connections.Add(connection); return Task.CompletedTask; }
        public Task UpdateConnectionAsync(AccountingConnection connection, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<AccountingConnection?> GetConnectionAsync(Guid restaurantId, AccountingConnectionProvider provider, string externalId, CancellationToken cancellationToken = default) => Task.FromResult(Connections.FirstOrDefault(x => x.RestaurantId == restaurantId && x.Provider == provider && x.ExternalId == externalId));
        public Task<HashSet<string>> GetExistingFingerprintsAsync(Guid restaurantId, IEnumerable<string> fingerprints, CancellationToken cancellationToken = default) => Task.FromResult(Sources.Where(x => x.RestaurantId == restaurantId && fingerprints.Contains(x.Fingerprint)).Select(x => x.Fingerprint).ToHashSet());
        public Task AddSourceTransactionsAsync(IEnumerable<AccountingSourceTransaction> transactions, CancellationToken cancellationToken = default) { Sources.AddRange(transactions); return Task.CompletedTask; }
        public Task<IReadOnlyCollection<AccountingCategory>> GetCategoriesAsync(Guid restaurantId, CancellationToken cancellationToken = default) => Empty<AccountingCategory>();
        public Task<AccountingCategory?> GetCategoryAsync(Guid restaurantId, Guid categoryId, CancellationToken cancellationToken = default) => Task.FromResult<AccountingCategory?>(null);
        public Task AddCategoriesAsync(IEnumerable<AccountingCategory> categories, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddCategoryAsync(AccountingCategory category, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateCategoryAsync(AccountingCategory category, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyCollection<AccountingEntry>> GetEntriesAsync(Guid restaurantId, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default) => Empty<AccountingEntry>();
        public Task<AccountingEntry?> GetEntryAsync(Guid restaurantId, Guid entryId, CancellationToken cancellationToken = default) => Task.FromResult<AccountingEntry?>(null);
        public Task AddEntryAsync(AccountingEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddEntriesAsync(IEnumerable<AccountingEntry> entries, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateEntryAsync(AccountingEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyCollection<AccountingSourceTransaction>> GetSourceTransactionsAsync(Guid restaurantId, AccountingSourceStatus? status = null, CancellationToken cancellationToken = default) => Empty<AccountingSourceTransaction>();
        public Task<AccountingSourceTransaction?> GetSourceTransactionAsync(Guid restaurantId, Guid sourceTransactionId, CancellationToken cancellationToken = default) => Task.FromResult<AccountingSourceTransaction?>(null);
        public Task UpdateSourceTransactionAsync(AccountingSourceTransaction transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ImportChecksumExistsAsync(Guid restaurantId, string checksum, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddImportBatchAsync(AccountingImportBatch batch, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyCollection<AccountingImportBatch>> GetImportBatchesAsync(Guid restaurantId, CancellationToken cancellationToken = default) => Empty<AccountingImportBatch>();
        public Task AddMatchAsync(AccountingMatch match, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyCollection<AccountingConnection>> GetConnectionsAsync(Guid restaurantId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<AccountingConnection>>(Connections);
        public Task<IReadOnlyCollection<AccountingConnection>> GetConnectionsDueForSyncAsync(DateTime beforeUtc, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<AccountingConnection>>(Connections);
        public Task AddAttachmentAsync(AccountingAttachment attachment, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<AccountingAttachment?> GetAttachmentAsync(Guid restaurantId, Guid attachmentId, CancellationToken cancellationToken = default) => Task.FromResult<AccountingAttachment?>(null);
        public Task<IReadOnlyCollection<AccountingAttachment>> GetAttachmentsAsync(Guid restaurantId, Guid entryId, CancellationToken cancellationToken = default) => Empty<AccountingAttachment>();
        private static Task<IReadOnlyCollection<T>> Empty<T>() => Task.FromResult<IReadOnlyCollection<T>>(Array.Empty<T>());
    }
}
