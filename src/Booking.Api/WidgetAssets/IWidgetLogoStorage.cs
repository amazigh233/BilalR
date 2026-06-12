namespace Booking.Api.WidgetAssets;

public interface IWidgetLogoStorage
{
    const long MaximumFileSize = 2 * 1024 * 1024;

    Task<WidgetLogoAsset> GetAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default);

    Task<string> SaveAsync(
        Guid restaurantId,
        Stream content,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default);

    bool IsManagedUrl(string? logoUrl);
}

public sealed record WidgetLogoAsset(
    Stream Content,
    string ContentType,
    DateTimeOffset LastModifiedUtc);
