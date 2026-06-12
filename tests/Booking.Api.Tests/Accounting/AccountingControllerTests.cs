using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Booking.Api.Contracts.Accounting;
using Booking.Api.Tests.Support;
using Booking.Application.Accounting;
using Booking.Domain.Accounting;
using Booking.Infrastructure.Identity;

namespace Booking.Api.Tests.Accounting;

public sealed class AccountingControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task AccountingEndpoints_AreOwnerOnly()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();
        var staff = await factory.SeedUserAsync(BookingRoles.Staff);
        await AuthenticateAsync(client, factory, staff.User.Email);

        var response = await client.GetAsync("api/admin/accounting/summary");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateConfirmAndSummary_AreTenantScoped()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();
        var ownerA = await factory.SeedUserAsync(BookingRoles.Owner);
        var ownerB = await factory.SeedUserAsync(BookingRoles.Owner);
        await AuthenticateAsync(client, factory, ownerA.User.Email);

        var create = await client.PostAsJsonAsync(
            "api/admin/accounting/entries",
            Request(109m),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var entry = await create.Content.ReadFromJsonAsync<AccountingEntryResponse>(JsonOptions);
        Assert.NotNull(entry);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"api/admin/accounting/entries/{entry!.Id}/confirm", null)).StatusCode);

        var summary = await client.GetFromJsonAsync<AccountingSummaryResponse>("api/admin/accounting/summary", JsonOptions);
        Assert.Equal(109m, summary!.Revenue);

        await AuthenticateAsync(client, factory, ownerB.User.Email);
        var otherSummary = await client.GetFromJsonAsync<AccountingSummaryResponse>("api/admin/accounting/summary", JsonOptions);
        Assert.Equal(0m, otherSummary!.Revenue);
    }

    [Fact]
    public async Task CsvImport_IsDeduplicated_AndCreatesReviewItems()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();
        var owner = await factory.SeedUserAsync(BookingRoles.Owner);
        await AuthenticateAsync(client, factory, owner.User.Email);
        const string csv = "Datum,Omschrijving,Referentie,Bedrag\n2026-06-01,Leverancier,abc,-25.00\n";
        var mapping = new AccountingImportMapping("Datum", "Omschrijving", "Bedrag", null, null, "Referentie", null, null, null);

        using var form = ImportForm(csv, mapping);
        var response = await client.PostAsync("api/admin/accounting/imports", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AccountingImportResult>(JsonOptions);
        Assert.Equal(1, result!.ImportedRows);
        var reviewItems = await client.GetFromJsonAsync<IReadOnlyCollection<AccountingSourceTransactionResponse>>("api/admin/accounting/review-items", JsonOptions);
        Assert.Single(reviewItems!);
        var profiles = await client.GetFromJsonAsync<IReadOnlyCollection<AccountingImportProfileApiResponse>>("api/admin/accounting/imports/profiles", JsonOptions);
        var profile = Assert.Single(profiles!);
        Assert.Equal("bank.csv", profile.Name);
        Assert.Equal(mapping, profile.Mapping);

        using var duplicateForm = ImportForm(csv, mapping);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("api/admin/accounting/imports", duplicateForm)).StatusCode);
    }

    [Fact]
    public async Task Attachment_IsPrivateToOwningRestaurant()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();
        var ownerA = await factory.SeedUserAsync(BookingRoles.Owner);
        var ownerB = await factory.SeedUserAsync(BookingRoles.Owner);
        await AuthenticateAsync(client, factory, ownerA.User.Email);
        var entry = await CreateEntryAsync(client);

        using var upload = new MultipartFormDataContent();
        upload.Add(new ByteArrayContent("%PDF-1.4 test"u8.ToArray()), "file", "bon.pdf");
        var uploaded = await client.PostAsync($"api/admin/accounting/entries/{entry.Id}/attachments", upload);
        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        var attachment = await uploaded.Content.ReadFromJsonAsync<AccountingAttachmentApiResponse>(JsonOptions);

        await AuthenticateAsync(client, factory, ownerB.User.Email);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"api/admin/accounting/attachments/{attachment!.Id}")).StatusCode);
    }

    [Fact]
    public async Task Exports_ReturnCsvAndPdf()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();
        var owner = await factory.SeedUserAsync(BookingRoles.Owner);
        await AuthenticateAsync(client, factory, owner.User.Email);
        var entry = await CreateEntryAsync(client);
        await client.PostAsync($"api/admin/accounting/entries/{entry.Id}/confirm", null);
        var from = DateOnly.FromDateTime(DateTime.Today).AddDays(-1).ToString("yyyy-MM-dd");
        var to = DateOnly.FromDateTime(DateTime.Today).AddDays(1).ToString("yyyy-MM-dd");

        var csv = await client.GetAsync($"api/admin/accounting/exports/entries.csv?from={from}&to={to}");
        var pdf = await client.GetAsync($"api/admin/accounting/exports/summary.pdf?from={from}&to={to}");

        Assert.Equal(HttpStatusCode.OK, csv.StatusCode);
        Assert.Contains("BoekingId", await csv.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString((await pdf.Content.ReadAsByteArrayAsync())[..4]));
    }

    private static SaveAccountingEntryApiRequest Request(decimal amount) =>
        new(AccountingEntryType.Revenue, DateOnly.FromDateTime(DateTime.Today), "Omzet", [new AccountingSplitRequest(null, 9, amount, 0)]);

    private static async Task<AccountingEntryResponse> CreateEntryAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("api/admin/accounting/entries", Request(109m), JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AccountingEntryResponse>(JsonOptions))!;
    }

    private static MultipartFormDataContent ImportForm(string csv, AccountingImportMapping mapping)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(AccountingImportKind.Bank.ToString()), "kind");
        form.Add(new StringContent(JsonSerializer.Serialize(mapping, JsonOptions)), "mappingJson");
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", "bank.csv");
        return form;
    }

    private static async Task AuthenticateAsync(HttpClient client, BookingApiFactory factory, string? email)
    {
        var token = await factory.LoginAsync(client, email ?? string.Empty);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
