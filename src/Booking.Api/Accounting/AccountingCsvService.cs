using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Booking.Application.Abstractions;
using Booking.Application.Accounting;
using Booking.Domain.Accounting;
using CsvHelper;
using CsvHelper.Configuration;

namespace Booking.Api.Accounting;

public sealed class AccountingCsvService(
    IAccountingRepository accountingRepository,
    IAccountingAssetStorage assetStorage)
{
    public async Task<AccountingImportResult> PreviewAsync(
        AccountingImportKind importKind,
        Stream content,
        AccountingImportMapping mapping,
        CancellationToken cancellationToken = default)
    {
        var bytes = await ReadBytesAsync(content, cancellationToken);
        var rows = Parse(importKind, bytes, mapping);
        return new AccountingImportResult(Checksum(bytes), rows.Count, rows.Count, 0, rows.Take(20).ToList());
    }

    public async Task<AccountingImportResult> ImportAsync(
        Guid restaurantId,
        AccountingImportKind importKind,
        string fileName,
        Stream content,
        AccountingImportMapping mapping,
        CancellationToken cancellationToken = default)
    {
        var bytes = await ReadBytesAsync(content, cancellationToken);
        var checksum = Checksum(bytes);
        if (await accountingRepository.ImportChecksumExistsAsync(restaurantId, checksum, cancellationToken))
        {
            throw new InvalidOperationException("Dit bestand is al geimporteerd.");
        }

        var rows = Parse(importKind, bytes, mapping);
        await using var storageStream = new MemoryStream(bytes);
        var stored = await assetStorage.SaveImportAsync(restaurantId, fileName, storageStream, cancellationToken);
        var batch = new AccountingImportBatch(
            restaurantId,
            importKind,
            fileName,
            checksum,
            JsonSerializer.Serialize(mapping),
            stored.StorageKey,
            rows.Count,
            DateTime.UtcNow);
        await accountingRepository.AddImportBatchAsync(batch, cancellationToken);

        var sourceKind = importKind == AccountingImportKind.Bank ? AccountingSourceKind.BankCsv : AccountingSourceKind.PosCsv;
        var candidates = rows.Select(row => new AccountingSourceTransaction(
            restaurantId,
            sourceKind,
            row.ExternalId,
            AccountingUseCases.Fingerprint(row.ExternalId, row.Date, row.Amount, "EUR"),
            row.Date,
            row.Description,
            row.Amount,
            "EUR",
            JsonSerializer.Serialize(row),
            DateTime.UtcNow,
            batch.Id)).ToList();
        var existing = await accountingRepository.GetExistingFingerprintsAsync(restaurantId, candidates.Select(item => item.Fingerprint), cancellationToken);
        var newRows = candidates.Where(item => !existing.Contains(item.Fingerprint)).ToList();
        if (newRows.Count > 0)
        {
            await accountingRepository.AddSourceTransactionsAsync(newRows, cancellationToken);
        }

        return new AccountingImportResult(checksum, rows.Count, newRows.Count, rows.Count - newRows.Count, rows.Take(20).ToList());
    }

    private static IReadOnlyList<AccountingImportPreviewRow> Parse(
        AccountingImportKind importKind,
        byte[] bytes,
        AccountingImportMapping mapping)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = mapping.Delimiter.ToString(),
            BadDataFound = null,
            MissingFieldFound = null,
            HeaderValidated = null,
            TrimOptions = TrimOptions.Trim
        };
        using var csv = new CsvReader(reader, configuration);
        csv.Read();
        csv.ReadHeader();
        var rows = new List<AccountingImportPreviewRow>();
        var rowNumber = 1;
        while (csv.Read())
        {
            rowNumber++;
            var dateText = Required(csv, mapping.DateColumn);
            if (!DateOnly.TryParseExact(dateText, mapping.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
                !DateOnly.TryParse(dateText, CultureInfo.GetCultureInfo("nl-NL"), DateTimeStyles.None, out date))
            {
                throw new ArgumentException($"Ongeldige datum op CSV-regel {rowNumber}.");
            }

            var description = Optional(csv, mapping.DescriptionColumn) ?? $"{importKind} import regel {rowNumber}";
            var reference = Optional(csv, mapping.ReferenceColumn);
            decimal amount;
            if (importKind == AccountingImportKind.Pos)
            {
                amount = ParseOptionalDecimal(csv, mapping.Vat0Column) +
                         ParseOptionalDecimal(csv, mapping.Vat9Column) +
                         ParseOptionalDecimal(csv, mapping.Vat21Column);
            }
            else if (!string.IsNullOrWhiteSpace(mapping.AmountColumn))
            {
                amount = ParseDecimal(Required(csv, mapping.AmountColumn), rowNumber);
            }
            else
            {
                amount = ParseOptionalDecimal(csv, mapping.CreditColumn) - ParseOptionalDecimal(csv, mapping.DebitColumn);
            }

            var externalId = string.IsNullOrWhiteSpace(reference)
                ? $"{importKind}:{date:yyyyMMdd}:{rowNumber}:{amount.ToString("0.00", CultureInfo.InvariantCulture)}"
                : reference.Trim();
            rows.Add(new AccountingImportPreviewRow(rowNumber, date, description, amount, externalId));
        }
        return rows;
    }

    private static string Required(CsvReader csv, string column) =>
        Optional(csv, column) ?? throw new ArgumentException($"CSV-kolom '{column}' ontbreekt of is leeg.");

    private static string? Optional(CsvReader csv, string? column) =>
        string.IsNullOrWhiteSpace(column) || !csv.TryGetField<string>(column, out var value) || string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    private static decimal ParseOptionalDecimal(CsvReader csv, string? column) =>
        Optional(csv, column) is { } value ? ParseDecimal(value, csv.Parser.Row) : 0m;

    private static decimal ParseDecimal(string value, int row)
    {
        if (decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.GetCultureInfo("nl-NL"), out var amount) ||
            decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out amount))
        {
            return amount;
        }
        throw new ArgumentException($"Ongeldig bedrag op CSV-regel {row}.");
    }

    private static async Task<byte[]> ReadBytesAsync(Stream content, CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length == 0 || buffer.Length > IAccountingAssetStorage.MaximumImportSize)
        {
            throw new ArgumentException("CSV-bestand moet tussen 1 byte en 10 MB groot zijn.");
        }
        return buffer.ToArray();
    }

    private static string Checksum(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
