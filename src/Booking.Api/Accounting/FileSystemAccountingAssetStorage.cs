using System.Security.Cryptography;

namespace Booking.Api.Accounting;

public sealed class FileSystemAccountingAssetStorage : IAccountingAssetStorage
{
    private static readonly IReadOnlyDictionary<string, string> AttachmentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg"
        };

    private readonly string _rootPath;

    public FileSystemAccountingAssetStorage(IConfiguration configuration)
    {
        _rootPath = Path.GetFullPath(configuration["Accounting:StoragePath"]
            ?? Path.Combine(AppContext.BaseDirectory, "accounting-assets"));
    }

    public Task<StoredAccountingAsset> SaveImportAsync(
        Guid restaurantId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default) =>
        SaveAsync(restaurantId, null, "imports", fileName, content, IAccountingAssetStorage.MaximumImportSize, import: true, cancellationToken);

    public Task<StoredAccountingAsset> SaveAttachmentAsync(
        Guid restaurantId,
        Guid entryId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default) =>
        SaveAsync(restaurantId, entryId, "attachments", fileName, content, IAccountingAssetStorage.MaximumAttachmentSize, import: false, cancellationToken);

    public Task<AccountingAsset> GetAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolveStorageKey(storageKey);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Accounting asset was not found.");
        }

        var contentType = ContentTypeFor(path, import: Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase));
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
        return Task.FromResult(new AccountingAsset(stream, Path.GetFileName(path), contentType, File.GetLastWriteTimeUtc(path)));
    }

    private async Task<StoredAccountingAsset> SaveAsync(
        Guid restaurantId,
        Guid? entryId,
        string folder,
        string fileName,
        Stream content,
        long maximumSize,
        bool import,
        CancellationToken cancellationToken)
    {
        if (restaurantId == Guid.Empty) throw new ArgumentException("Restaurant id is required.", nameof(restaurantId));
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName)) throw new ArgumentException("File name is required.", nameof(fileName));

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length == 0 || buffer.Length > maximumSize)
        {
            throw new ArgumentException($"Bestand moet tussen 1 byte en {maximumSize / 1024 / 1024} MB groot zijn.", nameof(content));
        }

        var bytes = buffer.ToArray();
        ValidateSignature(bytes, safeFileName, import);
        var checksum = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var extension = import ? ".csv" : NormalizeAttachmentExtension(bytes);
        var relativeDirectory = Path.Combine(
            restaurantId.ToString("N"),
            folder,
            entryId?.ToString("N") ?? DateTime.UtcNow.ToString("yyyyMM"));
        var directory = Path.Combine(_rootPath, relativeDirectory);
        Directory.CreateDirectory(directory);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(directory, storedFileName);
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);
        var storageKey = Path.Combine(relativeDirectory, storedFileName).Replace('\\', '/');
        return new StoredAccountingAsset(storageKey, safeFileName, ContentTypeFor(path, import), checksum, bytes.LongLength);
    }

    private string ResolveStorageKey(string storageKey)
    {
        var path = Path.GetFullPath(Path.Combine(_rootPath, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid accounting storage key.");
        }
        return path;
    }

    private static void ValidateSignature(byte[] bytes, string fileName, bool import)
    {
        if (import)
        {
            if (!Path.GetExtension(fileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Gebruik een CSV-bestand.", nameof(fileName));
            }
            return;
        }

        _ = NormalizeAttachmentExtension(bytes);
    }

    private static string NormalizeAttachmentExtension(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 5 && bytes[..5].SequenceEqual("%PDF-"u8)) return ".pdf";
        if (bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return ".png";
        if (bytes.Length >= 3 && bytes[..3].SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF })) return ".jpg";
        throw new ArgumentException("Gebruik een geldig PDF-, JPG- of PNG-bestand.");
    }

    private static string ContentTypeFor(string path, bool import) =>
        import ? "text/csv" : AttachmentTypes.GetValueOrDefault(Path.GetExtension(path), "application/octet-stream");
}
