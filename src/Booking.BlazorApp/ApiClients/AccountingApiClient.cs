using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace Booking.BlazorApp.ApiClients;

public sealed class AccountingApiClient(
    HttpClient httpClient,
    AuthenticationStateProvider authenticationStateProvider)
    : BookingApiClientBase(httpClient, authenticationStateProvider)
{
    public Task<AccountingSummaryDto> GetSummaryAsync(DateOnly from, DateOnly to, CancellationToken token = default) => GetAsync<AccountingSummaryDto>($"api/admin/accounting/summary?from={Date(from)}&to={Date(to)}", token);
    public Task<IReadOnlyCollection<AccountingEntryDto>> GetEntriesAsync(CancellationToken token = default) => GetAsync<IReadOnlyCollection<AccountingEntryDto>>("api/admin/accounting/entries", token);
    public Task<IReadOnlyCollection<AccountingCategoryDto>> GetCategoriesAsync(CancellationToken token = default) => GetAsync<IReadOnlyCollection<AccountingCategoryDto>>("api/admin/accounting/categories", token);
    public Task<IReadOnlyCollection<AccountingSourceTransactionDto>> GetReviewItemsAsync(CancellationToken token = default) => GetAsync<IReadOnlyCollection<AccountingSourceTransactionDto>>("api/admin/accounting/review-items", token);
    public Task<IReadOnlyCollection<AccountingConnectionDto>> GetConnectionsAsync(CancellationToken token = default) => GetAsync<IReadOnlyCollection<AccountingConnectionDto>>("api/admin/accounting/connections", token);
    public Task<IReadOnlyCollection<AccountingImportProfileDto>> GetImportProfilesAsync(CancellationToken token = default) => GetAsync<IReadOnlyCollection<AccountingImportProfileDto>>("api/admin/accounting/imports/profiles", token);
    public Task<AccountingConnectorAvailabilityDto> GetConnectorAvailabilityAsync(CancellationToken token = default) => GetAsync<AccountingConnectorAvailabilityDto>("api/admin/accounting/connections/availability", token);
    public Task<IReadOnlyCollection<AccountingInstitutionDto>> GetGoCardlessInstitutionsAsync(CancellationToken token = default) => GetAsync<IReadOnlyCollection<AccountingInstitutionDto>>("api/admin/accounting/connections/gocardless/institutions", token);
    public Task<AccountingEntryDto> CreateEntryAsync(SaveAccountingEntryRequest request, CancellationToken token = default) => PostAsync<SaveAccountingEntryRequest, AccountingEntryDto>("api/admin/accounting/entries", request, token);
    public Task<AccountingEntryDto> ConfirmEntryAsync(Guid id, CancellationToken token = default) => PostWithoutBodyAsync<AccountingEntryDto>($"api/admin/accounting/entries/{id}/confirm", token);
    public Task<AccountingEntryDto> CorrectEntryAsync(Guid id, SaveAccountingEntryRequest request, CancellationToken token = default) => PostAsync<SaveAccountingEntryRequest, AccountingEntryDto>($"api/admin/accounting/entries/{id}/correct", request, token);
    public Task<AccountingEntryDto> ReviewSourceAsync(Guid id, SaveAccountingEntryRequest request, CancellationToken token = default) => PostAsync<SaveAccountingEntryRequest, AccountingEntryDto>($"api/admin/accounting/review-items/{id}/confirm", request, token);
    public Task<AccountingCategoryDto> AddCategoryAsync(string name, AccountingEntryType type, CancellationToken token = default) => PostAsync<object, AccountingCategoryDto>("api/admin/accounting/categories", new { name, entryType = type }, token);
    public Task<int> SyncDeliveryAsync(CancellationToken token = default) => PostWithoutBodyAsync<int>("api/admin/accounting/sync-delivery", token);
    public Task<AccountingConnectionStartDto> StartMollieAsync(CancellationToken token = default) => PostWithoutBodyAsync<AccountingConnectionStartDto>("api/admin/accounting/connections/mollie/start", token);
    public Task<AccountingConnectionStartDto> StartGoCardlessAsync(string institutionId, CancellationToken token = default) => PostAsync<object, AccountingConnectionStartDto>("api/admin/accounting/connections/gocardless/start", new { institutionId }, token);
    public Task<int> SyncConnectionAsync(AccountingConnectionProvider provider, string externalId, CancellationToken token = default) => PostWithoutBodyAsync<int>($"api/admin/accounting/connections/{provider}/{Uri.EscapeDataString(externalId)}/sync", token);

    public async Task CompleteGoCardlessAsync(string requisitionId, CancellationToken token = default)
    {
        using var response = await SendAsync(ct => HttpClient.PostAsJsonAsync("api/admin/accounting/connections/gocardless/complete", new { requisitionId }, JsonOptions, ct), token);
        await EnsureSuccessAsync(response, token);
    }

    public async Task CompleteMollieAsync(string state, string code, CancellationToken token = default)
    {
        using var response = await SendAsync(ct => HttpClient.PostAsJsonAsync("api/admin/accounting/connections/mollie/complete", new { state, code }, JsonOptions, ct), token);
        await EnsureSuccessAsync(response, token);
    }

    public async Task DisableConnectionAsync(AccountingConnectionProvider provider, string externalId, CancellationToken token = default)
    {
        using var response = await SendAsync(ct => HttpClient.DeleteAsync($"api/admin/accounting/connections/{provider}/{Uri.EscapeDataString(externalId)}", ct), token);
        await EnsureSuccessAsync(response, token);
    }

    public async Task IgnoreSourceAsync(Guid id, CancellationToken token = default)
    {
        using var response = await SendAsync(ct => HttpClient.PostAsync($"api/admin/accounting/review-items/{id}/ignore", null, ct), token);
        await EnsureSuccessAsync(response, token);
    }

    public async Task MatchSourcesAsync(Guid bankSourceTransactionId, Guid matchedSourceTransactionId, CancellationToken token = default)
    {
        using var response = await SendAsync(ct => HttpClient.PostAsJsonAsync("api/admin/accounting/matches", new { bankSourceTransactionId, matchedSourceTransactionId }, JsonOptions, ct), token);
        await EnsureSuccessAsync(response, token);
    }

    public async Task SetCategoryActiveAsync(Guid categoryId, bool active, CancellationToken token = default)
    {
        using var response = await SendAsync(ct => HttpClient.PatchAsJsonAsync($"api/admin/accounting/categories/{categoryId}", new { active }, JsonOptions, ct), token);
        await EnsureSuccessAsync(response, token);
    }

    public Task<AccountingImportResultDto> PreviewImportAsync(AccountingImportKind kind, AccountingImportMapping mapping, Stream content, string fileName, CancellationToken token = default) =>
        UploadImportAsync("api/admin/accounting/imports/preview", kind, mapping, content, fileName, token);

    public Task<AccountingImportResultDto> ImportAsync(AccountingImportKind kind, AccountingImportMapping mapping, Stream content, string fileName, CancellationToken token = default) =>
        UploadImportAsync("api/admin/accounting/imports", kind, mapping, content, fileName, token);

    public async Task<AccountingAttachmentDto> UploadAttachmentAsync(Guid entryId, Stream content, string fileName, CancellationToken token = default)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(content);
        form.Add(fileContent, "file", fileName);
        using var response = await SendAsync(ct => HttpClient.PostAsync($"api/admin/accounting/entries/{entryId}/attachments", form, ct), token);
        return await ReadResponseAsync<AccountingAttachmentDto>(response, token);
    }

    public async Task<byte[]> DownloadExportAsync(string format, DateOnly from, DateOnly to, CancellationToken token = default)
    {
        var path = format == "pdf" ? "summary.pdf" : "entries.csv";
        using var response = await SendAsync(ct => HttpClient.GetAsync($"api/admin/accounting/exports/{path}?from={Date(from)}&to={Date(to)}", ct), token);
        await EnsureSuccessAsync(response, token);
        return await response.Content.ReadAsByteArrayAsync(token);
    }

    private async Task<AccountingImportResultDto> UploadImportAsync(string url, AccountingImportKind kind, AccountingImportMapping mapping, Stream content, string fileName, CancellationToken token)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(kind.ToString()), "kind");
        form.Add(new StringContent(JsonSerializer.Serialize(mapping, JsonOptions)), "mappingJson");
        using var fileContent = new StreamContent(content);
        form.Add(fileContent, "file", fileName);
        using var response = await SendAsync(ct => HttpClient.PostAsync(url, form, ct), token);
        return await ReadResponseAsync<AccountingImportResultDto>(response, token);
    }

    private async Task<T> GetAsync<T>(string url, CancellationToken token)
    {
        using var response = await SendAsync(ct => HttpClient.GetAsync(url, ct), token);
        return await ReadResponseAsync<T>(response, token);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest request, CancellationToken token)
    {
        using var response = await SendAsync(ct => HttpClient.PostAsJsonAsync(url, request, JsonOptions, ct), token);
        return await ReadResponseAsync<TResponse>(response, token);
    }

    private async Task<TResponse> PostWithoutBodyAsync<TResponse>(string url, CancellationToken token)
    {
        using var response = await SendAsync(ct => HttpClient.PostAsync(url, null, ct), token);
        return await ReadResponseAsync<TResponse>(response, token);
    }

    private static string Date(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
