namespace Booking.Api.WidgetAssets;

public sealed class FileSystemWidgetLogoStorage : IWidgetLogoStorage
{
    private const string LogoFilePrefix = "logo.";
    private static readonly IReadOnlyDictionary<string, string> ContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".webp"] = "image/webp"
        };

    private readonly string _storagePath;
    private readonly string _publicBaseUrl;

    public FileSystemWidgetLogoStorage(IConfiguration configuration)
    {
        _storagePath = configuration["WidgetAssets:StoragePath"]
            ?? Path.Combine(AppContext.BaseDirectory, "widget-assets");

        var publicBaseUrl = configuration["WidgetAssets:PublicBaseUrl"]
            ?? configuration["Delivery:PublicBaseUrl"]
            ?? string.Empty;
        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "WidgetAssets:PublicBaseUrl must be an absolute http or https URL.");
        }

        _publicBaseUrl = publicBaseUrl.TrimEnd('/');
    }

    public Task<WidgetLogoAsset> GetAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = FindLogoPath(restaurantId)
            ?? throw new FileNotFoundException("Restaurantlogo was not found.");
        var extension = Path.GetExtension(path);
        var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);

        return Task.FromResult(new WidgetLogoAsset(
            stream,
            ContentTypes[extension],
            File.GetLastWriteTimeUtc(path)));
    }

    public async Task<string> SaveAsync(
        Guid restaurantId,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (restaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(restaurantId));
        }

        await using var buffer = new MemoryStream();
        var readBuffer = new byte[64 * 1024];
        int bytesRead;
        while ((bytesRead = await content.ReadAsync(readBuffer, cancellationToken)) > 0)
        {
            if (buffer.Length + bytesRead > IWidgetLogoStorage.MaximumFileSize)
            {
                throw new ArgumentException("Het logo mag maximaal 2 MB groot zijn.", nameof(content));
            }

            await buffer.WriteAsync(readBuffer.AsMemory(0, bytesRead), cancellationToken);
        }

        if (buffer.Length == 0)
        {
            throw new ArgumentException("Kies een afbeelding om te uploaden.", nameof(content));
        }

        var extension = DetectExtension(buffer.GetBuffer().AsSpan(0, checked((int)buffer.Length)));
        var directory = GetRestaurantDirectory(restaurantId);
        Directory.CreateDirectory(directory);

        var targetPath = Path.Combine(directory, $"{LogoFilePrefix}{extension}");
        var temporaryPath = Path.Combine(directory, $"{Guid.NewGuid():N}.tmp");

        try
        {
            buffer.Position = 0;
            await using (var file = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 64 * 1024,
                useAsync: true))
            {
                await buffer.CopyToAsync(file, cancellationToken);
            }

            DeleteLogoFiles(directory);
            File.Move(temporaryPath, targetPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        var version = Guid.NewGuid().ToString("N");
        return $"{_publicBaseUrl}/api/restaurants/{restaurantId}/widget-logo?v={version}";
    }

    public Task DeleteAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directory = GetRestaurantDirectory(restaurantId);
        if (Directory.Exists(directory))
        {
            DeleteLogoFiles(directory);

            if (!Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
            }
        }

        return Task.CompletedTask;
    }

    public bool IsManagedUrl(string? logoUrl)
    {
        return !string.IsNullOrWhiteSpace(logoUrl) &&
               logoUrl.StartsWith(
                   $"{_publicBaseUrl}/api/restaurants/",
                   StringComparison.OrdinalIgnoreCase) &&
               logoUrl.Contains("/widget-logo", StringComparison.OrdinalIgnoreCase);
    }

    private string? FindLogoPath(Guid restaurantId)
    {
        var directory = GetRestaurantDirectory(restaurantId);
        return Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, $"{LogoFilePrefix}*")
                .FirstOrDefault(path => ContentTypes.ContainsKey(Path.GetExtension(path)))
            : null;
    }

    private string GetRestaurantDirectory(Guid restaurantId)
    {
        return Path.Combine(_storagePath, restaurantId.ToString("N"));
    }

    private static void DeleteLogoFiles(string directory)
    {
        foreach (var path in Directory.EnumerateFiles(directory, $"{LogoFilePrefix}*"))
        {
            File.Delete(path);
        }
    }

    private static string DetectExtension(ReadOnlySpan<byte> content)
    {
        if (content.Length >= 8 &&
            content[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return ".png";
        }

        if (content.Length >= 3 &&
            content[..3].SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF }))
        {
            return ".jpg";
        }

        if (content.Length >= 12 &&
            content[..4].SequenceEqual("RIFF"u8) &&
            content.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return ".webp";
        }

        throw new ArgumentException(
            "Gebruik een geldig PNG-, JPEG- of WebP-logo.",
            nameof(content));
    }
}
