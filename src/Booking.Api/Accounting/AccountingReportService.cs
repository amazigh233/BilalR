using System.Globalization;
using System.Text;
using Booking.Application.Accounting;
using CsvHelper;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace Booking.Api.Accounting;

public sealed class AccountingReportService(AccountingUseCases accountingUseCases)
{
    private static readonly object FontLock = new();

    public async Task<byte[]> CreateCsvAsync(Guid restaurantId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        var entries = await accountingUseCases.GetEntriesAsync(restaurantId, fromDate, toDate, cancellationToken);
        await using var stream = new MemoryStream();
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true);
        await using var csv = new CsvWriter(writer, CultureInfo.GetCultureInfo("nl-NL"));
        csv.WriteField("BoekingId");
        csv.WriteField("Datum");
        csv.WriteField("Type");
        csv.WriteField("Status");
        csv.WriteField("Omschrijving");
        csv.WriteField("CategorieId");
        csv.WriteField("BtwTarief");
        csv.WriteField("Bruto");
        csv.WriteField("Netto");
        csv.WriteField("Btw");
        csv.WriteField("AftrekbareBtw");
        csv.WriteField("BronTransactieId");
        csv.WriteField("CorrectieVan");
        await csv.NextRecordAsync();
        foreach (var entry in entries.Where(item => item.Status != Domain.Accounting.AccountingEntryStatus.Draft))
        {
            foreach (var split in entry.Splits)
            {
                csv.WriteField(entry.Id);
                csv.WriteField(entry.EntryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                csv.WriteField(entry.EntryType);
                csv.WriteField(entry.Status);
                csv.WriteField(entry.Description);
                csv.WriteField(split.CategoryId);
                csv.WriteField(split.VatRate);
                csv.WriteField(split.GrossAmount);
                csv.WriteField(split.NetAmount);
                csv.WriteField(split.VatAmount);
                csv.WriteField(split.DeductibleVatAmount);
                csv.WriteField(entry.SourceTransactionId);
                csv.WriteField(entry.CorrectionOfEntryId);
                await csv.NextRecordAsync();
            }
        }
        await writer.FlushAsync(cancellationToken);
        return stream.ToArray();
    }

    public async Task<byte[]> CreatePdfAsync(Guid restaurantId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        EnsureFontResolver();
        var summary = await accountingUseCases.GetSummaryAsync(restaurantId, fromDate, toDate, cancellationToken);
        using var document = new PdfDocument();
        document.Info.Title = $"Zambiq boekhoudoverzicht {fromDate:yyyy-MM-dd} - {toDate:yyyy-MM-dd}";
        var page = document.AddPage();
        using var graphics = XGraphics.FromPdfPage(page);
        var title = new XFont("Arial", 18, XFontStyleEx.Bold);
        var heading = new XFont("Arial", 11, XFontStyleEx.Bold);
        var body = new XFont("Arial", 10);
        var y = 42d;
        graphics.DrawString("Zambiq boekhouding-light", title, XBrushes.DarkSlateGray, 40, y);
        y += 24;
        graphics.DrawString($"Periode {fromDate:dd-MM-yyyy} tot en met {toDate:dd-MM-yyyy}", body, XBrushes.Black, 40, y);
        y += 30;
        DrawMoney(graphics, heading, body, "Omzet", summary.Revenue, ref y);
        DrawMoney(graphics, heading, body, "Kosten", summary.Expenses, ref y);
        DrawMoney(graphics, heading, body, "Resultaat", summary.Result, ref y);
        DrawMoney(graphics, heading, body, "Te betalen btw", summary.VatDue, ref y);
        DrawMoney(graphics, heading, body, "Aftrekbare voorbelasting", summary.DeductibleVat, ref y);
        DrawMoney(graphics, heading, body, "Btw-saldo", summary.VatBalance, ref y);
        y += 18;
        graphics.DrawString("Concept-btw-overzicht voor controle door ondernemer of boekhouder.", heading, XBrushes.DarkRed, 40, y);
        y += 26;
        foreach (var vat in summary.Vat)
        {
            graphics.DrawString($"{vat.VatRate}% btw: omzet {Money(vat.RevenueGross)}, verschuldigd {Money(vat.VatDue)}, voorbelasting {Money(vat.DeductibleVat)}", body, XBrushes.Black, 40, y);
            y += 16;
        }
        await using var output = new MemoryStream();
        document.Save(output, closeStream: false);
        return output.ToArray();
    }

    private static void DrawMoney(XGraphics graphics, XFont heading, XFont body, string label, decimal amount, ref double y)
    {
        graphics.DrawString(label, heading, XBrushes.Black, 40, y);
        graphics.DrawString(Money(amount), body, XBrushes.Black, 220, y);
        y += 18;
    }

    private static string Money(decimal amount) => amount.ToString("C", CultureInfo.GetCultureInfo("nl-NL"));

    private static void EnsureFontResolver()
    {
        if (GlobalFontSettings.FontResolver is not null)
        {
            return;
        }

        lock (FontLock)
        {
            GlobalFontSettings.FontResolver ??= new AccountingFontResolver();
        }
    }
}
