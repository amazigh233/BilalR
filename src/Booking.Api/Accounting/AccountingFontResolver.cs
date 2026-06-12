using PdfSharp.Fonts;

namespace Booking.Api.Accounting;

public sealed class AccountingFontResolver : IFontResolver
{
    private static readonly string[] RegularPaths =
    [
        @"C:\Windows\Fonts\arial.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        "/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf"
    ];

    private static readonly string[] BoldPaths =
    [
        @"C:\Windows\Fonts\arialbd.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
        "/usr/share/fonts/truetype/liberation2/LiberationSans-Bold.ttf"
    ];

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
        new(isBold ? "zambiq-bold" : "zambiq-regular");

    public byte[]? GetFont(string faceName)
    {
        var paths = faceName == "zambiq-bold" ? BoldPaths : RegularPaths;
        var path = paths.FirstOrDefault(File.Exists)
            ?? throw new InvalidOperationException("Geen bruikbaar PDF-lettertype gevonden op de server.");
        return File.ReadAllBytes(path);
    }
}
