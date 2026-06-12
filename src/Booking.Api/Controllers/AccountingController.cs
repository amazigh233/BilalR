using System.Text.Json;
using System.Text.Json.Serialization;
using Booking.Api.Accounting;
using Booking.Api.Contracts.Accounting;
using Booking.Application.Abstractions;
using Booking.Application.Accounting;
using Booking.Domain.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

[Route("api/admin/accounting")]
[Authorize(Policy = "RestaurantOwner")]
public sealed class AccountingController(
    AccountingUseCases accountingUseCases,
    AccountingCsvService csvService,
    AccountingReportService reportService,
    AccountingConnectorService connectorService,
    IAccountingRepository accountingRepository,
    IAccountingAssetStorage assetStorage) : ApiControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [HttpGet("summary")]
    public async Task<ActionResult<AccountingSummaryResponse>> GetSummary([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        var toDate = to ?? DateOnly.FromDateTime(DateTime.Now);
        var fromDate = from ?? new DateOnly(toDate.Year, toDate.Month, 1);
        return await HandleAsync(() => accountingUseCases.GetSummaryAsync(restaurantId, fromDate, toDate, cancellationToken));
    }

    [HttpGet("entries")]
    public async Task<ActionResult<IReadOnlyCollection<AccountingEntryResponse>>> GetEntries([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        return Ok(await accountingUseCases.GetEntriesAsync(restaurantId, from, to, cancellationToken));
    }

    [HttpPost("entries")]
    public async Task<ActionResult<AccountingEntryResponse>> CreateEntry([FromBody] SaveAccountingEntryApiRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        return await HandleAsync(() => accountingUseCases.CreateDraftAsync(ToApplicationRequest(restaurantId, request), cancellationToken));
    }

    [HttpPut("entries/{entryId:guid}")]
    public async Task<ActionResult<AccountingEntryResponse>> UpdateEntry(Guid entryId, [FromBody] SaveAccountingEntryApiRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        return await HandleAsync(() => accountingUseCases.UpdateDraftAsync(restaurantId, entryId, ToApplicationRequest(restaurantId, request), cancellationToken));
    }

    [HttpPost("entries/{entryId:guid}/confirm")]
    public async Task<ActionResult<AccountingEntryResponse>> ConfirmEntry(Guid entryId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        return await HandleAsync(() => accountingUseCases.ConfirmAsync(restaurantId, entryId, cancellationToken));
    }

    [HttpPost("entries/{entryId:guid}/correct")]
    public async Task<ActionResult<AccountingEntryResponse>> CorrectEntry(Guid entryId, [FromBody] SaveAccountingEntryApiRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        return await HandleAsync(() => accountingUseCases.CorrectAsync(
            new CorrectAccountingEntryRequest(restaurantId, entryId, request.EntryType, request.EntryDate, request.Description, request.Splits),
            cancellationToken));
    }

    [HttpGet("review-items")]
    public async Task<ActionResult<IReadOnlyCollection<AccountingSourceTransactionResponse>>> GetReviewItems(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        return Ok(await accountingUseCases.GetReviewItemsAsync(restaurantId, cancellationToken));
    }

    [HttpPost("review-items/{sourceId:guid}/confirm")]
    public async Task<ActionResult<AccountingEntryResponse>> ReviewSource(Guid sourceId, [FromBody] SaveAccountingEntryApiRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        return await HandleAsync(() => accountingUseCases.ReviewSourceAsync(
            new SaveAccountingEntryRequest(restaurantId, request.EntryType, request.EntryDate, request.Description, request.Splits, sourceId),
            cancellationToken));
    }

    [HttpPost("review-items/{sourceId:guid}/ignore")]
    public async Task<ActionResult> IgnoreSource(Guid sourceId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        return await HandleNoContentAsync(() => accountingUseCases.IgnoreSourceAsync(restaurantId, sourceId, cancellationToken));
    }

    [HttpPost("matches")]
    public async Task<ActionResult> MatchSources([FromBody] MatchAccountingSourcesApiRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        return await HandleNoContentAsync(() => accountingUseCases.MatchSourcesAsync(restaurantId, request.BankSourceTransactionId, request.MatchedSourceTransactionId, cancellationToken));
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyCollection<AccountingCategoryResponse>>> GetCategories(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        return Ok(await accountingUseCases.GetCategoriesAsync(restaurantId, cancellationToken));
    }

    [HttpPost("categories")]
    public async Task<ActionResult<AccountingCategoryResponse>> AddCategory([FromBody] AddAccountingCategoryApiRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        return await HandleAsync(() => accountingUseCases.AddCategoryAsync(restaurantId, request.Name, request.EntryType, cancellationToken));
    }

    [HttpPatch("categories/{categoryId:guid}")]
    public async Task<ActionResult<AccountingCategoryResponse>> SetCategoryActive(Guid categoryId, [FromBody] SetAccountingCategoryActiveApiRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        return await HandleAsync(() => accountingUseCases.SetCategoryActiveAsync(restaurantId, categoryId, request.Active, cancellationToken));
    }

    [HttpPost("imports/preview")]
    [RequestSizeLimit(10_500_000)]
    public async Task<ActionResult<AccountingImportResult>> PreviewImport([FromForm] AccountingImportKind kind, [FromForm] string mappingJson, [FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out _)) return Forbid();
        if (file is null) return BadRequest(ToError("Kies een CSV-bestand."));
        if (file.Length > IAccountingAssetStorage.MaximumImportSize) return BadRequest(ToError("CSV-bestand mag maximaal 10 MB groot zijn."));
        return await HandleAsync(async () =>
        {
            await using var content = file.OpenReadStream();
            return await csvService.PreviewAsync(kind, content, ParseMapping(mappingJson), cancellationToken);
        });
    }

    [HttpPost("imports")]
    [RequestSizeLimit(10_500_000)]
    public async Task<ActionResult<AccountingImportResult>> Import([FromForm] AccountingImportKind kind, [FromForm] string mappingJson, [FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        if (file is null) return BadRequest(ToError("Kies een CSV-bestand."));
        if (file.Length > IAccountingAssetStorage.MaximumImportSize) return BadRequest(ToError("CSV-bestand mag maximaal 10 MB groot zijn."));
        return await HandleAsync(async () =>
        {
            await using var content = file.OpenReadStream();
            return await csvService.ImportAsync(restaurantId, kind, file.FileName, content, ParseMapping(mappingJson), cancellationToken);
        });
    }

    [HttpGet("imports")]
    public async Task<ActionResult<IReadOnlyCollection<AccountingImportBatchResponse>>> GetImports(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        var batches = await accountingRepository.GetImportBatchesAsync(restaurantId, cancellationToken);
        return Ok(batches.Select(batch => new AccountingImportBatchResponse(batch.Id, batch.ImportKind, batch.FileName, batch.RowCount, batch.CreatedAtUtc)));
    }

    [HttpGet("imports/profiles")]
    public async Task<ActionResult<IReadOnlyCollection<AccountingImportProfileApiResponse>>> GetImportProfiles(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        var batches = await accountingRepository.GetImportBatchesAsync(restaurantId, cancellationToken);
        var profiles = batches
            .GroupBy(batch => new { batch.ImportKind, batch.MappingJson })
            .Select(group => group.OrderByDescending(batch => batch.CreatedAtUtc).First())
            .Select(batch => new AccountingImportProfileApiResponse(
                batch.Id,
                batch.FileName,
                batch.ImportKind,
                ParseMapping(batch.MappingJson),
                batch.CreatedAtUtc))
            .ToList();
        return Ok(profiles);
    }

    [HttpPost("sync-delivery")]
    public async Task<ActionResult<int>> SyncDelivery(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        return Ok(await accountingUseCases.SyncDeliverySalesAsync(restaurantId, cancellationToken));
    }

    [HttpGet("exports/entries.csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        var bytes = await reportService.CreateCsvAsync(restaurantId, from, to, cancellationToken);
        return File(bytes, "text/csv; charset=utf-8", $"zambiq-boekingen-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
    }

    [HttpGet("exports/summary.pdf")]
    public async Task<IActionResult> ExportPdf([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        var bytes = await reportService.CreatePdfAsync(restaurantId, from, to, cancellationToken);
        return File(bytes, "application/pdf", $"zambiq-boekhoudoverzicht-{from:yyyyMMdd}-{to:yyyyMMdd}.pdf");
    }

    [HttpPost("entries/{entryId:guid}/attachments")]
    [RequestSizeLimit(10_500_000)]
    public async Task<ActionResult<AccountingAttachmentApiResponse>> UploadAttachment(Guid entryId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        if (file is null) return BadRequest(ToError("Kies een bewijsstuk."));
        if (file.Length > IAccountingAssetStorage.MaximumAttachmentSize) return BadRequest(ToError("Bewijsstuk mag maximaal 10 MB groot zijn."));
        return await HandleAsync(async () =>
        {
            _ = await accountingRepository.GetEntryAsync(restaurantId, entryId, cancellationToken)
                ?? throw new KeyNotFoundException("Accounting entry was not found.");
            await using var content = file.OpenReadStream();
            var stored = await assetStorage.SaveAttachmentAsync(restaurantId, entryId, file.FileName, content, cancellationToken);
            var attachment = new AccountingAttachment(restaurantId, entryId, stored.FileName, stored.ContentType, stored.StorageKey, stored.Checksum, stored.Length, DateTime.UtcNow);
            await accountingRepository.AddAttachmentAsync(attachment, cancellationToken);
            return ToAttachmentResponse(attachment);
        });
    }

    [HttpGet("entries/{entryId:guid}/attachments")]
    public async Task<ActionResult<IReadOnlyCollection<AccountingAttachmentApiResponse>>> GetAttachments(Guid entryId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        var attachments = await accountingRepository.GetAttachmentsAsync(restaurantId, entryId, cancellationToken);
        return Ok(attachments.Select(ToAttachmentResponse));
    }

    [HttpGet("attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(Guid attachmentId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        var attachment = await accountingRepository.GetAttachmentAsync(restaurantId, attachmentId, cancellationToken);
        if (attachment is null) return NotFound(ToError("Bewijsstuk niet gevonden."));
        var asset = await assetStorage.GetAsync(attachment.StorageKey, cancellationToken);
        return File(asset.Content, attachment.ContentType, attachment.FileName, enableRangeProcessing: true);
    }

    [HttpGet("connections/availability")]
    public ActionResult<AccountingConnectorAvailabilityApiResponse> GetConnectorAvailability() =>
        Ok(new AccountingConnectorAvailabilityApiResponse(
            connectorService.IsConfigured(AccountingConnectionProvider.GoCardless),
            connectorService.IsConfigured(AccountingConnectionProvider.Mollie)));

    [HttpGet("connections")]
    public async Task<ActionResult<IReadOnlyCollection<AccountingConnectionResponse>>> GetConnections(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        var connections = await accountingRepository.GetConnectionsAsync(restaurantId, cancellationToken);
        return Ok(connections.Select(connection => new AccountingConnectionResponse(connection.Id, connection.Provider, connection.Status, connection.ExternalId, connection.LastSyncedAtUtc, connection.LastError)));
    }

    [HttpGet("connections/gocardless/institutions")]
    public async Task<ActionResult<IReadOnlyCollection<AccountingInstitutionApiResponse>>> GetInstitutions(CancellationToken cancellationToken) =>
        await HandleAsync<IReadOnlyCollection<AccountingInstitutionApiResponse>>(async () =>
            (await connectorService.GetGoCardlessInstitutionsAsync(cancellationToken))
                .Select(item => new AccountingInstitutionApiResponse(item.Id, item.Name))
                .ToList());

    [HttpPost("connections/gocardless/start")]
    public async Task<ActionResult<AccountingConnectionStartResponse>> StartGoCardless([FromBody] StartGoCardlessApiRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        return await HandleAsync(() => connectorService.StartGoCardlessAsync(restaurantId, request.InstitutionId, cancellationToken));
    }

    [HttpPost("connections/gocardless/complete")]
    public async Task<ActionResult> CompleteGoCardless([FromBody] CompleteGoCardlessApiRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        return await HandleNoContentAsync(() => connectorService.CompleteGoCardlessAsync(restaurantId, request.RequisitionId, cancellationToken));
    }

    [HttpPost("connections/mollie/start")]
    public async Task<ActionResult<AccountingConnectionStartResponse>> StartMollie(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        return await HandleAsync(() => connectorService.StartMollieAsync(restaurantId, cancellationToken));
    }

    [HttpPost("connections/mollie/complete")]
    public async Task<ActionResult> CompleteMollie([FromBody] CompleteMollieApiRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        return await HandleNoContentAsync(() => connectorService.CompleteMollieAsync(restaurantId, request.State, request.Code, cancellationToken));
    }

    [HttpPost("connections/{provider}/{externalId}/sync")]
    public async Task<ActionResult<int>> SyncConnection(AccountingConnectionProvider provider, string externalId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        return await HandleAsync(() => connectorService.SyncAsync(restaurantId, provider, externalId, cancellationToken));
    }

    [HttpDelete("connections/{provider}/{externalId}")]
    public async Task<ActionResult> DisableConnection(AccountingConnectionProvider provider, string externalId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();
        return await HandleNoContentAsync(() => connectorService.DisableAsync(restaurantId, provider, externalId, cancellationToken));
    }

    private static SaveAccountingEntryRequest ToApplicationRequest(Guid restaurantId, SaveAccountingEntryApiRequest request) =>
        new(restaurantId, request.EntryType, request.EntryDate, request.Description, request.Splits);

    private static AccountingImportMapping ParseMapping(string mappingJson) =>
        JsonSerializer.Deserialize<AccountingImportMapping>(mappingJson, JsonOptions)
        ?? throw new ArgumentException("Ongeldige CSV-mapping.");

    private static AccountingAttachmentApiResponse ToAttachmentResponse(AccountingAttachment attachment) =>
        new(attachment.Id, attachment.AccountingEntryId, attachment.FileName, attachment.ContentType, attachment.Length, attachment.CreatedAtUtc);

    private async Task<ActionResult<T>> HandleAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException or HttpRequestException)
        {
            return HandleKnownException(exception);
        }
    }

    private async Task<ActionResult> HandleNoContentAsync(Func<Task> action)
    {
        try
        {
            await action();
            return NoContent();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException or HttpRequestException)
        {
            return HandleKnownException(exception);
        }
    }
}
